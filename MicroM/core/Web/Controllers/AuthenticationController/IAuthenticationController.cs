using MicroM.Web.Authentication;
using MicroM.Web.Services;
using Microsoft.AspNetCore.Mvc;

namespace MicroM.Web.Controllers;

/// <summary>
/// Defines the contract for authentication API endpoints.
/// </summary>
public interface IAuthenticationController
{
    /// <summary>
    /// Returns a simple response indicating that the authentication API is available.
    /// </summary>
    string GetStatus();

    /// <summary>
    /// Authenticates a user and issues authentication tokens.
    /// </summary>
    Task<ActionResult> Login(IAuthenticationService api, IAuthenticationProvider auth, WebAPIJsonWebTokenHandler jwt_handler, string app_id, UserLogin userLogin, CancellationToken ct);
    /// <summary>
    /// Signs the current user out and clears authentication cookies.
    /// </summary>
    Task<ActionResult> Logoff(IAuthenticationProvider auth, IAuthenticationService api, string app_id, CancellationToken ct);
    /// <summary>
    /// Checks whether the current user session is authenticated.
    /// </summary>
    ActionResult IsLoggedIn();
    /// <summary>
    /// Resets a user's password using a recovery code.
    /// </summary>
    Task<ActionResult> RecoverPassword(IAuthenticationService api, IAuthenticationProvider auth, string app_id, UserRecoverPassword parms, CancellationToken ct);
    /// <summary>
    /// Sends a password recovery email.
    /// </summary>
    Task<ActionResult> RecoveryEmail(IAuthenticationService api, IAuthenticationProvider auth, string app_id, UserRecoveryEmail parms, CancellationToken ct);
    /// <summary>
    /// Exchanges a refresh token for a new access token.
    /// </summary>
    Task<ActionResult> RefreshToken(IAuthenticationService api, IAuthenticationProvider auth, WebAPIJsonWebTokenHandler jwt_handler, string app_id, UserRefreshTokenRequest user_refresh, CancellationToken ct);
}
