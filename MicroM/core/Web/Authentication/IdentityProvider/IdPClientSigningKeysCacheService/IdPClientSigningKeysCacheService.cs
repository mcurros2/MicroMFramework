using MicroM.Configuration;
using MicroM.Web.Services;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using System.Collections.Concurrent;
using System.Security.Cryptography.X509Certificates;

namespace MicroM.Web.Authentication.SSO;

public class IdPClientSigningKeysCacheService(
    IMicroMAppConfiguration appConfig,
    IApplicationCertificateCacheService certCache,
    IJWKSFetchCacheService jwksCache,
    ILogger<IdPClientSigningKeysCacheService> log
    ) : IIdPClientSigningKeysCacheService
{
    // Cache SigningKeys by a composite key (source + IdP app + client + etag)
    // The value is null when no usable keys were found for that composite key.
    private readonly ConcurrentDictionary<string, Lazy<IReadOnlyList<SecurityKey>?>> _cache = new();

    public async Task<IReadOnlyList<SecurityKey>> GetClientSigningKeysAsync(ApplicationOption idpApp, string clientId, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        if (string.IsNullOrWhiteSpace(clientId))
        {
            log.LogWarning("IDP_SIGNING_CLIENT_ID_EMPTY idp_app={idp}", idpApp.ApplicationID);
            return [];
        }

        if (idpApp.OIDCClientConfiguration == null ||
            !idpApp.OIDCClientConfiguration.TryGetValue(clientId, out var clientCfg) ||
            clientCfg == null)
        {
            log.LogWarning("IDP_SIGNING_CLIENT_NOT_REGISTERED idp_app={idp} client_id={client}", idpApp.ApplicationID, clientId);
            return [];
        }

        // 1. Hosted client app cert – prefer local
        var clientApp = appConfig.GetAppConfiguration(clientId);
        if (clientApp != null)
        {
            string cacheKey = $"local:{idpApp.ApplicationID}:{clientId}";

            var lazy = _cache.GetOrAdd(cacheKey, _ =>
                new Lazy<IReadOnlyList<SecurityKey>?>(() =>
                {
                    var cert = certCache.GetCertificate(clientApp);
                    if (cert == null)
                    {
                        log.LogWarning("IDP_SIGNING_LOCAL_CERT_MISSING idp_app={idp} client_id={client}", idpApp.ApplicationID, clientId);
                        return null;
                    }

                    var key = BuildSecurityKeyFromCert(cert);
                    if (key == null)
                    {
                        log.LogWarning("IDP_SIGNING_LOCAL_CERT_UNSUPPORTED_KEY idp_app={idp} client_id={client}", idpApp.ApplicationID, clientId);
                        return null;
                    }

                    log.LogTrace("IDP_SIGNING_LOCAL_CERT_USED idp_app={idp} client_id={client} thumbprint={thumb}",
                        idpApp.ApplicationID, clientId, cert.Thumbprint);

                    return [key];
                }));

            return lazy.Value ?? [];
        }

        // 2. Remote JWKS
        if (string.IsNullOrWhiteSpace(clientCfg.URLClientJWKS))
        {
            log.LogWarning("IDP_SIGNING_NO_JWKS_URL idp_app={idp} client_id={client}", idpApp.ApplicationID, clientId);
            return [];
        }

        var jwksResult = await jwksCache.GetAsync(clientCfg.URLClientJWKS, ct).ConfigureAwait(false);
        if (jwksResult.Keys.Count == 0)
        {
            log.LogWarning("IDP_SIGNING_JWKS_EMPTY idp_app={idp} client_id={client} jwks={jwks}", idpApp.ApplicationID, clientId, clientCfg.URLClientJWKS);
            return [];
        }


        string etag = jwksResult.ServerETag ?? jwksResult.ETag ?? "<noetag>";
        string jwksCacheKey = $"jwks:{idpApp.ApplicationID}:{clientId}:{clientCfg.URLClientJWKS}:{etag}";

        var jwksLazy = _cache.GetOrAdd(jwksCacheKey, _ =>
            new Lazy<IReadOnlyList<SecurityKey>?>(() =>
            {
                // Filter to candidate signing keys
                var signingKeys = jwksResult.Keys.Values
                    .Where(IsValidClientSigningKey)
                    .ToList();

                if (signingKeys.Count == 0)
                {
                    log.LogWarning("IDP_SIGNING_JWKS_NO_VALID_KEYS idp_app={idp} client_id={client} jwks={jwks}", idpApp.ApplicationID, clientId, clientCfg.URLClientJWKS);
                    return null;
                }

                // If we have a certificate unique id, try to match it as an exact key
                if (!string.IsNullOrWhiteSpace(clientCfg.CertificateUniqueID) && jwksResult.Keys.TryGetValue(clientCfg.CertificateUniqueID, out var exact))
                {
                    log.LogInformation("IDP_SIGNING_JWKS_CERT_ID_MATCH idp_app={idp} client_id={client} cert_unique_id={cid}", idpApp.ApplicationID, clientId, clientCfg.CertificateUniqueID);
                    return [exact];
                }

                // Otherwise, return all valid signing keys (caller will use them as a key set)
                log.LogTrace("IDP_SIGNING_JWKS_KEYS_SELECTED idp_app={idp} client_id={client} count={count}", idpApp.ApplicationID, clientId, signingKeys.Count);

                return signingKeys;
            }));

        return jwksLazy.Value ?? [];
    }

    public void Clear()
    {
        _cache.Clear();
    }

    private static SecurityKey? BuildSecurityKeyFromCert(X509Certificate2 cert)
    {
        if (cert.GetRSAPublicKey() != null)
            return new X509SecurityKey(cert);
        if (cert.GetECDsaPublicKey() != null)
            return new ECDsaSecurityKey(cert.GetECDsaPublicKey());
        return null;
    }

    private static bool IsValidClientSigningKey(SecurityKey key)
    {
        if (key is JsonWebKey jwk)
        {
            if (!string.IsNullOrEmpty(jwk.Use) &&
                !jwk.Use.Equals("sig", StringComparison.OrdinalIgnoreCase))
                return false;

            // alg allow-list per OIDC spec
            if (!string.IsNullOrEmpty(jwk.Alg) &&
                !OIDCCryptoCapabilities.Idp.AllowedClientAssertionSigningAlgStrings.Contains(jwk.Alg))
                return false;
        }

        // RSA and EC both allowed
        return true;
    }

}
