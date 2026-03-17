using MicroM.ActiveDirectory;
using MicroM.Configuration;
using MicroM.Core;
using MicroM.Data;
using MicroM.DataDictionary.CategoriesDefinitions;
using MicroM.DataDictionary.Entities;
using MicroM.Extensions;
using MicroM.Web.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace MicroM.Web.Authentication;

public class ActiveDirectoryAuthenticator(
    ILogger<ActiveDirectoryAuthenticator> log,
    IDeviceIdService deviceIdService,
    IHttpContextAccessor httpContextAccessor,
    IOptions<MicroMOptions> microm_config
    ) : IAuthenticator
{
    private string GetRefreshCookieName(ApplicationOption app_config)
    {
        return $"m-{nameof(ActiveDirectoryAuthenticator)}-{app_config.ApplicationID}-r";
    }
    private string? ReadRefreshTokenFromCookie(ApplicationOption app_config)
    {
        string? refresh_token = null;
        var httpc = httpContextAccessor.HttpContext;
        httpc?.Request.Cookies.TryGetValue(GetRefreshCookieName(app_config), out refresh_token);
        return refresh_token;
    }

    private void WriteRefreshTokenToCookie(ApplicationOption app_config, string new_refresh_token)
    {
        var httpc = httpContextAccessor.HttpContext;
        httpc?.Response.Cookies.Append(GetRefreshCookieName(app_config), new_refresh_token, new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.Strict,
            Path = $"/{microm_config.Value.MicroMAPIBaseRootPath}/{app_config.ApplicationID}",
            Expires = DateTimeOffset.UtcNow.AddHours(app_config.JWTRefreshExpirationHours + 1)
        });
    }

    private void DeleteRefreshCookie(ApplicationOption app_config)
    {
        var httpc = httpContextAccessor.HttpContext;
        httpc?.Response.Cookies.Delete(GetRefreshCookieName(app_config));
    }

    public async Task<AuthenticatorResult> AuthenticateLogin(ApplicationOption app_config, UserLogin user_login, CancellationToken ct)
    {
        AuthenticatorResult result = new();
        if (string.IsNullOrEmpty(user_login.Username)) return result;

        var ad_config = app_config.GetADConfiguration(user_login.Username);
        if (ad_config == null) return result;

        using DatabaseClient ec = app_config.CreateDatabaseClient(log, deviceIdService, null);

        LoginData? login_data = null;
        try
        {

            var (ad_result, auth_result) = await ADCore.AuthenticateUserByEmailAsync(ad_config.ADServerIP, ad_config.ADUser, ad_config.ADPassword, ad_config.ADContainer, user_login.Username, user_login.Password, ad_config.ADUserPrincipalDomain);

            if (ad_result == null || auth_result == ADAuthenticationResult.InvalidUserDomain || auth_result == ADAuthenticationResult.UserNotAnEmail || auth_result == ADAuthenticationResult.UserNotFound)
            {
                log.LogDebug("AD authentication failed for user {username} with result {auth_result}", user_login.Username, auth_result);
                return result;
            }

            var (device_id, ipaddress, user_agent) = deviceIdService.GetDeviceID(user_login.LocalDeviceID);

            await ec.Connect(ct);

            // MMC: Get the data needed to perform the login attempt: password hash and account status
            login_data = await MicromUsers.GetUserData(user_login.Username, null, device_id, ec, ct);

            result.PasswordVerificationResult = auth_result == ADAuthenticationResult.Authenticated ? PasswordVerificationResult.Success : PasswordVerificationResult.Failed;

            if (login_data != null)
            {
                result.LoginData = login_data;
                if (login_data.disabled)
                {
                    result.AccountDisabled = true;
                }
                else if (login_data.locked)
                {
                    result.AccountLocked = true;
                }
            }
            else
            {
                if (ad_config.CreateUserOnLogin || ad_result.isDomainAdmin)
                {
                    var jitUser = new MicromUsers(ec);
                    jitUser.Def.vc_username.Value = ad_result.PrincipalName;
                    jitUser.Def.vc_email.Value = ad_result.PrincipalName;
                    jitUser.Def.c_usertype_id.Value = ad_result.isDomainAdmin ? nameof(UserTypes.ADMIN) : nameof(UserTypes.USER);
                    jitUser.Def.vc_password.Value = CryptClass.GenerateRandomBase64String();
                    jitUser.Def.vc_user_groups.Value = !ad_result.isDomainAdmin && !string.IsNullOrWhiteSpace(ad_config.DefaultUserGroupID) ? [ad_config.DefaultUserGroupID] : [];

                    var jti_result = await jitUser.InsertData(ct);
                    if (jti_result.IsNullOrFailed())
                    {
                        log.LogError("Failed to create user {username} after successful AD authentication. DBStatus: {@DBStatusResult}", user_login.Username, jti_result);
                        return result;
                    }

                    // Re-read freshly provisioned user
                    login_data = await MicromUsers.GetUserData(user_login.Username, null, device_id, ec, ct);

                    result.LoginData = login_data;
                    if (login_data!.disabled)
                    {
                        result.AccountDisabled = true;
                    }
                    else if (login_data.locked)
                    {
                        result.AccountLocked = true;
                    }
                }

                if (login_data == null)
                {
                    result.AccountNotProvisioned = true;
                    return result;
                }

            }

            string? new_refresh_token = CryptClass.GenerateRandomBase64String();

            // MMC: take note that the refresh token will be invalidated (null) if there is a bad login attempt
            var attempt_result = await MicromUsers.UpdateLoginAttempt(
                user_id: login_data.user_id,
                device_id: device_id,
                new_refresh_token: result.PasswordVerificationResult.IsIn(PasswordVerificationResult.Success, PasswordVerificationResult.SuccessRehashNeeded) ? new_refresh_token : login_data.refresh_token,
                success: result.PasswordVerificationResult.IsIn(PasswordVerificationResult.Success, PasswordVerificationResult.SuccessRehashNeeded),
                app_config.AccountLockoutMinutes,
                app_config.JWTRefreshExpirationHours,
                app_config.MaxBadLogonAttempts,
                ipaddress ?? "",
                user_agent ?? "",
                ec,
                ct
            );

            if (result.PasswordVerificationResult.IsIn(PasswordVerificationResult.Success, PasswordVerificationResult.SuccessRehashNeeded))
            {
                // MMC: save the refresh cookie. Note that Update login attempt will return a new refresh token if needed, the same or null. Update login attempt will also clear any refresh token if needed.
                if (!string.IsNullOrEmpty(attempt_result.RefreshToken)) WriteRefreshTokenToCookie(app_config, attempt_result.RefreshToken);

                // Get claims
                var (server_claims, client_claims) = await MicromUsers.GetClaims(user_login.Username, ec, ct);

                result.ClientClaims = client_claims;
                result.ServerClaims = server_claims;
            }

            result.ServerClaims[MicroMServerClaimTypes.MicroMUser_id] = login_data.user_id;
            result.ServerClaims[MicroMServerClaimTypes.MicroMAPP_id] = app_config.ApplicationID;
            result.ServerClaims[MicroMServerClaimTypes.MicroMUsername] = user_login.Username;
            result.ServerClaims[MicroMServerClaimTypes.MicroMUserType_id] = login_data.usertype_id ?? "";
            result.ServerClaims[MicroMServerClaimTypes.MicroMUserDeviceID] = device_id;

            // Json string array of user groups ids
            result.ServerClaims[MicroMServerClaimTypes.MicroMUserGroups] = login_data.user_groups ?? "[]";

        }
        finally
        {
            await ec.Disconnect();
        }

        return result;

    }

    public async Task<RefreshTokenResult> AuthenticateRefresh(ApplicationOption app_config, string user_id, string refresh_token, string local_device_id, CancellationToken ct)
    {
        RefreshTokenResult result = new();

        var (device_id, ipaddress, user_agent) = deviceIdService.GetDeviceID(local_device_id);

        string cookie_token = ReadRefreshTokenFromCookie(app_config) ?? "";

        if (!string.IsNullOrEmpty(cookie_token))
        {
            if (cookie_token != refresh_token)
            {
                log.LogWarning("Refresh token from cookie is different from the one in the request:  user_id: {user_id}, device_id: {device_id}, ipaddress: {ipaddress}, user-agent: {user_agent}. Taking cookie_token", user_id, device_id, ipaddress, user_agent);
            }
            refresh_token = cookie_token;
        }

        if (string.IsNullOrEmpty(user_id))
        {
            log.LogTrace("Refresh token: User id is null user_id: {user_id}, device_id: {device_id}, ipaddress: {ip}, user-agent: {ua}", user_id, device_id, ipaddress, user_agent);
            result.Status = LoginAttemptStatus.InvalidRefreshToken;
            return result;
        }

        if (string.IsNullOrEmpty(refresh_token))
        {
            log.LogTrace("Refresh token: User id is null user_id: {user_id}, device_id: {device_id}, ipaddress: {ip}, user-agent: {ua}", user_id, device_id, ipaddress, user_agent);
            result.Status = LoginAttemptStatus.InvalidRefreshToken;
            return result;
        }

        using DatabaseClient ec = app_config.CreateDatabaseClient(log, deviceIdService, null);

        try
        {
            await ec.Connect(ct);

            LoginData? login_data = await MicromUsers.GetUserData(null, user_id, device_id, ec, ct);
            if (login_data != null && !login_data.disabled && !login_data.refresh_expired && !login_data.locked && string.IsNullOrEmpty(login_data.refresh_token) == false)
            {
                var new_token = CryptClass.GenerateRandomBase64String();
                var validated_token = await MicromUsers.RefreshToken(user_id, device_id, refresh_token, new_token, app_config.JWTRefreshExpirationHours, app_config.MaxRefreshTokenAttempts, ec, ct);
                if (validated_token != null)
                {
                    result = validated_token;
                    if (result.Status.IsIn(LoginAttemptStatus.Updated, LoginAttemptStatus.RefreshTokenValid) && !string.IsNullOrEmpty(result.RefreshToken))
                    {
                        WriteRefreshTokenToCookie(app_config, result.RefreshToken);
                    }
                }
                else
                {
                    log.LogWarning("Refresh token proc returned null: user_id: {user_id}, device_id: {device_id}, ipaddress: {ipaddress}, user-agent: {user_agent}", user_id, device_id, ipaddress, user_agent);
                }
            }
            else
            {
                log.LogTrace("Can't refresh token. User Data: user_id: {user_id}, device_id: {device_id}, disabled: {disabled}, refresh_expired: {expired}, locked: {locked}", user_id, device_id, login_data?.disabled, login_data?.refresh_expired, login_data?.locked);
            }

        }
        catch (Exception ex)
        {
            log.LogError(ex, "Error refreshing token for user_id: {user_id}, device_id: {device_id}, ipaddress: {ipaddress}, user-agent: {user_agent}", user_id, device_id, ipaddress, user_agent);
            throw;
        }
        finally
        {
            await ec.Disconnect();
        }

        return result;
    }


    public async Task Logoff(ApplicationOption app_config, string user_name, CancellationToken ct)
    {
        if (string.IsNullOrEmpty(user_name)) throw new ArgumentException("Username is null or empty");

        using DatabaseClient ec = app_config.CreateDatabaseClient(log, deviceIdService, null);

        var _ = await MicromUsers.Logoff(user_name, ec, ct);
        DeleteRefreshCookie(app_config);
    }

    public void UnencryptClaims(Dictionary<string, object>? server_claims) { }

    public Task<ResultWithStatus<bool, string>> RecoverPassword(ApplicationOption app_config, string user_name, string new_password, string recovery_code, CancellationToken ct)
    {
        throw new NotImplementedException();
    }

    public Task<ResultWithStatus<bool, string>> SendPasswordRecoveryEmail(ApplicationOption app_config, string user_name, CancellationToken ct)
    {
        throw new NotImplementedException();
    }

    public Task<ExternalSignInResult> HandleExternalSignIn(ApplicationOption app, ExternalIdentity identity, string deviceId, CancellationToken ct)
    {
        throw new NotImplementedException();
    }


}
