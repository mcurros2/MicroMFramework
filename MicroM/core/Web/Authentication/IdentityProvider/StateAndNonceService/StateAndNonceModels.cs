using MicroM.Core;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.WebUtilities;
using System.Security.Cryptography;
using System.Text;

namespace MicroM.Web.Authentication.SSO;

// CORE DATA: canonical values persisted and validated
public sealed record StateAndNonceData(string State, string Nonce, string? DeviceId, long Timestamp);

// CONTEXT: wraps data plus adjusted form used during PAR construction (not persisted)
public sealed record StateAndNonceContext(StateAndNonceData Data, IFormCollection? AdjustedForm);

public sealed record StateAndNonceHashed
(
    StateAndNonceData Data,
    string MacHash
)
{
    private static string EncodeDeviceId(string? deviceId) => string.IsNullOrEmpty(deviceId) ? "" : Convert.ToBase64String(Encoding.UTF8.GetBytes(deviceId));

    private static string CoreString(StateAndNonceData data) =>
        string.Join('.',
            data.State,
            data.Nonce,
            data.Timestamp.ToString(),
            EncodeDeviceId(data.DeviceId));

    private static string ComputeHmacBase64Url(string keyMaterial, string data)
    {
        using var h = new HMACSHA256(Encoding.UTF8.GetBytes(keyMaterial));
        var hash = h.ComputeHash(Encoding.UTF8.GetBytes(data));
        return WebEncoders.Base64UrlEncode(hash);
    }


    public static string Encode(string hmacKey, StateAndNonceData data)
    {
        var core = CoreString(data);
        var mac = ComputeHmacBase64Url(hmacKey, core);
        var composite = $"{core}.{mac}";
        return WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(composite));
    }

    private const int EXPECTED_SEGMENTS = 5;
    public static ResultWithStatus<StateAndNonceHashed, string> Decode(string hmacKey, string encoded)
    {
        if (string.IsNullOrWhiteSpace(encoded))
        {
            return new(null, "payload_missing");
        }

        string composite;
        try
        {
            composite = Encoding.UTF8.GetString(WebEncoders.Base64UrlDecode(encoded));
        }
        catch
        {
            return new(null, "payload_base64url_invalid");
        }

        var parts = composite.Split('.', StringSplitOptions.None);
        if (parts.Length != EXPECTED_SEGMENTS)
        {
            return new(null, "invalid_payload_format");
        }

        var state = parts[0];
        var nonce = parts[1];
        var tsStr = parts[2];
        var encodedDeviceId = parts[3];
        var mac = parts[4];

        if (!long.TryParse(tsStr, out var ts))
        {
            return new(null, "invalid_payload_timestamp");
        }

        // Recreate core for MAC verification
        var core = string.Join('.', state, nonce, tsStr, encodedDeviceId);
        var expectedMac = ComputeHmacBase64Url(hmacKey, core);

        if (!CryptographicOperations.FixedTimeEquals(
                Encoding.UTF8.GetBytes(mac),
                Encoding.UTF8.GetBytes(expectedMac)))
        {
            return new(null, "mac_invalid");
        }

        string? deviceId = null;
        if (!string.IsNullOrEmpty(encodedDeviceId))
        {
            try
            {
                deviceId = Encoding.UTF8.GetString(Convert.FromBase64String(encodedDeviceId));
            }
            catch
            {
                return new(null, "deviceid_decode_error");
            }
        }

        var data = new StateAndNonceData(state, nonce, deviceId, ts);
        return new(new StateAndNonceHashed(data, mac), null);
    }

    public bool IsExpired(int ttl_minutes) => (DateTimeOffset.UtcNow - DateTimeOffset.FromUnixTimeSeconds(Data.Timestamp)) > TimeSpan.FromMinutes(ttl_minutes);


}

