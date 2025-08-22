using MicroM.Web.Authentication;
using MicroM.Web.Services;
using Microsoft.AspNetCore.Mvc;

namespace MicroM.Web.Controllers;

/// <summary>
/// Represents the IAuthenticationController.
/// </summary>
public interface IAuthenticationController
{
    /// <summary>
    /// Performs the GetStatus operation.
    /// </summary>
    string GetStatus();

    /// <summary>
    /// Performs the Login operation.
    /// </summary>
    Task<ActionResult> Login(IAuthenticationService api, IAuthenticationProvider auth, WebAPIJsonWebTokenHandler jwt_handler, string app_id, UserLogin userLogin, CancellationToken ct);
    /// <summary>
    /// Performs the Logoff operation.
    /// </summary>
    Task<ActionResult> Logoff(IAuthenticationProvider auth, IAuthenticationService api, string app_id, CancellationToken ct);
    /// <summary>
    /// Performs the IsLoggedIn operation.
    /// </summary>
    ActionResult IsLoggedIn();
    /// <summary>
    /// Performs the RecoverPassword operation.
    /// </summary>
    Task<ActionResult> RecoverPassword(IAuthenticationService api, IAuthenticationProvider auth, string app_id, UserRecoverPassword parms, CancellationToken ct);
    /// <summary>
    /// Performs the RecoveryEmail operation.
    /// </summary>
    Task<ActionResult> RecoveryEmail(IAuthenticationService api, IAuthenticationProvider auth, string app_id, UserRecoveryEmail parms, CancellationToken ct);
    /// <summary>
    /// Performs the RefreshToken operation.
    /// </summary>
    Task<ActionResult> RefreshToken(IAuthenticationService api, IAuthenticationProvider auth, WebAPIJsonWebTokenHandler jwt_handler, string app_id, UserRefreshTokenRequest user_refresh, CancellationToken ct);
}
