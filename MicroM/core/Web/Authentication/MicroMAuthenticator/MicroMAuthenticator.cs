using MicroM.Configuration;
using MicroM.Core;
using MicroM.Data;
using MicroM.DataDictionary;
using MicroM.DataDictionary.Entities.MicromUsers;
using MicroM.Extensions;
using MicroM.Web.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace MicroM.Web.Authentication;

public class MicroMAuthenticator : IAuthenticator
{
    private readonly ILogger<MicroMAuthenticator> _log;
    private readonly IDeviceIdService _deviceIdService;
    private readonly IHttpContextAccessor _contextAccessor;
    private readonly IOptions<MicroMOptions> _microm_config;
    private readonly IEmailService _emailService;

    public MicroMAuthenticator(ILogger<MicroMAuthenticator> logger, IDeviceIdService deviceIdService, IHttpContextAccessor httpContextAccessor, IOptions<MicroMOptions> microm_config, IEmailService emailService)
    {
        _log = logger;
        _deviceIdService = deviceIdService;
        _contextAccessor = httpContextAccessor;
        _microm_config = microm_config;
        _emailService = emailService;
    }

    private string GetRefreshCookieName(ApplicationOption app_config)
    {
        return $"m-{nameof(MicroMAuthenticator)}-{app_config.ApplicationID}-r";
    }

    private string? ReadRefreshTokenFromCookie(ApplicationOption app_config)
    {
        string? refresh_token = null;
        var httpc = _contextAccessor.HttpContext;
        httpc?.Request.Cookies.TryGetValue(GetRefreshCookieName(app_config), out refresh_token);
        return refresh_token;
    }

