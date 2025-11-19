using MicroM.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace MicroM.Web.Authentication.SSO;

public interface IAudienceCryptoCacheService
{
    Task<EncryptingCredentials?> GetEncryptingCredentialsAsync(ApplicationOption idpApp, string audience, CancellationToken ct);
    void Clear();
}