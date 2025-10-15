using MicroM.Configuration;
using MicroM.Core;
using MicroM.DataDictionary.CategoriesDefinitions;
using MicroM.DataDictionary.Entities;
using MicroM.Extensions;
using MicroM.Web.Authentication;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using System.Globalization;
using System.Security.Claims;

namespace MicroM.Web.Services;

public class AuthenticationService(
            ILogger<AuthenticationService> log,
            IMicroMAppConfiguration app_config,
            IDeviceIdService deviceid_service,
            IMicroMEncryption encryptor
    ) : IAuthenticationService
{
    public async Task<(LoginResult? user_data, TokenResult? token_result)> HandleLogin(IAuthenticationProvider auth, WebAPIJsonWebTokenHandler jwt_handler, string app_id, UserLogin user_login, Dictionary<string, object> server_claims, CancellationToken ct)
    {
        if (string.IsNullOrEmpty(user_login.Username)) return (null, null);

        ApplicationOption? app = app_config.GetAppConfiguration(app_id);

        if (app == null)
        {
            return (null, null);
        }

        if (app.IdentityProviderRoleType == nameof(IdentityProviderRole.IDPServer) && app.OIDCIdPsubjectPepper.IsNullOrEmpty())
        {
            log.LogError("LOGIN: APP_ID {app_id} User: {username} OIDCIdPsubjectPepper is not configured, cannot create OIDC session", app_id, user_login.Username);
            return (null, null);
        }

        TokenResult? token_result = null;
        AuthenticatorResult? authenticatorResult = null;
        string? oidc_session_id = null;

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
                    if (app.IdentityProviderRoleType == nameof(IdentityProviderRole.IDPServer))
                    {
                        using var dbc = app.CreateDatabaseClient(log, deviceid_service, null);
                        try
                        {
                            await dbc.Connect(ct);

                            var device_id = authenticatorResult.ServerClaims[MicroMServerClaimTypes.MicroMUserDeviceID].ToString() ?? "";

                            if (string.IsNullOrEmpty(device_id))
                            {
                                log.LogWarning("LOGIN: APP_ID {app_id} User: {username} empty device_id", app_id, authenticatorResult.LoginData.username);
                            }

                            string new_session_id = await ApplicationOidcActiveSessions.CreateIdPSession(dbc, app.ApplicationID, authenticatorResult.LoginData.username, authenticatorResult.LoginData.user_id, device_id, app.OIDCIdPsubjectPepper!, encryptor, ct);

                            authenticatorResult.ServerClaims[MicroMServerClaimTypes.MicroMOidcSessionID] = new_session_id;

                        }
                        finally
                        {
                            await dbc.Disconnect();
                        }
                    }

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
                authenticator_result = authenticatorResult,
                oidc_session_id = oidc_session_id
            };

        }

        return (result, token_result);
    }

    public async Task HandleLogoff(IAuthenticationProvider auth, string app_id, string user_name, string user_id, CancellationToken ct)
    {
        ApplicationOption app = app_config.GetAppConfiguration(app_id)!;

        var authenticator = auth.GetAuthenticator(app);
        if (authenticator != null)
        {
            await authenticator.Logoff(app, user_name, ct);

            if (app.IdentityProviderRoleType == nameof(IdentityProviderRole.IDPServer))
            {
                // TODO: trigger SLO to clients
            }
        }
        else
        {
            log.LogError("LOGOFF: Invalid authenticator specified: {authenticator} APP_ID {app_id}", app_id, app.AuthenticationType);
        }
    }

    public async Task<ResultWithStatus<bool, string>> HandleRecoverPassword(IAuthenticationProvider auth, string app_id, string user_name, string new_password, string recovery_code, CancellationToken ct)
    {
        ApplicationOption? app = app_config.GetAppConfiguration(app_id);
        if (app == null)
        {
            log.LogTrace("RECOVER_PASSWORD: Invalid APP_ID {app_id}", app_id);
            return new(true, null);
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
            return new(true, null);
        }
    }

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

    public async Task<ResultWithStatus<bool, string>> HandleSendRecoveryEmail(IAuthenticationProvider auth, string app_id, string user_name, CancellationToken ct)
    {
        ApplicationOption? app = app_config.GetAppConfiguration(app_id);
        if (app == null)
        {
            log.LogTrace("RECOVERY_EMAIL: Invalid APP_ID {app_id}", app_id);
            return new(true, null);
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
            return new(true, null);
        }

    }

    public async Task<Dictionary<string, string>> SignInAsync(HttpContext httpc, TokenResult token_result, string refresh_token)
    {
        if (token_result.SD == null) throw new Exception("SecurityTokenDescriptor is null");
        if (token_result.Token == null) throw new Exception("token is null");
        if (token_result.SD.Expires == null) throw new Exception("Expires is null");

        var claimsIdentity = new ClaimsIdentity(token_result.SD.Claims.ToClaims(), CookieAuthenticationDefaults.AuthenticationScheme);

        await httpc.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(claimsIdentity), new AuthenticationProperties()
        {
            ExpiresUtc = token_result.SD.Expires
        });

        string expires_in = ((int)(token_result.SD.Expires - DateTime.UtcNow).Value.TotalSeconds).ToString(CultureInfo.InvariantCulture);
        return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase) {
                    { "access_token", token_result.Token }
                    , { "token_type", "Bearer" }
                    , { "expires_in", expires_in }
                    , { "refresh-token", refresh_token}
                };

    }


}
