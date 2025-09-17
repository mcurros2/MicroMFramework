using MicroM.Configuration;
using MicroM.Web.Services;

namespace MicroM.Web.Authentication.SSO;

public interface IIdentityProviderService
{
    EtagContent HandleWellKnown(ApplicationOption app_config, string request_base, CancellationToken ct);

    EtagContent HandleJwks(ApplicationOption app_config, CancellationToken ct);

    Task<string> HandleAuthorize(ApplicationOption app_config, string app_id, string user_id, CancellationToken ct);

    Task<string> HandleToken(ApplicationOption app_config, string app_id, string user_id, CancellationToken ct);

    Task<Dictionary<string, object?>> HandleUserInfo(ApplicationOption app_config, string app_id, string user_id, CancellationToken ct);

    Task<Dictionary<string, object?>> HandlePAR(ApplicationOption app_config, string app_id, string token, CancellationToken ct);

    Task<bool> HandleEndSession(ApplicationOption app_config, string app_id, string user_id, CancellationToken ct);


}
