using MicroM.Configuration;
using MicroM.Web.Services;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;
using System.Security.Cryptography.X509Certificates;

namespace MicroM.Web.Authentication.SSO;

public class ApplicationCertificateCacheService : IApplicationCertificateCacheService
{
    private readonly ConcurrentDictionary<string, X509Certificate2> _certificateCache = new();
    private readonly ILogger<ApplicationCertificateCacheService> log;
    private readonly IMemoryEventBus bus;

    public ApplicationCertificateCacheService(ILogger<ApplicationCertificateCacheService> log, IMemoryEventBus bus)
    {
        this.log = log;
        this.bus = bus;
        bus.Subscribe<MicroMConfigurationReloaded>(_ =>
        {
            log.LogInformation("Clearing application certificate cache due to MicroMConfigurationReloaded");
            ClearCache();
        });
    }

    private X509Certificate2? GetOrAddCertificateToCache(ApplicationOption app)
    {
        if (app.OIDCCertificateBlob == null || app.OIDCCertificateBlob.Length == 0)
        {
            log.LogError("{app_id}: OIDC Certificate not configured - Blob is null or empty", app.ApplicationID);
            return null;
        }

        return _certificateCache.GetOrAdd($"{app.ApplicationID}_{app.OIDCCertificateUniqueID ?? "default"}", key =>
        {
            var cert = X509CertificateLoader.LoadPkcs12(
                app.OIDCCertificateBlob,
                app.OIDCCertificatePassword,
                X509KeyStorageFlags.EphemeralKeySet | X509KeyStorageFlags.Exportable);
            return cert;
        });
    }

    public X509Certificate2? GetCertificate(ApplicationOption app)
    {
        return GetOrAddCertificateToCache(app);
    }

    public void ClearCache() => _certificateCache.Clear();

}
