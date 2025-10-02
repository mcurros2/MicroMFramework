using MicroM.Web.Authentication;
using MicroM.Web.Authentication.SSO;
using MicroM.Web.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace MicroM.Web.Controllers;

public interface IOIDCClientController
{
    ActionResult Jwks(IMicroMAppConfiguration app_config, IOIDCClientService client_service, string app_id, CancellationToken ct);
    Task<ActionResult> SignInOidc(IMicroMAppConfiguration app_config, IOIDCClientService clientService, string app_id, CancellationToken ct);
    Task<ActionResult> OidcClientCallback(
        IMicroMAppConfiguration app_config,
        IOIDCClientService clientService,
        IDeviceIdService deviceid_service,
        IAuthenticationService auth_service,
        WebAPIJsonWebTokenHandler jwt_handler,
        IAuthenticationProvider auth,
        ILogger<OIDCClientController> log,
        string app_id,
        CancellationToken ct);
    Task SignOutOidc(string app_id, string? returnUrl, CancellationToken ct);
}
