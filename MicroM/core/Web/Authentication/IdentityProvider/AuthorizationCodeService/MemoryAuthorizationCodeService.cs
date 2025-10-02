using MicroM.Configuration;
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
        record = record with { Code = GenerateCode() };
        var key = $"{app.ApplicationID}_{clientId}_{record.Code}";
        _codeStore.TryAdd(key, record);
        return record;
    }

    public AuthorizationCodeRecord? ValidateAndConsumeAuthorizationCode(ApplicationOption app, string code, string clientId, string redirectUri, string? codeVerifier)
    {
        var key = $"{app.ApplicationID}_{clientId}_{code}";
        if (!_codeStore.TryRemove(key, out var record)) return null;

        if (!string.Equals(record.RedirectUri, redirectUri, StringComparison.Ordinal)) return null;
        if (DateTimeOffset.UtcNow > record.ExpiresAt) return null;

        if (!string.IsNullOrEmpty(record.CodeChallenge))
        {
            if (string.IsNullOrEmpty(codeVerifier)) return null;
            if (string.Equals(record.CodeChallengeMethod, "S256", StringComparison.OrdinalIgnoreCase))
            {
                var hash = SHA256.HashData(Encoding.UTF8.GetBytes(codeVerifier));
                var ver = Base64UrlEncoder.Encode(hash);
                if (!string.Equals(ver, record.CodeChallenge, StringComparison.Ordinal)) return null;
            }
            else return null;
        }

        return record;
    }

    public void RemoveAuthorizationCodesForClient(ApplicationOption app, string clientId)
    {
        var prefix = $"{app.ApplicationID}_{clientId}_";
        var keys = _codeStore.Keys.Where(k => k.StartsWith(prefix, StringComparison.Ordinal)).ToList();
        foreach (var k in keys) _codeStore.TryRemove(k, out _);
    }

    public void ClearAuthorizationCodesForApp(ApplicationOption app)
    {
        var prefix = $"{app.ApplicationID}_";
        var keys = _codeStore.Keys.Where(k => k.StartsWith(prefix, StringComparison.Ordinal)).ToList();
        foreach (var k in keys) _codeStore.TryRemove(k, out _);
    }

    public void ClearAllAuthorizationCodes() => _codeStore.Clear();

    private static string GenerateCode(int length = 32)
    {
        var bytes = RandomNumberGenerator.GetBytes(length);
        return Base64UrlEncoder.Encode(bytes);
    }
}
