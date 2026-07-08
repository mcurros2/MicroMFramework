using MicroM.Configuration;
using MicroM.Core;
using MicroM.Data;
using MicroM.Database;
using MicroM.DataDictionary.CategoriesDefinitions;
using MicroM.Web.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Collections.Concurrent;

namespace MicroM.Web.Authentication;

public class SQLServerAuthenticator : IAuthenticator
{
    private readonly ILogger<SQLServerAuthenticator> _log;
    private readonly IMicroMEncryption _encryptor;
    private readonly IHttpContextAccessor _contextAccessor;
    private readonly IOptions<MicroMOptions> _microm_config;
    private readonly ITwoFactorChallengeStore _challengeStore;
    private readonly ITotpService _totpService;
    private readonly IMicroMAppConfiguration _app_config;


    private const string TwoFactorProvider = "Authenticator";
    private const string ChallengePasswordMetadataKey = "password";

    public SQLServerAuthenticator(
        ILogger<SQLServerAuthenticator> logger,
        IMicroMEncryption encryptor,
        IHttpContextAccessor contextAccessor,
        IOptions<MicroMOptions> microm_config,
        ITwoFactorChallengeStore challengeStore,
        ITotpService totpService,
        IMicroMAppConfiguration appConfiguration
        )
    {
        _log = logger;
        _encryptor = encryptor;
        _contextAccessor = contextAccessor;
        _microm_config = microm_config;
        _challengeStore = challengeStore;
        _totpService = totpService;
        _app_config = appConfiguration;
    }

    private static ConcurrentDictionary<string, AccountLockout> _lockout_cache = new(StringComparer.OrdinalIgnoreCase);

    private string GetRefreshCookieName(ApplicationOption app_config)
    {
        return $"m-{nameof(SQLServerAuthenticator)}-{app_config.ApplicationID}-r";
    }

