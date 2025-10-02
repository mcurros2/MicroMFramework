using MicroM.Web.Authentication;
using MicroM.Web.Authentication.SSO;
using MicroM.Web.Services;
using Microsoft.AspNetCore.Mvc;

namespace MicroM.Web.Controllers;

public interface IIdentityProviderController
{
    ActionResult WellKnown(IMicroMAppConfiguration app_config, IIdentityProviderService idp, string app_id, CancellationToken ct);

    ActionResult Jwks(IMicroMAppConfiguration app_config, IIdentityProviderService idp, string app_id, CancellationToken ct);

    Task<ActionResult> Token(IMicroMAppConfiguration app_config, IIdentityProviderService idp, string app_id, CancellationToken ct);

    Task<ActionResult> PAR(IMicroMAppConfiguration app_config, IIdentityProviderService idp, string app_id, CancellationToken ct);

    Task<ActionResult> Authorize(IMicroMAppConfiguration app_config, IIdentityProviderService idp, string app_id, CancellationToken ct);

    Task<Dictionary<string, object?>> UserInfo(IAuthenticationProvider auth, IMicroMAppConfiguration app_config, string app_id, string userId, CancellationToken ct);

    Task<bool> Revoke(IAuthenticationProvider auth, IMicroMAppConfiguration app_config, string app_id, string token, CancellationToken ct);

    Task<Dictionary<string, object?>> Introspect(IAuthenticationProvider auth, IMicroMAppConfiguration app_config, string app_id, string token, CancellationToken ct);

    Task<bool> EndSession(IAuthenticationProvider auth, IMicroMAppConfiguration app_config, string app_id, string userId, CancellationToken ct);
}