    private void WriteRefreshTokenToCookie(ApplicationOption app_config, string new_refresh_token)
    {
        var httpc = _contextAccessor.HttpContext;
        httpc?.Response.Cookies.Append(GetRefreshCookieName(app_config), new_refresh_token, new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.Strict,
            Path = $"/{_microm_config.Value.MicroMAPIBaseRootPath}/{app_config.ApplicationID}",
            Expires = DateTimeOffset.UtcNow.AddHours(app_config.JWTRefreshExpirationHours + 1)
        });
    }

    private void DeleteRefreshCookie(ApplicationOption app_config)
    {
        var httpc = _contextAccessor.HttpContext;
        httpc?.Response.Cookies.Delete(GetRefreshCookieName(app_config));
    }

    public async Task<AuthenticatorResult> AuthenticateLogin(ApplicationOption app_config, UserLogin user_login, CancellationToken ct)
    {
        AuthenticatorResult result = new();
        if (string.IsNullOrEmpty(user_login.Username)) return result;

        using DatabaseClient ec = new(server: app_config.SQLServer, user: app_config.SQLUser, password: app_config.SQLPassword, db: app_config.SQLDB);

        LoginData? login_data = null;
        try
        {
            await ec.Connect(ct);

            var (device_id, ipaddress, user_agent) = _deviceIdService.GetDeviceID(user_login.LocalDeviceID);

            // MMC: Get the data needed to perform the login attempt: password hash and account status
            login_data = await MicromUsers.GetUserData(user_login.Username, null, device_id, ec, ct);

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
                else
                {
                    PasswordVerificationResult ret = UserPasswordHasher.VerifyPassword(user_login, login_data.pwhash, user_login.Password);
                    result.PasswordVerificationResult = ret;


                    string? new_refresh_token = CryptClass.GenerateRandomBase64String();

                    // MMC: take note that the refresh token will be invalidated (null) if there is a bad login attempt
                    var attempt_result = await MicromUsers.UpdateLoginAttempt(
                        login_data.user_id,
                        device_id,
                        result.PasswordVerificationResult.IsIn(PasswordVerificationResult.Success, PasswordVerificationResult.SuccessRehashNeeded) ? new_refresh_token : login_data.refresh_token,
                        result.PasswordVerificationResult.IsIn(PasswordVerificationResult.Success, PasswordVerificationResult.SuccessRehashNeeded),
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

            }

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

        var (device_id, ipaddress, user_agent) = _deviceIdService.GetDeviceID(local_device_id);

        string cookie_token = ReadRefreshTokenFromCookie(app_config) ?? "";

        if (!string.IsNullOrEmpty(cookie_token))
        {
            if (cookie_token != refresh_token)
            {
                _log.LogWarning("Refresh token from cookie is different from the one in the request:  cookie [{cookie_token}] request : [{refresh_token}] user_id: {user_id}, device_id: {device_id}, ipaddress: {ipaddress}, user-agent: {user_agent}. Taking cookie_token", cookie_token, refresh_token, user_id, device_id, ipaddress, user_agent);
            }
            refresh_token = cookie_token;
        }

        if (string.IsNullOrEmpty(user_id))
        {
            _log.LogTrace("Refresh token: User id is null {refresh_token} user_id: {user_id}, device_id: {device_id}, ipaddress: {ipaddress}, user-agent: {user_agent}", refresh_token, user_id, device_id, ipaddress, user_agent);
            result.Status = LoginAttemptStatus.InvalidRefreshToken;
            return result;
        }

        if (string.IsNullOrEmpty(refresh_token))
        {
            _log.LogTrace("Refresh token is null or empty token: {refresh_token} user_id: {user_id}, device_id: {device_id}, ipaddress: {ipaddress}, user-agent: {user_agent}", refresh_token, user_id, device_id, ipaddress, user_agent);
            result.Status = LoginAttemptStatus.InvalidRefreshToken;
            return result;
        }

        using DatabaseClient ec = new(server: app_config.SQLServer, user: app_config.SQLUser, password: app_config.SQLPassword, db: app_config.SQLDB);

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
                    _log.LogWarning("Refresh token proc returned null: {refresh_token} user_id: {user_id}, device_id: {device_id}, ipaddress: {ipaddress}, user-agent: {user_agent}", refresh_token, user_id, device_id, ipaddress, user_agent);
                }
            }
            else
            {
                _log.LogTrace("Can't refresh token. User Data: {refresh_token} user_id: {user_id}, device_id: {device_id}, ipaddress: {ipaddress}, user-agent: {user_agent}, disabled: {disabled}, refresh_expired: {expired}, locked: {locked}", refresh_token, user_id, device_id, ipaddress, user_agent, login_data?.disabled, login_data?.refresh_expired, login_data?.locked);
            }

        }
        catch (Exception ex)
        {
            _log.LogError(ex, "Error refreshing token for user_id: {user_id}, device_id: {device_id}, ipaddress: {ipaddress}, user-agent: {user_agent}", user_id, device_id, ipaddress, user_agent);
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
        using DatabaseClient ec = new(server: app_config.SQLServer, user: app_config.SQLUser, password: app_config.SQLPassword, db: app_config.SQLDB);
        var _ = await MicromUsers.Logoff(user_name, ec, ct);
        DeleteRefreshCookie(app_config);
    }

    public void UnencryptClaims(Dictionary<string, object>? server_claims) { }

    public async Task<(bool failed, string? error_message)> SendPasswordRecoveryEmail(ApplicationOption app_config, string user_name, CancellationToken ct)
    {
        if (string.IsNullOrEmpty(user_name)) throw new ArgumentException("Username is null or empty");

        using DatabaseClient ec = new(server: app_config.SQLServer, user: app_config.SQLUser, password: app_config.SQLPassword, db: app_config.SQLDB);

        try
        {
            var app_id = app_config.ApplicationID;

            await ec.Connect(ct);

            var templates = new EmailServiceTemplates(ec);
            templates.Def.c_email_template_id.Value = IAuthenticator.AuthenticatorRecoveryEmailTemplateID;
            await templates.GetData(ct);

            if (string.IsNullOrEmpty(templates.Def.vc_template_body.Value) || string.IsNullOrEmpty(templates.Def.vc_template_subject.Value))
            {
                _log.LogWarning("{app_id} No email template found for recovery email", app_id);
                return (failed: true, "No email template found for recovery email");
            }

            var emails = await MicromUsers.GetRecoveryEmails(user_name, ec, ct);
            if (emails == null || emails.Count == 0)
            {
                _log.LogWarning("{app_id} No emails found for user: {user_name}", app_id, user_name);
                return (failed: true, $"No emails found for user: {user_name}");
            }

            var get_code = await MicromUsers.GetRecoveryCode(user_name, ec, ct);
            if (string.IsNullOrEmpty(get_code.recovery_code))
            {
                _log.LogWarning("{app_id} Can't get a recovery code for user: {user_name} {error}", app_id, user_name, get_code.error);
                return (failed: true, $"Can't get a recovery code for user: {user_name} {get_code.error}");
            }

            EmailServiceTags recovery_tag = new() { tag = IAuthenticator.AuthenticatorRecoveryEmailTemplateCodeTAG, value = get_code.recovery_code };
            EmailServiceDestination[] destinations = emails.Select(e =>
            new EmailServiceDestination
            {
                reference_id = Guid.NewGuid().ToString(),
                destination_name = "",
                destination_email = e,
                tags = [recovery_tag]
            }).ToArray();

            var email = new EmailServiceItem()
            {
                EmailServiceConfigurationId = app_id,
                SubjectTemplate = templates.Def.vc_template_subject.Value,
                MessageTemplate = templates.Def.vc_template_body.Value,
                Destinations = destinations,
            };

            await _emailService.QueueEmail(app_id, email, ct, true);

            return (failed: false, error_message: null);
        }
        finally
        {
            await ec.Disconnect();
        }
    }

    public async Task<(bool failed, string? error_message)> RecoverPassword(ApplicationOption app_config, string user_name, string new_password, string recovery_code, CancellationToken ct)
    {
        if (string.IsNullOrEmpty(user_name) || string.IsNullOrEmpty(new_password) || string.IsNullOrEmpty(recovery_code)) throw new ArgumentException("Username, new password or recovery code is null or empty");

        using DatabaseClient ec = new(server: app_config.SQLServer, user: app_config.SQLUser, password: app_config.SQLPassword, db: app_config.SQLDB);

        try
        {
            var result = await MicromUsers.RecoverPassword(user_name, recovery_code, new_password, ec, ct);
            if (result != null)
            {
                if (result.Failed)
                {
                    _log.LogWarning("{app_id} Failed to recover password for user: {user_name} {error}", app_config.ApplicationID, user_name, result.Results?[0].Message);
                    return (failed: true, result.Results?[0].Message);
                }
                return (failed: false, error_message: null);
            }
            else
            {
                _log.LogWarning("{app_id} Failed to recover password for user: {user_name} {error}", app_config.ApplicationID, user_name, "No result");
                return (failed: true, null);
            }
        }
        finally
        {
            await ec.Disconnect();
        }
    }
}
