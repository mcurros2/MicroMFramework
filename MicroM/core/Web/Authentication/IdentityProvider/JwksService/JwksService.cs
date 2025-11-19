using MicroM.Configuration;
using MicroM.Web.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Headers;
using Microsoft.Extensions.Logging;
using System.Security.Cryptography.X509Certificates;
using System.Text.Json;

namespace MicroM.Web.Authentication.SSO;

public class JwksService(
    IApplicationCertificateCacheService certificate_cache,
    IEtagCacheService etag_cache,
    ILogger<JwksService> log
    ) : IJwksService
{

    private JsonSerializerOptions _jsonUnsafeSerializationOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull,
        Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };

    public EtagCacheServiceCacheCheckResult? HandleJwks(ApplicationOption app, RequestHeaders request_headers, IHeaderDictionary response_headers)
    {
        if (app.OIDCCertificateBlob == null)
        {
            log.LogWarning("JWKS requested for app {app} which has no certificate configured", app.ApplicationID);
            return null;
        }
        var key = $"{app.ApplicationID}_JWKS";

        var result = etag_cache.GetOrAddResponseWithCacheCheck(
            key,
            request_headers,
            response_headers,
            cache_duration_seconds: ConfigurationDefaults.JwksCacheDurationSeconds,
            () =>
        {
            var jwks = CreateJwksResponse(app);
            return JsonSerializer.Serialize(jwks, _jsonUnsafeSerializationOptions);
        });

        return result;
    }

    private OIDCJwksResponse CreateJwksResponse(ApplicationOption app)
    {
        X509Certificate2? cert = certificate_cache.GetCertificate(app);

        if (cert == null)
        {
            log.LogWarning("JWKS requested for app {app} but certificate_cache returned null certificate", app.ApplicationID);
            return new(keys: []);
        }

        var keys = new List<OIDCJwksKeyResponse>(capacity: 2);

        // Add RSA key when certificate has RSA public key
        var rsaKey = JwksProvider.GetRSAKey(app, cert);
        if (rsaKey != null) keys.Add(rsaKey);

        // Add EC key when certificate has ECDSA public key
        var ecKey = JwksProvider.GetECDKey(app, cert);
        if (ecKey != null) keys.Add(ecKey);

        return new(keys: keys);
    }
}
