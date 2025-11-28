using MicroM.Configuration;
using MicroM.Core;
using Microsoft.IdentityModel.Tokens;
using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Text;

namespace MicroM.Web.Authentication.SSO;

public class MemoryAuthorizationCodeService : IAuthorizationCodeService
{
    private readonly ConcurrentDictionary<string, AuthorizationCodeRecord> _codeStore = new();

    // Store by key: {appId}_{clientId}_{code}
    public AuthorizationCodeRecord CreateAndStoreAuthorizationCode(ApplicationOption app, string clientId, AuthorizationCodeRecord record)
    {
        record = record with { Code = CryptClass.GenerateBase64UrlRandomCode(32) };
        var key = $"{app.ApplicationID}_{clientId}_{record.Code}";
        _codeStore.TryAdd(key, record);
        return record;
    }

    public AuthorizationCodeRecord? ValidateAndConsumeAuthorizationCode(ApplicationOption app, string code, string clientId, string redirectUri, string? codeVerifier)
    {
        var key = $"{app.ApplicationID}_{clientId}_{code}";
        if (!_codeStore.TryRemove(key, out var record)) return null;

        if (record.RedirectUri != redirectUri) return null;
        if (DateTimeOffset.UtcNow > record.ExpiresAt) return null;

        if (!string.IsNullOrEmpty(record.CodeChallenge))
        {
            if (string.IsNullOrEmpty(codeVerifier)) return null;

            // Enforce PKCE verification based on the original code_challenge_method
            if (string.Equals(record.CodeChallengeMethod, nameof(OIDCCodeChallengeMethod.S256), StringComparison.OrdinalIgnoreCase))
            {
                var hash = SHA256.HashData(Encoding.UTF8.GetBytes(codeVerifier));
                var ver = Base64UrlEncoder.Encode(hash);

                // Use constant time comparison to avoid timing attacks
                if (!CryptographicOperations.FixedTimeEquals(
                        Encoding.UTF8.GetBytes(ver),
                        Encoding.UTF8.GetBytes(record.CodeChallenge)))
                {
                    return null;
                }
            }
            else if (string.Equals(record.CodeChallengeMethod, nameof(OIDCCodeChallengeMethod.plain), StringComparison.OrdinalIgnoreCase))
            {
                // plain: verifier must exactly match the stored code_challenge
                if (!CryptographicOperations.FixedTimeEquals(
                        Encoding.UTF8.GetBytes(codeVerifier),
                        Encoding.UTF8.GetBytes(record.CodeChallenge)))
                {
                    return null;
                }
            }
            else
            {
                // Unknown PKCE method -> reject
                return null;
            }
        }

        return record;
    }

    public void RemoveAuthorizationCodesForClient(ApplicationOption app, string clientId)
    {
        var prefix = $"{app.ApplicationID}_{clientId}_";
        var keys = _codeStore.Keys.Where(k => k.StartsWith(prefix)).ToList();
        foreach (var k in keys) _codeStore.TryRemove(k, out _);
    }

    public void ClearAuthorizationCodesForApp(ApplicationOption app)
    {
        var prefix = $"{app.ApplicationID}_";
        var keys = _codeStore.Keys.Where(k => k.StartsWith(prefix)).ToList();
        foreach (var k in keys) _codeStore.TryRemove(k, out _);
    }

    public void ClearAllAuthorizationCodes() => _codeStore.Clear();

}
