using MicroM.Configuration;

namespace MicroM.Web.Authentication.SSO;

public interface IOIDCIdentityProvider
{
    Task<Dictionary<string, string>> GetWellKnown(ApplicationOption app_config, CancellationToken ct);

    Task<string> GetJwks(CancellationToken ct);

    Task<string> GetAuthorizationCode(ApplicationOption app_config, string user_id, CancellationToken ct);

    Task<string> GetBackchannelToken(ApplicationOption app_config, string user_id, CancellationToken ct);

}
