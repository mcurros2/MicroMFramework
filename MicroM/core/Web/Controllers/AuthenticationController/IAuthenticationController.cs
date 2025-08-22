using MicroM.Web.Authentication;
using MicroM.Web.Services;
using Microsoft.AspNetCore.Mvc;

namespace MicroM.Web.Controllers;

public interface IAuthenticationController
{
    string GetStatus();

    Task<ActionResult> Login(IAuthenticationService api, IAuthenticationProvider auth, WebAPIJsonWebTokenHandler jwt_handler, string app_id, UserLogin userLogin, CancellationToken ct);
    Task<ActionResult> Logoff(IAuthenticationProvider auth, IAuthenticationService api, string app_id, CancellationToken ct);
    ActionResult IsLoggedIn();
    Task<ActionResult> RecoverPassword(IAuthenticationService api, IAuthenticationProvider auth, string app_id, UserRecoverPassword parms, CancellationToken ct);
    Task<ActionResult> RecoveryEmail(IAuthenticationService api, IAuthenticationProvider auth, string app_id, UserRecoveryEmail parms, CancellationToken ct);
    Task<ActionResult> RefreshToken(IAuthenticationService api, IAuthenticationProvider auth, WebAPIJsonWebTokenHandler jwt_handler, string app_id, UserRefreshTokenRequest user_refresh, CancellationToken ct);
}
