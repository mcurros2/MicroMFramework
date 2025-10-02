using MicroM.Configuration;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;
using System.Security.Cryptography.X509Certificates;

namespace MicroM.Web.Authentication.SSO;

public class ApplicationCertificateCacheService(ILogger<ApplicationCertificateCacheService> log) : IApplicationCertificateCacheService
{
    private readonly ConcurrentDictionary<string, X509Certificate2> _certificateCache = new();

    private X509Certificate2? GetOrAddCertificateToCache(ApplicationOption app)
    {
        if (app.OIDCCertificateBlob == null || app.OIDCCertificateBlob.Length == 0)
        {
            log.LogError("{app_id}: OIDC Certificate not configured - Blob is null or empty", app.ApplicationID);
            return null;
        }

        return _certificateCache.GetOrAdd($"{app.ApplicationID}_{app.OIDCCertificateUniqueID ?? "default"}", key =>
        {
            var cert = new X509Certificate2(app.OIDCCertificateBlob, app.OIDCCertificatePassword, X509KeyStorageFlags.EphemeralKeySet);
            return cert;
        });
    }

    public X509Certificate2? GetCertificate(ApplicationOption app)
    {
        return GetOrAddCertificateToCache(app);
    }

    public void ClearCache() => _certificateCache.Clear();

}
