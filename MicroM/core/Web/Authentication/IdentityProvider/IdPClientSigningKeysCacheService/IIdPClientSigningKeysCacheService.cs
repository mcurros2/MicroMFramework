using MicroM.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace MicroM.Web.Authentication.SSO;

public interface IIdPClientSigningKeysCacheService
{
    Task<IReadOnlyList<SecurityKey>> GetClientSigningKeysAsync(ApplicationOption idpApp, string clientId, CancellationToken ct);

    void Clear();
}
