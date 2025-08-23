using MicroM.DataDictionary.Entities.MicromUsers;
using MicroM.Web.Authentication;

namespace MicroM.Web.Services;

/// <summary>
/// Represents the IAuthenticationService.
/// </summary>
public interface IAuthenticationService
{
    /// <summary>
    /// Authenticates a user and issues a JWT for the Web API.
    /// </summary>
    /// <param name="auth">Authentication provider used to validate the credentials.</param>
    /// <param name="jwt_handler">Handler used to create the JSON Web Token.</param>
    /// <param name="app_id">Application identifier.</param>
    /// <param name="user_login">User login credentials.</param>
    /// <param name="server_claims">Additional claims added to the token.</param>
    /// <param name="ct">Cancellation token to observe.</param>
    /// <returns>Information about the authenticated user together with the generated token.</returns>
    /// <remarks>Throws <see cref="OperationCanceledException"/> when <paramref name="ct"/> is cancelled and may propagate exceptions from the authentication provider.</remarks>
    public Task<(LoginResult? user_data, TokenResult? token_result)> HandleLogin(IAuthenticationProvider auth, WebAPIJsonWebTokenHandler jwt_handler, string app_id, UserLogin user_login, Dictionary<string, object> server_claims, CancellationToken ct);

    /// <summary>
    /// Refreshes an expired token and returns a new JWT.
    /// </summary>
    /// <param name="auth">Authentication provider responsible for validating the refresh request.</param>
    /// <param name="jwt_handler">Token handler used to generate the new token.</param>
    /// <param name="app_id">Application identifier.</param>
    /// <param name="refreshRequest">Refresh request containing the expired token and refresh token.</param>
    /// <param name="ct">Cancellation token to observe.</param>
    /// <returns>A tuple containing the refresh operation result and the generated token, if successful.</returns>
    /// <remarks>Throws <see cref="OperationCanceledException"/> when cancelled and may propagate exceptions from the authentication provider.</remarks>
    public Task<(RefreshTokenResult? result, TokenResult? token_result)> HandleRefreshToken(IAuthenticationProvider auth, WebAPIJsonWebTokenHandler jwt_handler, string app_id, UserRefreshTokenRequest refreshRequest, CancellationToken ct);

    /// <summary>
    /// Logs the specified user off the system.
    /// </summary>
    /// <param name="auth">Authentication provider used to perform the logoff.</param>
    /// <param name="app_id">Application identifier.</param>
    /// <param name="user_name">Name of the user to log off.</param>
    /// <param name="ct">Cancellation token to observe.</param>
    /// <returns>A task that completes when the logoff process finishes.</returns>
    /// <remarks>Throws <see cref="OperationCanceledException"/> when cancelled and may propagate exceptions from the authentication provider.</remarks>
    public Task HandleLogoff(IAuthenticationProvider auth, string app_id, string user_name, CancellationToken ct);

    /// <summary>
    /// Sends a password recovery email to the specified user.
    /// </summary>
    /// <param name="auth">Authentication provider used to send the recovery email.</param>
    /// <param name="app_id">Application identifier.</param>
    /// <param name="user_name">Target user name.</param>
    /// <param name="ct">Cancellation token to observe.</param>
    /// <returns>A tuple indicating whether the operation failed and any error message.</returns>
    /// <remarks>Throws <see cref="OperationCanceledException"/> when cancelled and may propagate exceptions from the authentication provider or email service.</remarks>
    public Task<(bool failed, string? error_message)> HandleSendRecoveryEmail(IAuthenticationProvider auth, string app_id, string user_name, CancellationToken ct);

    /// <summary>
    /// Resets a user's password using a recovery code.
    /// </summary>
    /// <param name="auth">Authentication provider that performs the recovery.</param>
    /// <param name="app_id">Application identifier.</param>
    /// <param name="user_name">User whose password is being reset.</param>
    /// <param name="new_password">New password to set for the user.</param>
    /// <param name="recovery_code">Code provided to authorize the password reset.</param>
    /// <param name="ct">Cancellation token to observe.</param>
    /// <returns>A tuple indicating whether the operation failed and any error message.</returns>
    /// <remarks>Throws <see cref="OperationCanceledException"/> when cancelled and may propagate exceptions from the authentication provider.</remarks>
    public Task<(bool failed, string? error_message)> HandleRecoverPassword(IAuthenticationProvider auth, string app_id, string user_name, string new_password, string recovery_code, CancellationToken ct);

}
