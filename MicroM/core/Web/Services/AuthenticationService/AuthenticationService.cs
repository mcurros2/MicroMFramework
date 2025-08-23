using MicroM.Configuration;
using MicroM.DataDictionary.Entities.MicromUsers;
using MicroM.Extensions;
using MicroM.Web.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using System.Security.Claims;

namespace MicroM.Web.Services;

/// <summary>
/// Represents the AuthenticationService.
/// </summary>
public class AuthenticationService(
            ILogger<AuthenticationService> log,
            IMicroMAppConfiguration app_config
    ) : IAuthenticationService
{
    /// <summary>
    /// Authenticates a user and issues a JWT for the Web API.
    /// </summary>
    /// <param name="auth">Authentication provider used to validate the user credentials.</param>
    /// <param name="jwt_handler">Token handler used to generate the JSON Web Token.</param>
    /// <param name="app_id">Application identifier.</param>
    /// <param name="user_login">User login request.</param>
    /// <param name="server_claims">Claims to merge with the authenticator claims.</param>
    /// <param name="ct">Token to observe for cancellation.</param>
    /// <returns>The authenticated user information and generated token, if successful; otherwise <see langword="null"/> values.</returns>
    /// <remarks>Throws <see cref="OperationCanceledException"/> when <paramref name="ct"/> is cancelled and may propagate exceptions from the authentication provider.</remarks>
    public async Task<(LoginResult? user_data, TokenResult? token_result)> HandleLogin(IAuthenticationProvider auth, WebAPIJsonWebTokenHandler jwt_handler, string app_id, UserLogin user_login, Dictionary<string, object> server_claims, CancellationToken ct)
    {
        if (string.IsNullOrEmpty(user_login.Username)) return (null, null);

        ApplicationOption? app = app_config.GetAppConfiguration(app_id);

        if (app == null)
        {
            return (null, null);
        }


        TokenResult? token_result = null;
        AuthenticatorResult? authenticatorResult = null;

        var authenticator = auth.GetAuthenticator(app);
        if (authenticator != null)
        {
            authenticatorResult = await authenticator.AuthenticateLogin(app, user_login, ct);

            if (authenticatorResult.AccountDisabled)
            {
                log.LogWarning("ACCOUNT_DISABLED: APP_ID {app_id} User: {username}", app_id, user_login.Username);
            }
            else if (authenticatorResult.AccountLocked)
            {
                log.LogWarning("ACCOUNT_LOCKOUT: APP_ID {app_id} User: {username} Locked minutes remaining: {lockout_mins}", app_id, user_login.Username, authenticatorResult.LoginData?.locked_minutes_remaining);
            }
            else
            {
                if (authenticatorResult.PasswordVerificationResult.IsIn(PasswordVerificationResult.Success, PasswordVerificationResult.SuccessRehashNeeded) && authenticatorResult.LoginData != null)
                {
                    var merged_claims = authenticatorResult.ServerClaims.Concat(server_claims)
                                  .GroupBy(i => i.Key)
                                  .ToDictionary(g => g.Key, g => g.First().Value);

                    token_result = jwt_handler.GenerateJwtTokenWEBApi(merged_claims, app);
                }
            }
        }
        else
        {
            log.LogError("LOGIN: Invalid authenticator specified: {authenticator} APP_ID {app_id}", app.AuthenticationType, app_id);
        }

        LoginResult? result = null;
        if (authenticatorResult?.LoginData != null)
        {
            result = new()
            {
                email = authenticatorResult.LoginData.email,
                refresh_token = authenticatorResult.LoginData.refresh_token,
                username = authenticatorResult.LoginData.username,
                client_claims = authenticatorResult.ClientClaims,
                authenticator_result = authenticatorResult
            };

        }

        return (result, token_result);
    }

    /// <summary>
    /// Logs the specified user off the system.
    /// </summary>
    /// <param name="auth">Authentication provider used to perform the logoff.</param>
    /// <param name="app_id">Application identifier.</param>
    /// <param name="user_name">Name of the user to log off.</param>
    /// <param name="ct">Token to observe for cancellation.</param>
    /// <returns>A task that represents the asynchronous logoff operation.</returns>
    /// <remarks>Throws <see cref="OperationCanceledException"/> when cancelled and may propagate exceptions from the authentication provider.</remarks>
    public async Task HandleLogoff(IAuthenticationProvider auth, string app_id, string user_name, CancellationToken ct)
    {
        ApplicationOption app = app_config.GetAppConfiguration(app_id)!;

        var authenticator = auth.GetAuthenticator(app);
        if (authenticator != null)
        {
            await authenticator.Logoff(app, user_name, ct);
        }
        else
        {
            log.LogError("LOGOFF: Invalid authenticator specified: {authenticator} APP_ID {app_id}", app_id, app.AuthenticationType);
        }
    }

    /// <summary>
    /// Resets a user's password using a recovery code.
    /// </summary>
    /// <param name="auth">Authentication provider that performs the recovery.</param>
    /// <param name="app_id">Application identifier.</param>
    /// <param name="user_name">User whose password is being reset.</param>
    /// <param name="new_password">New password to set for the user.</param>
    /// <param name="recovery_code">Code provided to authorize the reset.</param>
    /// <param name="ct">Token to observe for cancellation.</param>
    /// <returns>A tuple indicating whether the operation failed and any error message.</returns>
    /// <remarks>Throws <see cref="OperationCanceledException"/> when cancelled and may propagate exceptions from the authentication provider.</remarks>
    public async Task<(bool failed, string? error_message)> HandleRecoverPassword(IAuthenticationProvider auth, string app_id, string user_name, string new_password, string recovery_code, CancellationToken ct)
    {
        ApplicationOption? app = app_config.GetAppConfiguration(app_id);
        if (app == null)
        {
            log.LogTrace("RECOVER_PASSWORD: Invalid APP_ID {app_id}", app_id);
            return (failed: true, error_message: null);
        }

        var authenticator = auth.GetAuthenticator(app);
        if (authenticator != null)
        {
            var result = await authenticator.RecoverPassword(app, user_name, new_password, recovery_code, ct);
            return result;
        }
        else
        {
            log.LogError("RECOVER_PASSWORD: Invalid authenticator specified: {authenticator} APP_ID {app_id}", app.AuthenticationType, app_id);
            return (failed: true, null);
        }
    }

    /// <summary>
    /// Refreshes an expired token and returns a new JWT.
    /// </summary>
    /// <param name="auth">Authentication provider responsible for validating the refresh request.</param>
    /// <param name="jwt_handler">Token handler used to generate the new token.</param>
    /// <param name="app_id">Application identifier.</param>
    /// <param name="refreshRequest">Refresh request containing the expired token and refresh token.</param>
    /// <param name="ct">Token to observe for cancellation.</param>
    /// <returns>A tuple with the refresh operation result and new token, if successful.</returns>
    /// <remarks>Throws <see cref="OperationCanceledException"/> when cancelled and may propagate exceptions from the authentication provider.</remarks>
    public async Task<(RefreshTokenResult? result, TokenResult? token_result)> HandleRefreshToken(IAuthenticationProvider auth, WebAPIJsonWebTokenHandler jwt_handler, string app_id, UserRefreshTokenRequest refreshRequest, CancellationToken ct)
    {
        ApplicationOption? app = app_config.GetAppConfiguration(app_id);
        if (app == null)
        {
            log.LogTrace("REFRESH_TOKEN: Invalid APP_ID {app_id}", app_id);
            return (null, null);
        }

        // MMC: read the expired token to get the claims
        var claims = await jwt_handler.ValidateExpiredToken(app, refreshRequest.Bearer);
        if (claims != null)
        {
            var user_id = claims.FindFirstValue(MicroMServerClaimTypes.MicroMUser_id);
            if (user_id != null)
            {
                var authenticator = auth.GetAuthenticator(app);
                if (authenticator != null)
                {
                    var device_id = claims.FindFirstValue(MicroMServerClaimTypes.MicroMUserDeviceID);
                    if (device_id != null)
                    {
                        var refresh_result = await authenticator.AuthenticateRefresh(app, user_id, refreshRequest.RefreshToken, device_id, ct);

                        if (refresh_result.Status.IsIn(LoginAttemptStatus.Updated, LoginAttemptStatus.RefreshTokenValid))
                        {
                            var dicClaim = claims.Claims.GroupBy(claim => claim.Type).ToDictionary(group => group.Key, group => (object)group.Last().Value);
                            return (refresh_result, jwt_handler.GenerateJwtTokenWEBApi(dicClaim, app));
                        }
                        else
                        {
                            log.LogTrace("REFRESH_TOKEN: APP_ID {app_id} can't refresh token. Status: {status} Message {message} refresh-token: {token}", app_id, refresh_result.Status, refresh_result.Message, refreshRequest.RefreshToken);
                        }

                    }
                    else
                    {
                        log.LogTrace("REFRESH_TOKEN: APP_ID {app_id} empty device_id. refresh-token: {token}", app_id, refreshRequest.RefreshToken);
                    }
                }
                else
                {
                    log.LogError("REFRESH_TOKEN: Invalid authenticator specified: {authenticator} APP_ID {app_id}", app.AuthenticationType, app_id);
                }
            }
            else
            {
                log.LogWarning("REFRESH_TOKEN: APP_ID {app_id} can't find userID in claims or expired token", app_id);
            }
        }
        else
        {
            log.LogTrace("REFRESH_TOKEN: APP_ID {app_id} Invalid expired token", app_id);
        }

        return (null, null);
    }

    /// <summary>
    /// Sends a password recovery email to the specified user.
    /// </summary>
    /// <param name="auth">Authentication provider used to send the recovery email.</param>
    /// <param name="app_id">Application identifier.</param>
    /// <param name="user_name">Target user name.</param>
    /// <param name="ct">Token to observe for cancellation.</param>
    /// <returns>A tuple indicating whether the operation failed and any error message.</returns>
    /// <remarks>Throws <see cref="OperationCanceledException"/> when cancelled and may propagate exceptions from the authentication provider or email service.</remarks>
    public async Task<(bool failed, string? error_message)> HandleSendRecoveryEmail(IAuthenticationProvider auth, string app_id, string user_name, CancellationToken ct)
    {
        ApplicationOption? app = app_config.GetAppConfiguration(app_id);
        if (app == null)
        {
            log.LogTrace("RECOVERY_EMAIL: Invalid APP_ID {app_id}", app_id);
            return (failed: true, error_message: null);
        }

        var authenticator = auth.GetAuthenticator(app);
        if (authenticator != null)
        {
            var result = await authenticator.SendPasswordRecoveryEmail(app, user_name, ct);
            return result;
        }
        else
        {
            log.LogError("RECOVERY_EMAIL: Invalid authenticator specified: {authenticator} APP_ID {app_id}", app.AuthenticationType, app_id);
            return (failed: true, null);
        }

    }
}
