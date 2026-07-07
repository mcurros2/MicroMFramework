using MicroM.Configuration;
using MicroM.Configuration.Entities;
using MicroM.Core;
using MicroM.Data;
using MicroM.Web.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Collections.Concurrent;

namespace MicroM.Web.Authentication;

public class SecretsAuthenticator(
    ILogger<SecretsAuthenticator> logger,
    IDeviceIdService deviceIdService,
    IHttpContextAccessor httpContextAccessor,
    IOptions<MicroMOptions> microm_config,
    IEmailService emailService,
    IMicroMEncryption encryptor) : IAuthenticator
{
    private readonly ILogger<SecretsAuthenticator> _log = logger;
    private readonly IDeviceIdService _deviceIdService = deviceIdService;
    private readonly IHttpContextAccessor _contextAccessor = httpContextAccessor;
    private readonly IOptions<MicroMOptions> _microm_config = microm_config;
    private readonly IEmailService _emailService = emailService;
    private readonly IMicroMEncryption _encryptor = encryptor;

    private static ConcurrentDictionary<string, AccountLockout> _lockout_cache = new(StringComparer.OrdinalIgnoreCase);

    public async Task<AuthenticatorResult> AuthenticateLogin(ApplicationOption app_config, UserLogin user_login, CancellationToken ct)
    {
        AuthenticatorResult result = new();
        if (string.IsNullOrEmpty(user_login.Username)) return result;

        // user_login.Username -> apiKey, user_login.Password -> secret
        _lockout_cache.TryGetValue($"{app_config.ApplicationID}.{user_login.Username}", out var account);

        if (account != null)
        {
            lock (account)
            {
                if (account.isAccountLocked())
                {
                    result.AccountLocked = true;
                    return result;
                }
            }
        }

        using DatabaseClient ec = app_config.CreateDatabaseClient(_log, _deviceIdService, null);

        try
        {

            var keys = new MicromApplicationApiKeys(ec, _encryptor, schema_name: ConfigurationDefaults.SchemaConfiguration.APPSchema);
            keys.Def.c_application_id.Value = app_config.ApplicationID;
            keys.Def.vc_apikey.Value = user_login.Username;

            // The get operation will decrypt the values for us
            var found = await keys.GetByAPIKey(ct);
            if (found)
            {
                if (keys.Def.vc_apikey.Value == user_login.Username && CryptClass.SecureEquals(keys.Def.vc_secret.Value, user_login.Password))
                {
                    result.PasswordVerificationResult = Microsoft.AspNetCore.Identity.PasswordVerificationResult.Success;
                    result.LoginData = new LoginData()
                    {
                        user_id = keys.Def.c_api_key_id.Value,
                        username = user_login.Username,
                        refresh_token = CryptClass.GenerateRandomBase64String()
                    };

                    account = _lockout_cache.GetOrAdd($"{app_config.ApplicationID}.{user_login.Username}", new AccountLockout());
                    lock (account)
                    {
                        account.unlockAccount();
                        account.setRefreshToken(result.LoginData.refresh_token);
                    }

                    result.ServerClaims.Add(MicroMServerClaimTypes.MicroMUser_id, result.LoginData.user_id);
                    result.ServerClaims.Add(MicroMServerClaimTypes.MicroMAPP_id, app_config.ApplicationID);
                    result.ServerClaims.Add(MicroMServerClaimTypes.MicroMUsername, user_login.Username);
                    result.ServerClaims.Add(MicroMServerClaimTypes.MicroMUserDeviceID, "secrets-client");
                    result.ServerClaims[MicroMServerClaimTypes.MicroMUserGroups] = (List<string>)[];
                }
                else
                {
                    account = _lockout_cache.GetOrAdd($"{app_config.ApplicationID}.{user_login.Username}", new AccountLockout());
                    lock (account)
                    {
                        account.incrementBadLogonAndLock();
                        result.AccountLocked = account.isAccountLocked();
                        result.PasswordVerificationResult = Microsoft.AspNetCore.Identity.PasswordVerificationResult.Failed;
                    }
                }
            }
            else
            {
                _log.LogWarning("SecretsAuthenticator: API key not found for application {app_id} and key {api_key}", app_config.ApplicationID, user_login.Username);
            }
        }
        catch (Exception ex)
        {
            _log.LogError(ex, "SecretsAuthenticator: error authenticating API key");
        }
        finally
        {
            await ec.Disconnect();
        }

        return result;
    }

    public Task<RefreshTokenResult> AuthenticateRefresh(ApplicationOption app_config, string user_id, string refresh_token, string local_device_id, CancellationToken ct)
    {
        RefreshTokenResult result = new();

        if (string.IsNullOrEmpty(refresh_token) || string.IsNullOrEmpty(user_id)) return Task.FromResult(result);

        _lockout_cache.TryGetValue($"{app_config.ApplicationID}.{user_id}", out var account);
        if (account != null)
        {
            lock (account)
            {
                if (!account.isAccountLocked())
                {
                    result.Status = account.validateRefreshToken(refresh_token, app_config.MaxRefreshTokenAttempts);
                    if (result.Status == LoginAttemptStatus.RefreshTokenValid)
                    {
                        result.RefreshToken = refresh_token;
                        result.RefreshExpiration = account.getRefreshExpiration();
                        account.incrementRefreshTokenValidationCount();
                    }
                    else
                    {
                        account.clearRefreshToken();
                    }
                }
                else
                {
                    result.Status = LoginAttemptStatus.AccountLocked;
                }
            }
        }

        return Task.FromResult(result);
    }

    public Task Logoff(ApplicationOption app_config, string user_name, CancellationToken ct)
    {
        if (string.IsNullOrEmpty(user_name)) throw new ArgumentException("Username is null or empty");
        _ = _lockout_cache.TryRemove($"{app_config.ApplicationID}.{user_name}", out _);
        return Task.CompletedTask;
    }

    public Task<ResultWithStatus<bool, string>> RecoverPassword(ApplicationOption app_config, string user_name, string new_password, string recovery_code, CancellationToken ct)
    {
        throw new NotImplementedException();
    }

    public Task<ResultWithStatus<bool, string>> SendPasswordRecoveryEmail(ApplicationOption app_config, string user_name, CancellationToken ct)
    {
        throw new NotImplementedException();
    }

    public void UnencryptClaims(Dictionary<string, object>? server_claims)
    {
        // nothing to unencrypt
    }

    public Task<ExternalSignInResult> HandleExternalSignIn(ApplicationOption app, ExternalIdentity identity, string deviceId, CancellationToken ct)
    {
        throw new NotImplementedException();
    }

    public Task<AuthenticatorResult> VerifyTwoFactorCode(ApplicationOption app_config, string challengeId, string code, CancellationToken ct)
    {
        throw new NotImplementedException("SecretsAuthenticator does not support two-factor authentication.");
    }
}
