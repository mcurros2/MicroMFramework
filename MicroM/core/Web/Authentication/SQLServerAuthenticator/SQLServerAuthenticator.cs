﻿using MicroM.Configuration;
using MicroM.Core;
using MicroM.Data;
using MicroM.Database;
using MicroM.DataDictionary.CategoriesDefinitions;
using MicroM.DataDictionary.Entities.MicromUsers;
using MicroM.Web.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Collections.Concurrent;

namespace MicroM.Web.Authentication
{

    public class SQLServerAuthenticator : IAuthenticator
    {
        private readonly ILogger<SQLServerAuthenticator> _log;
        private readonly IMicroMEncryption _encryptor;
        private readonly IHttpContextAccessor _contextAccessor;
        private readonly IOptions<MicroMOptions> _microm_config;

        public SQLServerAuthenticator(ILogger<SQLServerAuthenticator> logger, IMicroMEncryption encryptor, IHttpContextAccessor contextAccessor, IOptions<MicroMOptions> microm_config)
        {
            _log = logger;
            _encryptor = encryptor;
            _contextAccessor = contextAccessor;
            _microm_config = microm_config;
        }

        private static ConcurrentDictionary<string, AccountLockout> _lockout_cache = new(StringComparer.OrdinalIgnoreCase);

        private string GetRefreshCookieName(ApplicationOption app_config)
        {
            return $"m-{nameof(SQLServerAuthenticator)}-{app_config.ApplicationID}-r";
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

        public async Task<AuthenticatorResult> AuthenticateLogin(ApplicationOption app_config, UserLogin user_login, CancellationToken ct)
        {
            AuthenticatorResult result = new();
            if (string.IsNullOrEmpty(user_login.Username)) return result;

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

            using DatabaseClient dbc = new(app_config.SQLServer, "master", user_login.Username, user_login.Password);

            try
            {
                await dbc.Connect(ct);

                result.LoginData = new()
                {
                    user_id = user_login.Username,
                    username = user_login.Username,
                    refresh_token = CryptClass.GenerateRandomBase64String()
                };
                result.PasswordVerificationResult = Microsoft.AspNetCore.Identity.PasswordVerificationResult.Success;

                var is_admin = await DatabaseManagement.LoggedInUserHasAdminRights(dbc, ct);

                account = _lockout_cache.GetOrAdd($"{app_config.ApplicationID}.{user_login.Username}", new AccountLockout());
                lock (account)
                {
                    account.unlockAccount();
                    account.setRefreshToken(result.LoginData.refresh_token);
                    WriteRefreshTokenToCookie(app_config, result.LoginData.refresh_token);
                }

                result.ServerClaims.Add(MicroMServerClaimTypes.MicroMUser_id, user_login.Username);
                result.ServerClaims.Add(MicroMServerClaimTypes.MicroMAPP_id, app_config.ApplicationID);
                result.ServerClaims.Add(MicroMServerClaimTypes.MicroMUsername, user_login.Username);
                result.ServerClaims.Add(MicroMServerClaimTypes.MicroMUserType_id, is_admin ? nameof(UserTypes.ADMIN) : nameof(UserTypes.USER));
                result.ServerClaims.Add(MicroMServerClaimTypes.MicroMUserDeviceID, "none");
                result.ServerClaims[MicroMServerClaimTypes.MicroMUserGroups] = (List<string>)[];


                result.ServerClaims.Add(MicroMServerClaimTypes.MicroMPassword, _encryptor.Encrypt(user_login.Password));
            }
            catch (Exception ex)
            {
                _log.LogError("Error: {error}", ex);
                account = _lockout_cache.GetOrAdd($"{app_config.ApplicationID}.{user_login.Username}", new AccountLockout());
                lock (account)
                {
                    account.incrementBadLogonAndLock();
                    result.AccountLocked = account.isAccountLocked();
                    result.PasswordVerificationResult = Microsoft.AspNetCore.Identity.PasswordVerificationResult.Failed;
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
            _ = _lockout_cache.TryRemove($"{app_config.ApplicationID}.{user_name}", out _);
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

        Task<(bool failed, string? error_message)> IAuthenticator.SendPasswordRecoveryEmail(ApplicationOption app_config, string user_name, CancellationToken ct)
        {
            throw new NotImplementedException();
        }

        Task<(bool failed, string? error_message)> IAuthenticator.RecoverPassword(ApplicationOption app_config, string user_name, string new_password, string recovery_code, CancellationToken ct)
        {
            throw new NotImplementedException();
        }
    }
}
