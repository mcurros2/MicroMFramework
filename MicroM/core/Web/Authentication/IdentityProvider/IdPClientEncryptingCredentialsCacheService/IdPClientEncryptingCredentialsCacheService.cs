using MicroM.Configuration;
using MicroM.Web.Services;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using System.Collections.Concurrent;
using System.Security.Cryptography.X509Certificates;

namespace MicroM.Web.Authentication.SSO;

public class IdPClientEncryptingCredentialsCacheService(
    IMicroMAppConfiguration appConfig,
    IApplicationCertificateCacheService certCache,
    IJWKSFetchCacheService jwksCache,
    ILogger<IdPClientEncryptingCredentialsCacheService> log
    ) : IIdPClientEncryptingCredentialsCacheService
{
    // Cache EncryptingCredentials by a composite key (source + app + audience + etag + keyid + algs)
    private readonly ConcurrentDictionary<string, Lazy<EncryptingCredentials?>> _cache = new(StringComparer.Ordinal);

    public void Clear() => _cache.Clear();

    public async Task<EncryptingCredentials?> GetEncryptingCredentialsAsync(ApplicationOption idpApp, string audience, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        // Must have client registration
        if (idpApp.OIDCClientConfiguration == null ||
            !idpApp.OIDCClientConfiguration.TryGetValue(audience, out var clientCfg))
        {
            log.LogWarning("IDP_CRYPTO_CLIENT_ID_EMPTY idp_app={idp}", idpApp.ApplicationID);
            return null;
        }

        // 1. Hosted client certificate path (no network)
        var clientApp = appConfig.GetAppConfiguration(audience);
        if (clientApp != null)
        {
            var clientCert = certCache.GetCertificate(clientApp);
            if (clientCert != null)
            {
                var hostedKey = BuildEncryptingCredentialsFromCertificate(clientCert);
                if (hostedKey != null) return hostedKey; // Local always preferred
            }
        }

        // 2. Remote JWKS path
        if (string.IsNullOrWhiteSpace(clientCfg.URLClientJWKS))
        {
            log.LogWarning("IDP_CRYPTO_JWKS_URL_EMPTY idp_app={idp} audience={aud}", idpApp.ApplicationID, audience);
            return null;
        }

        var jwksResult = await jwksCache.GetAsync(clientCfg.URLClientJWKS, ct).ConfigureAwait(false);
        if (jwksResult.Keys.Count == 0)
        {
            log.LogWarning("IDP_CRYPTO_JWKS_NO_KEYS idp_app={idp} audience={aud} jwks_url={url}", idpApp.ApplicationID, audience, clientCfg.URLClientJWKS);
            return null;
        }

        // Resolve candidate key
        var selectedKey = ResolveSecurityKey(jwksResult.Keys, clientCfg.CertificateUniqueID);
        if (selectedKey == null)
        {
            log.LogWarning("IDP_CRYPTO_NO_MATCH idp_app={idp} audience={aud} cert_unique_id={cid}", idpApp.ApplicationID, audience, clientCfg.CertificateUniqueID ?? "<null>");
            return null;
        }

        // Build cache key (JWKS ETag ensures rotation invalidation)
        string keyId = selectedKey.KeyId ?? "<nokid>";
        string etag = jwksResult.ServerETag ?? jwksResult.ETag ?? "<noetag>";
        string cacheKey = $"jwks:{idpApp.ApplicationID}:{audience}:{clientCfg.URLClientJWKS}:{etag}:{keyId}";

        var lazy = _cache.GetOrAdd(cacheKey, _ => new Lazy<EncryptingCredentials?>(() => BuildEncryptingCredentialsFromSecurityKey(selectedKey)));
        return lazy.Value;
    }

    private static EncryptingCredentials? BuildEncryptingCredentialsFromCertificate(X509Certificate2 cert)
    {
        SecurityKey? key = null;
        if (cert.GetRSAPublicKey() != null)
            key = new X509SecurityKey(cert);
        else
        {
            var ec = cert.GetECDsaPublicKey();
            if (ec != null) key = new ECDsaSecurityKey(ec);
        }
        if (key == null) return null;
        return BuildEncryptingCredentialsFromSecurityKey(key);
    }

    private static EncryptingCredentials? BuildEncryptingCredentialsFromSecurityKey(SecurityKey key)
    {
        // Key management algorithm candidates (RSA or EC)
        string[] keyAlgs;
        if (IsRsa(key))
            keyAlgs = [SecurityAlgorithms.RsaOAEP, SecurityAlgorithms.RsaPKCS1];
        else if (IsEc(key))
            keyAlgs = [SecurityAlgorithms.EcdhEsA256kw, SecurityAlgorithms.EcdhEs];
        else
            return null;

        // Content encryption preference
        string[] contentAlgs = [SecurityAlgorithms.Aes256Gcm, SecurityAlgorithms.Aes256CbcHmacSha512];

        foreach (var km in keyAlgs)
        {
            foreach (var enc in contentAlgs)
            {
                try { return new EncryptingCredentials(key, km, enc); } catch { }
            }
        }
        return null;
    }

    private static bool IsRsa(SecurityKey k)
    {
        if (k is JsonWebKey jwk && jwk.Kty.Equals("RSA", StringComparison.OrdinalIgnoreCase)) return true;
        if (k is RsaSecurityKey) return true;
        if (k is X509SecurityKey x509 && x509.Certificate?.GetRSAPublicKey() != null) return true;
        return false;
    }

    private static bool IsEc(SecurityKey k)
    {
        if (k is JsonWebKey jwk && jwk.Kty.Equals("EC", StringComparison.OrdinalIgnoreCase)) return true;
        if (k is ECDsaSecurityKey) return true;
        if (k is X509SecurityKey x509 && x509.Certificate?.GetECDsaPublicKey() != null) return true;
        return false;
    }

    private static SecurityKey? ResolveSecurityKey(IReadOnlyDictionary<string, SecurityKey> keys, string? certificateUniqueId)
    {
        // Exact dictionary hit (covers kid/x5tS256/x5t selection order used when building map)
        if (!string.IsNullOrWhiteSpace(certificateUniqueId) && keys.TryGetValue(certificateUniqueId, out var exact))
            return exact;

        // Preference fallback: RSA first then EC
        foreach (var k in keys.Values)
            if (IsRsa(k)) return k;
        foreach (var k in keys.Values)
            if (IsEc(k)) return k;

        return null;
    }


}