using MicroM.Web.Authentication;
using MicroM.Web.Services;

namespace MicroM.Web.Controllers;

public interface IIdentityProviderController
{
    Task<Dictionary<string, string>> WellKnown(IAuthenticationProvider auth, IMicroMAppConfiguration app_config, string app_id, CancellationToken ct);
    Task<string> Jwks(IAuthenticationProvider auth, IMicroMAppConfiguration app_config, string app_id, CancellationToken ct);

    Task<Dictionary<string, object?>> UserInfo(IAuthenticationProvider auth, IMicroMAppConfiguration app_config, string app_id, string userId, CancellationToken ct);
    //Task<bool> Revoke(IAuthenticationProvider auth, IMicroMAppConfiguration app_config, string app_id, string token, CancellationToken ct);
    //Task<Dictionary<string, object?>> Introspect(IAuthenticationProvider auth, IMicroMAppConfiguration app_config, string app_id, string token, CancellationToken ct);

    Task<string> Authorize(IAuthenticationProvider auth, IMicroMAppConfiguration app_config, string app_id, string userId, CancellationToken ct);
    Task<string> Token(IAuthenticationProvider auth, IMicroMAppConfiguration app_config, string app_id, string userId, CancellationToken ct);

    Task<Dictionary<string, object?>> PAR(IAuthenticationProvider auth, IMicroMAppConfiguration app_config, string app_id, string token, CancellationToken ct);

    Task<bool> EndSession(IAuthenticationProvider auth, IMicroMAppConfiguration app_config, string app_id, string userId, CancellationToken ct);
}
