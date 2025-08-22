using MicroM.Web.Authentication;
using MicroM.Web.Services;
using Microsoft.AspNetCore.Mvc;

namespace MicroM.Web.Controllers;

/// <summary>
/// Defines endpoints for user authentication, token issuance, and session management.
/// </summary>
public interface IAuthenticationController
{
    /// <summary>
    /// Reports the authentication API status.
    /// </summary>
    /// <returns>"OK" when the service is available.</returns>
    string GetStatus();

    /// <summary>
    /// Validates credentials and issues access and refresh tokens.
    /// </summary>
    /// <param name="api">Authentication service used to validate credentials and issue tokens.</param>
    /// <param name="auth">Provider that accesses user information for validation.</param>
    /// <param name="jwt_handler">Handler responsible for generating JWTs.</param>
    /// <param name="app_id">Identifier of the application requesting authentication.</param>
    /// <param name="userLogin">User credentials payload.</param>
    /// <param name="ct">Token to observe cancellation requests.</param>
    /// <returns>An <see cref="ActionResult"/> containing tokens on success or an authorization error.</returns>
    /// <exception cref="OperationCanceledException">Thrown when the operation is cancelled.</exception>
    Task<ActionResult> Login(IAuthenticationService api, IAuthenticationProvider auth, WebAPIJsonWebTokenHandler jwt_handler, string app_id, UserLogin userLogin, CancellationToken ct);

    /// <summary>
    /// Terminates the current user's session.
    /// </summary>
    /// <param name="auth">Provider used to revoke the user's session.</param>
    /// <param name="api">Authentication service that performs logoff logic.</param>
    /// <param name="app_id">Identifier of the application owning the session.</param>
    /// <param name="ct">Token to observe cancellation requests.</param>
    /// <returns>Result indicating success or cancellation.</returns>
    /// <exception cref="OperationCanceledException">Thrown when the operation is cancelled.</exception>
    /// <exception cref="UnauthorizedAccessException">Thrown when the caller is not authorized.</exception>
    Task<ActionResult> Logoff(IAuthenticationProvider auth, IAuthenticationService api, string app_id, CancellationToken ct);

    /// <summary>
    /// Determines whether the current user session is authenticated.
    /// </summary>
    /// <returns>An <see cref="ActionResult"/> representing the authorization state.</returns>
    /// <exception cref="UnauthorizedAccessException">Thrown when the caller is not authorized.</exception>
    ActionResult IsLoggedIn();

    /// <summary>
    /// Resets a user's password using a recovery code.
    /// </summary>
    /// <param name="api">Authentication service that handles password recovery.</param>
    /// <param name="auth">Provider used to manage user accounts.</param>
    /// <param name="app_id">Identifier of the target application.</param>
    /// <param name="parms">Parameters containing the username, new password, and recovery code.</param>
    /// <param name="ct">Token to observe cancellation requests.</param>
    /// <returns>Status indicating success or failure.</returns>
    /// <exception cref="OperationCanceledException">Thrown when the operation is cancelled.</exception>
    Task<ActionResult> RecoverPassword(IAuthenticationService api, IAuthenticationProvider auth, string app_id, UserRecoverPassword parms, CancellationToken ct);

    /// <summary>
    /// Sends a password recovery email to the specified user.
    /// </summary>
    /// <param name="api">Authentication service that sends the recovery email.</param>
    /// <param name="auth">Provider used to locate the user account.</param>
    /// <param name="app_id">Identifier of the application.</param>
    /// <param name="parms">Parameters containing the username to recover.</param>
    /// <param name="ct">Token to observe cancellation requests.</param>
    /// <returns>Status describing the result of the email send operation.</returns>
    /// <exception cref="OperationCanceledException">Thrown when the operation is cancelled.</exception>
    Task<ActionResult> RecoveryEmail(IAuthenticationService api, IAuthenticationProvider auth, string app_id, UserRecoveryEmail parms, CancellationToken ct);

    /// <summary>
    /// Issues a new access token pair based on a valid refresh token.
    /// </summary>
    /// <param name="api">Authentication service that validates and refreshes tokens.</param>
    /// <param name="auth">Provider used to verify the refresh token.</param>
    /// <param name="jwt_handler">Handler responsible for generating new JWTs.</param>
    /// <param name="app_id">Identifier of the application.</param>
    /// <param name="user_refresh">Refresh token request payload.</param>
    /// <param name="ct">Token to observe cancellation requests.</param>
    /// <returns>New tokens on success or an authorization error.</returns>
    /// <exception cref="OperationCanceledException">Thrown when the operation is cancelled.</exception>
    Task<ActionResult> RefreshToken(IAuthenticationService api, IAuthenticationProvider auth, WebAPIJsonWebTokenHandler jwt_handler, string app_id, UserRefreshTokenRequest user_refresh, CancellationToken ct);
}
