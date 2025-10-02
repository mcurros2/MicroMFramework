using MicroM.Configuration;
using System.Security.Cryptography.X509Certificates;

namespace MicroM.Web.Authentication.SSO
{
    public interface IApplicationCertificateCacheService
    {
        void ClearCache();
        X509Certificate2? GetCertificate(ApplicationOption app);
    }
}