    private void WriteRefreshTokenToCookie(ApplicationOption app_config, string new_refresh_token)
    {
        var httpc = _contextAccessor.HttpContext;
        httpc?.Response.Cookies.Append(GetRefreshCookieName(app_config), new_refresh_token, new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            Expires = DateTimeOffset.UtcNow.AddHours(app_config.JWTRefreshExpirationHours + 1),
            SameSite = SameSiteMode.Strict,
            Path = $"/{_microm_config.Value.MicroMAPIBaseRootPath}/{app_config.ApplicationID}",

        });
    }

    private void DeleteRefreshCookie(ApplicationOption app_config)
    {
        var httpc = _contextAccessor.HttpContext;
        httpc?.Response.Cookies.Delete(GetRefreshCookieName(app_config));
    }

    private static string GetLockoutKey(ApplicationOption app_config, string username)
    {
        return $"{app_config.ApplicationID}.{username}";
    }

    private void SetTwoFactorChallengeResult(ApplicationOption app_config, UserLogin user_login, AuthenticatorResult result)
    {
        string encryptedPassword = _encryptor.Encrypt(user_login.Password);
        string challengeId = _challengeStore.CreateChallenge(
            user_login.Username,
            user_login.Username,
            "none",
            app_config.ApplicationID,
            user_login.LocalDeviceID ?? "",
            new(StringComparer.OrdinalIgnoreCase)
            {
                [ChallengePasswordMetadataKey] = encryptedPassword
            });

        result.RequiresTwoFactor = true;
        result.TwoFactorProvider = TwoFactorProvider;
        result.TwoFactorChallengeId = challengeId;

        result.ServerClaims[MicroMServerClaimTypes.MicroMUser_id] = user_login.Username;
        result.ServerClaims[MicroMServerClaimTypes.MicroMAPP_id] = app_config.ApplicationID;
        result.ServerClaims[MicroMServerClaimTypes.MicroMUsername] = user_login.Username;
        result.ServerClaims[MicroMServerClaimTypes.MicroMUserType_id] = nameof(UserTypes.ADMIN);
        result.ServerClaims[MicroMServerClaimTypes.MicroMUserDeviceID] = "none";
        result.ServerClaims[MicroMServerClaimTypes.MicroMUserGroups] = (List<string>)[];
    }

    private async Task<bool> EnsureSqlAdminTotpSecret(ApplicationOption app_config, CancellationToken ct)
    {
        if (!string.IsNullOrWhiteSpace(app_config.SQLAdminTotpSecret)) return true;

        string secret = _totpService.GenerateSecret();

        if (app_config.ApplicationID.Equals(ConfigurationDefaults.ControlPanelAppID, StringComparison.OrdinalIgnoreCase))
        {
            string configPath = Path.Combine(ConfigurationDefaults.SecretsFilePath, ConfigurationDefaults.MicroMCommonID);
            string configFile = Path.Combine(configPath, ConfigurationDefaults.SecretsFilename);
            if (!File.Exists(configFile))
            {
                _log.LogWarning("SQL admin TOTP secret for Control Panel could not be created because the secrets file does not exist.");
                return false;
            }

            string encrypted = await File.ReadAllTextAsync(configFile, ct);
            SecretsOptions? secrets = _encryptor.DecryptObject<SecretsOptions>(encrypted);
            if (secrets == null)
            {
                _log.LogWarning("SQL admin TOTP secret for Control Panel could not be created because the secrets file could not be decrypted.");
                return false;
            }

            secrets.ControlPanelAdminTotpSecret = secret;
            Directory.CreateDirectory(configPath);
            await File.WriteAllTextAsync(configFile, _encryptor.EncryptObject(secrets), ct);
            app_config.SQLAdminTotpSecret = secret;
            return true;
        }

        var controlPanel = _app_config.GetAppConfiguration(ConfigurationDefaults.ControlPanelAppID);
        if (controlPanel == null || string.IsNullOrWhiteSpace(controlPanel.SQLServer) || string.IsNullOrWhiteSpace(controlPanel.SQLDB))
        {
            _log.LogWarning("SQL admin TOTP secret for APP_ID {app_id} could not be created because the configuration database is unavailable.", app_config.ApplicationID);
            return false;
        }

        using var configDb = controlPanel.CreateDatabaseClient(_log, null, null);
        var result = await global::MicroM.Configuration.Entities.Applications.SetAdminTotpSecret(app_config, secret, configDb, _encryptor, ct);
        if (result.Failed)
        {
            _log.LogWarning("SQL admin TOTP secret for APP_ID {app_id} could not be created. Result: {result}", app_config.ApplicationID, result.Results?.FirstOrDefault()?.Message);
            return false;
        }

        return true;
    }

    private void FinalizeSuccessfulLogin(ApplicationOption app_config, string username, string encryptedPassword, AuthenticatorResult result, bool isAdmin)
    {
        string refreshToken = CryptClass.GenerateRandomBase64String();
        result.LoginData = new()
        {
            user_id = username,
            username = username,
            refresh_token = refreshToken
        };
        result.PasswordVerificationResult = PasswordVerificationResult.Success;

        var account = _lockout_cache.GetOrAdd(GetLockoutKey(app_config, username), new AccountLockout());
        lock (account)
        {
            account.unlockAccount();
            account.setRefreshToken(refreshToken);
            WriteRefreshTokenToCookie(app_config, refreshToken);
        }

        result.ServerClaims[MicroMServerClaimTypes.MicroMUser_id] = username;
        result.ServerClaims[MicroMServerClaimTypes.MicroMAPP_id] = app_config.ApplicationID;
        result.ServerClaims[MicroMServerClaimTypes.MicroMUsername] = username;
        result.ServerClaims[MicroMServerClaimTypes.MicroMUserType_id] = isAdmin ? nameof(UserTypes.ADMIN) : nameof(UserTypes.USER);
        result.ServerClaims[MicroMServerClaimTypes.MicroMUserDeviceID] = "none";
        result.ServerClaims[MicroMServerClaimTypes.MicroMUserGroups] = (List<string>)[];
        result.ServerClaims[MicroMServerClaimTypes.MicroMPassword] = encryptedPassword;
    }

    public async Task<AuthenticatorResult> AuthenticateLogin(ApplicationOption app_config, UserLogin user_login, CancellationToken ct)
    {
        AuthenticatorResult result = new();
        if (string.IsNullOrEmpty(user_login.Username)) return result;

        _lockout_cache.TryGetValue(GetLockoutKey(app_config, user_login.Username), out var account);

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

        // This is a special case as we use the connection to authenticate the user.
        using DatabaseClient dbc = new(app_config.SQLServer, "master", user_login.Username, user_login.Password);

        try
        {
            await dbc.Connect(ct);

            result.LoginData = new()
            {
                user_id = user_login.Username,
                username = user_login.Username
            };
            result.PasswordVerificationResult = PasswordVerificationResult.Success;

            var is_admin = await DatabaseManagement.LoggedInUserHasAdminRights(dbc, ct);
            if (is_admin)
            {
                if (_microm_config.Value.DisableSQLServerAdministratorTwoFactorAuthentication)
                {
                    FinalizeSuccessfulLogin(app_config, user_login.Username, _encryptor.Encrypt(user_login.Password), result, isAdmin: true);
                    return result;
                }

                if (!await EnsureSqlAdminTotpSecret(app_config, ct))
                {
                    _log.LogError("SQL admin TOTP is enabled but APP_ID {app_id} has no configured TOTP secret and one could not be created.", app_config.ApplicationID);
                    result.PasswordVerificationResult = PasswordVerificationResult.Failed;
                    return result;
                }

                SetTwoFactorChallengeResult(app_config, user_login, result);
                return result;
            }

            FinalizeSuccessfulLogin(app_config, user_login.Username, _encryptor.Encrypt(user_login.Password), result, isAdmin: false);
        }
        catch (Exception ex)
        {
            _log.LogError("Error: {error}", ex);
            account = _lockout_cache.GetOrAdd(GetLockoutKey(app_config, user_login.Username), new AccountLockout());
            lock (account)
            {
                account.incrementBadLogonAndLock();
                result.AccountLocked = account.isAccountLocked();
                result.PasswordVerificationResult = PasswordVerificationResult.Failed;
            }
        }
        finally
        {
            await dbc.Disconnect();
        }

        return result;
    }

    public Task<RefreshTokenResult> AuthenticateRefresh(ApplicationOption app_config, string user_id, string refresh_token, string local_device_id, CancellationToken ct)
    {
        RefreshTokenResult result = new();

        if (string.IsNullOrEmpty(refresh_token) || string.IsNullOrEmpty(user_id)) return Task.FromResult(result);

        _lockout_cache.TryGetValue(GetLockoutKey(app_config, user_id), out var account);
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
                        DeleteRefreshCookie(app_config);
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
        _ = _lockout_cache.TryRemove(GetLockoutKey(app_config, user_name), out _);
        DeleteRefreshCookie(app_config);
        return Task.CompletedTask;
    }

    public void UnencryptClaims(Dictionary<string, object>? server_claims)
    {
        if (server_claims != null && server_claims.TryGetValue(MicroMServerClaimTypes.MicroMPassword, out var admin_password))
        {
            if (admin_password != null) server_claims[MicroMServerClaimTypes.MicroMPassword] = _encryptor?.Decrypt((string)admin_password) ?? admin_password;
        }
    }

    public Task<bool> SendPasswordRecoveryEmail(ApplicationOption app_config, string user_name, CancellationToken ct)
    {
        throw new NotImplementedException();
    }

    public Task<bool> RecoverPassword(ApplicationOption app_config, string user_name, string new_password, string recovery_code, CancellationToken ct)
    {
        throw new NotImplementedException();
    }

    Task<ResultWithStatus<bool, string>> IAuthenticator.SendPasswordRecoveryEmail(ApplicationOption app_config, string user_name, CancellationToken ct)
    {
        throw new NotImplementedException();
    }

    Task<ResultWithStatus<bool, string>> IAuthenticator.RecoverPassword(ApplicationOption app_config, string user_name, string new_password, string recovery_code, CancellationToken ct)
    {
        throw new NotImplementedException();
    }

    public Task<ExternalSignInResult> HandleExternalSignIn(ApplicationOption app, ExternalIdentity identity, string deviceId, CancellationToken ct)
    {
        throw new NotImplementedException();
    }

    public async Task<AuthenticatorResult> VerifyTwoFactorCode(ApplicationOption app_config, string challengeId, string code, CancellationToken ct)
    {
        AuthenticatorResult result = new();

        if (_microm_config.Value.DisableSQLServerAdministratorTwoFactorAuthentication)
        {
            _log.LogWarning("SQL admin TOTP verification rejected because SQL Server administrator 2FA is disabled for APP_ID {app_id}.", app_config.ApplicationID);
            _challengeStore.RemoveChallenge(challengeId);
            result.PasswordVerificationResult = PasswordVerificationResult.Failed;
            return result;
        }

        var challenge = _challengeStore.GetChallenge(challengeId);
        if (challenge == null || DateTime.UtcNow > challenge.ExpiresUtc || !challenge.ApplicationId.Equals(app_config.ApplicationID, StringComparison.OrdinalIgnoreCase))
        {
            _log.LogWarning("SQL admin TOTP verification failed: challenge {challenge_id} not found, expired, or for another application.", challengeId);
            if (challenge != null) _challengeStore.RemoveChallenge(challengeId);
            result.PasswordVerificationResult = PasswordVerificationResult.Failed;
            return result;
        }

        result.LoginData = new()
        {
            user_id = challenge.Username,
            username = challenge.Username
        };

        if (!challenge.Metadata.TryGetValue(ChallengePasswordMetadataKey, out var encryptedPassword) || string.IsNullOrWhiteSpace(encryptedPassword))
        {
            _log.LogWarning("SQL admin TOTP verification failed: challenge {challenge_id} has no password metadata.", challengeId);
            _challengeStore.RemoveChallenge(challengeId);
            result.PasswordVerificationResult = PasswordVerificationResult.Failed;
            return result;
        }

        if (string.IsNullOrWhiteSpace(app_config.SQLAdminTotpSecret))
        {
            _log.LogWarning("SQL admin TOTP verification failed: APP_ID {app_id} has no TOTP secret.", app_config.ApplicationID);
            _challengeStore.RemoveChallenge(challengeId);
            result.PasswordVerificationResult = PasswordVerificationResult.Failed;
            return result;
        }

        bool isValidCode = _totpService.VerifyCode(app_config.SQLAdminTotpSecret, code);
        if (!isValidCode)
        {
            _log.LogWarning("SQL admin TOTP verification failed: invalid code for user {username}.", challenge.Username);
            result.PasswordVerificationResult = PasswordVerificationResult.Failed;
            return result;
        }

        string password = _encryptor.Decrypt(encryptedPassword);
        using DatabaseClient dbc = new(app_config.SQLServer, "master", challenge.Username, password);
        try
        {
            await dbc.Connect(ct);
            bool isAdmin = await DatabaseManagement.LoggedInUserHasAdminRights(dbc, ct);
            if (!isAdmin)
            {
                _log.LogWarning("SQL admin TOTP verification failed: user {username} no longer has admin rights.", challenge.Username);
                _challengeStore.RemoveChallenge(challengeId);
                result.PasswordVerificationResult = PasswordVerificationResult.Failed;
                return result;
            }
        }
        catch (Exception ex)
        {
            _log.LogWarning(ex, "SQL admin TOTP verification failed: could not revalidate SQL login for user {username}.", challenge.Username);
            result.PasswordVerificationResult = PasswordVerificationResult.Failed;
            return result;
        }
        finally
        {
            await dbc.Disconnect();
        }

        _challengeStore.RemoveChallenge(challengeId);
        FinalizeSuccessfulLogin(app_config, challenge.Username, encryptedPassword, result, isAdmin: true);
        return result;
    }
}
