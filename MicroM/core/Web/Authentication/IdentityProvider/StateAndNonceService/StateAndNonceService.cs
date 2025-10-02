using MicroM.Configuration;
using MicroM.Core;
using MicroM.Web.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Primitives;
using System.Security.Cryptography;
using System.Text;

namespace MicroM.Web.Authentication.SSO;

public class StateAndNonceService
    (
    IHttpContextAccessor http_context,
    IMicroMAppConfiguration app_config,
    ILogger<StateAndNonceService> log
    ) : IStateAndNonceService
{

    private const int STATE_COOKIE_TTL_MINUTES = 10;

    private static string GetStateAndNonceCookieName(ApplicationOption app_config)
    {
        return $"m-oidc-stn-{app_config.ApplicationID}";
    }

    private static string ComputeHmacBase64String(string keyMaterial, string data)
    {
        using var h = new HMACSHA256(Encoding.UTF8.GetBytes(keyMaterial));
        return WebEncoders.Base64UrlEncode(h.ComputeHash(Encoding.UTF8.GetBytes(data)));
    }

    public StateAndNonceRecord EnsureStateAndNonce(IFormCollection original, string? providedState, string? providedNonce, string? providedDeviceId)
    {
        string state = string.IsNullOrWhiteSpace(providedState) ? WebEncoders.Base64UrlEncode(RandomNumberGenerator.GetBytes(32)) : providedState;
        string nonce = string.IsNullOrWhiteSpace(providedNonce) ? WebEncoders.Base64UrlEncode(RandomNumberGenerator.GetBytes(32)) : providedNonce;
        string? deviceId = !string.IsNullOrWhiteSpace(providedDeviceId)
                ? providedDeviceId
                : (original.TryGetValue(WellknownIdentityConstants.LocalDeviceId, out var ldid) ? ldid.ToString() : null);


        var dict = new Dictionary<string, StringValues>(StringComparer.Ordinal);
        foreach (var kv in original) dict[kv.Key] = kv.Value;
        dict[WellknownIdentityConstants.State] = new StringValues(state);
        dict[WellknownIdentityConstants.Nonce] = new StringValues(nonce);
        if (!string.IsNullOrWhiteSpace(deviceId))
        {
            dict[WellknownIdentityConstants.LocalDeviceId] = new StringValues(deviceId);
        }

        return new(state, nonce, deviceId, new FormCollection(dict));
    }

    public void StoreStateCookie(ApplicationOption app, string hmacKey, string state, string nonce, string? deviceId)
    {
        var context = http_context.HttpContext ?? throw new InvalidOperationException("No HttpContext available");

        var ts = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        var payload = $"{state}.{nonce}.{ts}.{deviceId ?? ""}";
        var mac = ComputeHmacBase64String(hmacKey, payload);
        var value = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes($"{payload}.{mac}"));

        var cookie_name = GetStateAndNonceCookieName(app);

        var response = context.Response;
        var app_tenant_path = app_config.GetTenantPath(context);

        response.Cookies.Append(
            cookie_name,
            value,
            new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.Strict,
                Path = app_tenant_path,
                Expires = DateTimeOffset.UtcNow.AddMinutes(STATE_COOKIE_TTL_MINUTES)
            });
    }

    public ResultWithStatus<StateAndNonceRecord, string> ValidateAndConsumeStateCookie(string app_id, string hmacKey, string incomingState)
    {
        var context = http_context.HttpContext ?? throw new InvalidOperationException("No HttpContext available");
        var request = context.Request;

        var cookie_name = GetStateAndNonceCookieName(app_config.GetAppConfiguration(app_id) ?? throw new InvalidOperationException("App config not found"));

        if (!request.Cookies.TryGetValue(cookie_name, out var stored))
        {
            log.LogDebug("State cookie {cookie_name} missing", cookie_name);
            return new(null, "state_cookie_missing");
        }

        try
        {
            var decoded = Encoding.UTF8.GetString(WebEncoders.Base64UrlDecode(stored));
            var parts = decoded.Split('.', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length != 4) return new(null, "state_cookie_invalid_format");

            var state = parts[0];
            var nonce = parts[1];
            var tsStr = parts[2];
            var deviceId = parts.Length == 5 ? parts[3] : null;
            var mac = parts.Length == 5 ? parts[4] : parts[3];

            if (!long.TryParse(tsStr, out var ts))
            {
                log.LogDebug("State cookie {cookie_name} has invalid timestamp", cookie_name);
                return new(null, "state_cookie_timestamp_invalid");
            }
            var age = DateTimeOffset.UtcNow - DateTimeOffset.FromUnixTimeSeconds(ts);
            if (age > TimeSpan.FromMinutes(STATE_COOKIE_TTL_MINUTES))
            {
                log.LogDebug("State cookie {cookie_name} has expired (age: {age})", cookie_name, age);
                return new(null, "state_cookie_expired");
            }

            var macPayload = parts.Length == 5 ? $"{state}.{nonce}.{tsStr}.{deviceId}" : $"{state}.{nonce}.{tsStr}";
            var expectedMac = ComputeHmacBase64String(hmacKey, macPayload);

            if (!CryptographicOperations.FixedTimeEquals(
                Encoding.UTF8.GetBytes(mac),
                Encoding.UTF8.GetBytes(expectedMac)))
            {
                log.LogDebug("State cookie {cookie_name} has invalid MAC", cookie_name);
                return new(null, "state_cookie_mac_invalid");
            }

            if (!string.Equals(state, incomingState, StringComparison.Ordinal))
            {
                log.LogDebug("State cookie {cookie_name} state mismatch (expected {expected}, got {actual})", cookie_name, state, incomingState);
                return new(null, "state_mismatch");
            }

            // Single-use
            var response = context.Response;
            var app_tenant_path = app_config.GetTenantPath(context);
            response.Cookies.Delete(cookie_name, new CookieOptions { Path = app_tenant_path });

            return new(new(state, nonce, deviceId, null), null);
        }
        catch (Exception ex)
        {
            log.LogDebug(ex, "Failed to decode or validate state cookie {cookie_name}", cookie_name);
            return new(null, "state_cookie_corrupt");
        }
    }
}
