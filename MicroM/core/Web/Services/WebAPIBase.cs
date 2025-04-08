
using MicroM.Configuration;
using MicroM.Core;
using MicroM.Data;
using MicroM.DataDictionary;
using MicroM.DataDictionary.CategoriesDefinitions;
using MicroM.DataDictionary.Entities.MicromUsers;
using MicroM.Extensions;
using MicroM.ImportData;
using MicroM.Web.Authentication;
using MicroM.Web.Services.Security;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Collections.Concurrent;
using System.Reflection;
using System.Security.Claims;
using System.Text.Json;
using static MicroM.Extensions.ColumnExtensions;
using static System.ArgumentNullException;
using static MicroM.ImportData.EntityImportData;
using MicroM.DataDictionary.StatusDefs;

namespace MicroM.Web.Services
{
    public class WebAPIBase : IMicroMWebAPI
    {
        private readonly ILogger<WebAPIBase> _log;
        private readonly IMicroMEncryption _encryptor;
        private readonly IMicroMAppConfiguration _app_config;
        private readonly IBackgroundTaskQueue _backgroundTaskQueue;
        private readonly IFileUploadService _upload;
        private readonly IEmailService _emailService;
        private readonly ISecurityService _securityService;
        private readonly IDeviceIdService _deviceIdService;

        #region "Applications Keys"

        private readonly ConcurrentDictionary<string, Dictionary<string, object>> _ApplicationKeys = new(StringComparer.OrdinalIgnoreCase);

        public void ReplaceApplicationKey(string app_id, string key, string value)
        {
            _ApplicationKeys.TryGetValue(app_id, out Dictionary<string, object>? app_keys);
            if (app_keys == null)
            {
                app_keys = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
                _ApplicationKeys[app_id] = app_keys;
            }
            _ApplicationKeys[app_id][key] = value;
        }

        public void EnsureApplicationKeys(string app_id, Dictionary<string, object> values)
        {
            _ApplicationKeys.TryGetValue(app_id, out Dictionary<string, object>? app_keys);
            if (app_keys != null)
            {
                foreach (var key in app_keys.Keys)
                {
                    values[key] = app_keys[key];
                }
            }
        }

        public Dictionary<string, object> GetApplicationKeys(string app_id)
        {
            _ApplicationKeys.TryGetValue(app_id, out Dictionary<string, object>? app_keys);
            return app_keys ?? new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
        }

        #endregion

        #region "Constructor"

        private readonly MicroMOptions _options;


        public WebAPIBase
            (
            IOptions<MicroMOptions> options,
            ILogger<WebAPIBase> logger,
            IMicroMEncryption encryptor,
            IMicroMAppConfiguration app_config,
            IBackgroundTaskQueue queue,
            IFileUploadService upload,
            IEmailService emailService,
            ISecurityService securityService,
            IDeviceIdService deviceIdService
            )
        {
            _log = logger;
            _options = options.Value;
            _encryptor = encryptor;
            _app_config = app_config;
            _backgroundTaskQueue = queue;
            _upload = upload;
            _emailService = emailService;
            _securityService = securityService;
            _deviceIdService = deviceIdService;

            ThrowIfNull(options);

            if (_options.DefaultConnectionTimeOutInSecs != -1) DataDefaults.DefaultConnectionTimeOutInSecs = _options.DefaultConnectionTimeOutInSecs;
            if (_options.DefaultRowLimitForViews != -1) DataDefaults.DefaultRowLimitForViews = _options.DefaultRowLimitForViews;
            if (_options.DefaultCommandTimeOutInMins != -1) DataDefaults.DefaultCommandTimeOutInMins = _options.DefaultCommandTimeOutInMins;
        }

        #endregion

        public IBackgroundTaskQueue Queue => _backgroundTaskQueue;

        public IEmailService EmailService => _emailService;

        public ISecurityService SecurityService => _securityService;

        public IFileUploadService FileUploadService => _upload;

        public async Task<bool> RefreshConfig(string? app_id, CancellationToken ct)
        {
            bool ret = await _app_config.RefreshConfiguration(app_id, ct);

            await _securityService.RefreshGroupsSecurityRecords(app_id, ct);

            return ret;
        }

        public Type? GetEntityType(string app_id, string entity_name)
        {
            return _app_config.GetEntityType(app_id, entity_name);
        }

        public List<Assembly> GetAllAPPAssemblies(string app_id)
        {
            return _app_config.GetAllAPPAssemblies(app_id);
        }

        /// <summary>
        /// Connection factory for the webAPI.
        /// </summary>
        /// <returns></returns>
        public IEntityClient CreateDbConnection(ApplicationOption app, Dictionary<string, object>? server_claims, IAuthenticationProvider auth)
        {
            var authenticator = auth.GetAuthenticator(app);

            authenticator?.UnencryptClaims(server_claims);

            string user = app.AuthenticationType == nameof(AuthenticationTypes.SQLServerAuthentication) && string.IsNullOrEmpty(app.SQLUser) ? (string?)server_claims?[MicroMServerClaimTypes.MicroMUsername] ?? "" : app.SQLUser;
            string pass = app.AuthenticationType == nameof(AuthenticationTypes.SQLServerAuthentication) && string.IsNullOrEmpty(app.SQLUser) ? (string?)server_claims?[MicroMServerClaimTypes.MicroMPassword] ?? "" : app.SQLPassword;

            var (device_id, ipaddress, user_agent) = _deviceIdService.GetDeviceID();
            string workstation_id = $"{ipaddress} {user_agent}";

            DatabaseClient dbc = new(server: app.SQLServer, user: user, password: pass, db: app.SQLDB, logger: _log, server_claims: server_claims)
            {
                // TODO: add to application option pooling options
                Pooling = true,
                MinPoolSize = 0,
                MaxPoolSize = 500,
                ApplicationName = $"MicroM - {app.ApplicationName}",
                WorkstationID = workstation_id,
            };

            return dbc;
        }

        public Task<IEntityClient> CreateDbConnection(string app_id, Dictionary<string, object>? server_claims, IAuthenticationProvider auth, CancellationToken ct)
        {
            ApplicationOption app = _app_config.GetAppConfiguration(app_id, ct)!;
            return Task.FromResult(CreateDbConnection(app, server_claims, auth));
        }



        /// <summary>
        /// Creates an Entity if exists in the configured assembly <see cref="LoadEntityTypes(Assembly)"/>.
        /// </summary>
        /// <param name="entity_name"></param>
        /// <param name="ec"></param>
        /// <returns></returns>
        public EntityBase? CreateEntity(ApplicationOption app, string entity_name, Dictionary<string, object>? server_claims, IAuthenticationProvider auth, IEntityClient? ec = null)
        {
            EntityBase? entity = null;
            ec ??= CreateDbConnection(app, server_claims, auth);
            Type? ent_type = _app_config.GetEntityType(app.ApplicationID, entity_name);
            if (ent_type != null) entity = (EntityBase?)Activator.CreateInstance(ent_type, ec, _encryptor);

            return entity;
        }

        public EntityBase? CreateEntity(string app_id, string entity_name, Dictionary<string, object>? server_claims, IAuthenticationProvider auth, CancellationToken ct)
        {
            EntityBase? entity = null;
            var ec = CreateDbConnection(app_id, server_claims, auth, ct);
            Type? ent_type = _app_config.GetEntityType(app_id, entity_name);
            if (ent_type != null) entity = (EntityBase?)Activator.CreateInstance(ent_type, ec, _encryptor);

            return entity;
        }


        #region "Authentication"


        public async Task HandleLogoff(IAuthenticationProvider auth, string app_id, string user_name, CancellationToken ct)
        {
            ApplicationOption app = _app_config.GetAppConfiguration(app_id, ct)!;

            var authenticator = auth.GetAuthenticator(app);
            if (authenticator != null)
            {
                await authenticator.Logoff(app, user_name, ct);
            }
            else
            {
                _log.LogError("LOGOFF: Invalid authenticator specified: {authenticator} APP_ID {app_id}", app_id, app.AuthenticationType);
            }

        }


        public async Task<(LoginResult? user_data, TokenResult? token_result)>
            HandleLogin(IAuthenticationProvider auth, WebAPIJsonWebTokenHandler jwt_handler, string app_id, UserLogin user_login, Dictionary<string, object> claims, CancellationToken ct)
        {
            if (string.IsNullOrEmpty(user_login.Username)) return (null, null);

            ApplicationOption? app = _app_config.GetAppConfiguration(app_id, ct);

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
                    _log.LogWarning("ACCOUNT_DISABLED: APP_ID {app_id} User: {username}", app_id, user_login.Username);
                }
                else if (authenticatorResult.AccountLocked)
                {
                    _log.LogWarning("ACCOUNT_LOCKOUT: APP_ID {app_id} User: {username} Locked minutes remaining: {lockout_mins}", app_id, user_login.Username, authenticatorResult.LoginData?.locked_minutes_remaining);
                }
                else
                {
                    if (authenticatorResult.PasswordVerificationResult.IsIn(PasswordVerificationResult.Success, PasswordVerificationResult.SuccessRehashNeeded) && authenticatorResult.LoginData != null)
                    {
                        var merged_claims = authenticatorResult.ServerClaims.Concat(claims)
                                      .GroupBy(i => i.Key)
                                      .ToDictionary(g => g.Key, g => g.First().Value);

                        token_result = jwt_handler.GenerateJwtTokenWEBApi(merged_claims, app);
                    }
                }
            }
            else
            {
                _log.LogError("LOGIN: Invalid authenticator specified: {authenticator} APP_ID {app_id}", app.AuthenticationType, app_id);
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


        public async Task<(RefreshTokenResult? result, TokenResult? token_result)>
            HandleRefreshToken(IAuthenticationProvider auth, WebAPIJsonWebTokenHandler jwt_handler, string app_id, UserRefreshTokenRequest refreshRequest, CancellationToken ct)
        {
            ApplicationOption? app = _app_config.GetAppConfiguration(app_id, ct);
            if (app == null)
            {
                _log.LogTrace("REFRESH_TOKEN: Invalid APP_ID {app_id}", app_id);
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
                        var refresh_result = await authenticator.AuthenticateRefresh(app, user_id, refreshRequest.RefreshToken, ct);

                        if (refresh_result.Status.IsIn(LoginAttemptStatus.Updated, LoginAttemptStatus.RefreshTokenValid))
                        {
                            var dicClaim = claims.Claims.GroupBy(claim => claim.Type).ToDictionary(group => group.Key, group => (object)group.Last().Value);
                            return (refresh_result, jwt_handler.GenerateJwtTokenWEBApi(dicClaim, app));
                        }
                        else
                        {
                            _log.LogWarning("REFRESH_TOKEN: APP_ID {app_id} can't refresh token. Status: {status} Message {message} refresh-token: {token}", app_id, refresh_result.Status, refresh_result.Message, refreshRequest.RefreshToken);
                        }
                    }
                    else
                    {
                        _log.LogError("REFRESH_TOKEN: Invalid authenticator specified: {authenticator} APP_ID {app_id}", app.AuthenticationType, app_id);
                    }
                }
                else
                {
                    _log.LogWarning("REFRESH_TOKEN: APP_ID {app_id} can't find userID in claims or expired token", app_id);
                }
            }
            else
            {
                _log.LogWarning("REFRESH_TOKEN: APP_ID {app_id} Invalid expired token", app_id);
            }

            return (null, null);
        }

        public async Task<(bool failed, string? error_message)> HandleSendRecoveryEmail(IAuthenticationProvider auth, string app_id, string user_name, CancellationToken ct)
        {
            ApplicationOption? app = _app_config.GetAppConfiguration(app_id, ct);
            if (app == null)
            {
                _log.LogTrace("RECOVERY_EMAIL: Invalid APP_ID {app_id}", app_id);
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
                _log.LogError("RECOVERY_EMAIL: Invalid authenticator specified: {authenticator} APP_ID {app_id}", app.AuthenticationType, app_id);
                return (failed: true, null);
            }
        }

        public async Task<(bool failed, string? error_message)> HandleRecoverPassword(IAuthenticationProvider auth, string app_id, string user_name, string new_password, string recovery_code, CancellationToken ct)
        {
            ApplicationOption? app = _app_config.GetAppConfiguration(app_id, ct);
            if (app == null)
            {
                _log.LogTrace("RECOVER_PASSWORD: Invalid APP_ID {app_id}", app_id);
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
                _log.LogError("RECOVER_PASSWORD: Invalid authenticator specified: {authenticator} APP_ID {app_id}", app.AuthenticationType, app_id);
                return (failed: true, null);
            }

        }

        #endregion


        #region "WebAPI standard calls"

        public EntityDefinition? HandleGetEntityDefinition(string app_id, string entity_name)
        {
            EntityDefinition? result = null;


            Type? ent_type = _app_config.GetEntityType(app_id, entity_name);
            if (ent_type != null)
            {
                EntityBase? obj = (EntityBase?)Activator.CreateInstance(ent_type);
                if (obj != null)
                {
                    result = obj.Def;
                }

            }
            else
            {
                _log.LogError("GetEntityDefinition ERROR: {entity_name} not found in entities type cache for application {app_id}.", entity_name, app_id);
            }

            return result;
        }

        private async Task<Dictionary<string, object?>?>
            GetEntity(IAuthenticationProvider auth, string app_id, string entity_name, DataWebAPIRequest parms, IEntityClient ec, CancellationToken ct)
        {
            Dictionary<string, object?>? result = null;
            try
            {
                ApplicationOption? app = _app_config.GetAppConfiguration(app_id, ct);

                if (app == null) return null;

                var entity = CreateEntity(app, entity_name, parms.ServerClaims, auth, ec);
                if (entity != null)
                {

                    EnsureApplicationKeys(app_id, parms.Values);
                    entity.SetKeyValues(parms.Values);
                    if (parms.ParentKeys != null && parms.ParentKeys.Count > 0)
                    {
                        EnsureApplicationKeys(app_id, parms.ParentKeys);
                        entity.SetKeyValues(parms.ParentKeys);
                    }
                    await entity.GetData(ct, _options, parms.ServerClaims, api: this, app_id: app_id);
                    result = entity.Def.Columns.ToDictionary(new HashSet<string>(StringComparer.Ordinal) { SystemColumnNames.webusr });
                }
                else
                {
                    _log.LogError("GetEntity ERROR: {entity_name} not found in entities type cache.", entity_name);
                }
            }
            finally
            {
                await ec.Disconnect();
            }

            return result;

        }

        public async Task<Dictionary<string, object?>?> HandleGetEntity(IAuthenticationProvider auth, string app_id, string entity_name, DataWebAPIRequest parms, IEntityClient ec, CancellationToken ct)
        {
            return await GetEntity(auth, app_id, entity_name, parms, ec, ct);
        }

        private async Task<DBStatusResult?> InsertEntity(IAuthenticationProvider auth, string app_id, string entity_name, DataWebAPIRequest parms, IEntityClient ec, CancellationToken ct)
        {
            DBStatusResult? result = null;
            try
            {
                ApplicationOption? app = _app_config.GetAppConfiguration(app_id, ct);

                if (app == null) return null;

                var entity = CreateEntity(app, entity_name, parms.ServerClaims, auth, ec);
                if (entity != null)
                {
                    EnsureApplicationKeys(app_id, parms.Values);
                    if (parms.ParentKeys != null && parms.ParentKeys.Count > 0)
                    {
                        EnsureApplicationKeys(app_id, parms.ParentKeys);
                    }

                    if (parms.RecordsSelection == null || parms.RecordsSelection.Count == 0)
                    {
                        entity.SetColumnValues(parms.Values);
                        if (parms.ParentKeys != null && parms.ParentKeys.Count > 0)
                        {
                            entity.SetKeyValues(parms.ParentKeys);
                        }
                        result = await entity.InsertData(ct, options: _options, server_claims: parms.ServerClaims, api: this, app_id: app_id);
                    }
                    else
                    {
                        List<DBStatus> results = [];
                        bool failed = false;
                        var clonedValues = entity.Def.Columns.ToDictionary(new(SystemColumnNames.AsStringArray)).ToDictionary(kvp => kvp.Key, kvp => kvp.Value!);

                        await entity.Client.Connect(ct);

                        foreach (var keys in parms.RecordsSelection)
                        {
                            entity.SetColumnValues(clonedValues);
                            EnsureApplicationKeys(app_id, keys);
                            entity.SetColumnValues(parms.Values);
                            entity.SetColumnValues(keys);
                            if (parms.ParentKeys != null && parms.ParentKeys.Count > 0)
                            {
                                entity.SetKeyValues(parms.ParentKeys);
                            }

                            var record_result = await entity.InsertData(ct, options: _options, server_claims: parms.ServerClaims, api: this, app_id: app_id);
                            if (record_result != null)
                            {
                                if (!failed) failed = record_result.Failed;
                                results.AddRange(record_result.Results!);
                            }
                        }
                        if (results.Count > 0)
                        {
                            result = new() { Failed = failed, Results = results };
                        }

                        await entity.Client.Disconnect();
                    }

                }
                else
                {
                    _log.LogError("InsertEntity ERROR: {entity_name} not found in entities type cache.", entity_name);
                }

            }
            finally
            {
                await ec.Disconnect();
            }

            return result;

        }

        public async Task<DBStatusResult?> HandleInsertEntity(IAuthenticationProvider auth, string app_id, string entity_name, DataWebAPIRequest parms, IEntityClient ec, CancellationToken ct)
        {
            return await InsertEntity(auth, app_id, entity_name, parms, ec, ct);
        }


        private async Task<DBStatusResult?> UpdateEntity(IAuthenticationProvider auth, string app_id, string entity_name, DataWebAPIRequest parms, IEntityClient ec, CancellationToken ct)
        {
            DBStatusResult? result = null;

            try
            {
                ApplicationOption? app = _app_config.GetAppConfiguration(app_id, ct);

                if (app == null) return null;

                var entity = CreateEntity(app, entity_name, parms.ServerClaims, auth, ec);
                if (entity != null)
                {
                    EnsureApplicationKeys(app_id, parms.Values);
                    if (parms.ParentKeys != null && parms.ParentKeys.Count > 0)
                    {
                        EnsureApplicationKeys(app_id, parms.ParentKeys);
                    }

                    if (parms.RecordsSelection == null || parms.RecordsSelection.Count == 0)
                    {
                        entity.SetColumnValues(parms.Values);
                        if (parms.ParentKeys != null && parms.ParentKeys.Count > 0)
                        {
                            entity.SetKeyValues(parms.ParentKeys);
                        }
                        result = await entity.UpdateData(ct, options: _options, server_claims: parms.ServerClaims, api: this, app_id: app_id);
                    }
                    else
                    {
                        List<DBStatus> results = [];
                        bool failed = false;
                        var clonedValues = entity.Def.Columns.ToDictionary(new(SystemColumnNames.AsStringArray)).ToDictionary(kvp => kvp.Key, kvp => kvp.Value!);

                        await entity.Client.Connect(ct);

                        foreach (var keys in parms.RecordsSelection)
                        {
                            entity.SetColumnValues(clonedValues);
                            EnsureApplicationKeys(app_id, keys);
                            entity.SetColumnValues(parms.Values);
                            entity.SetColumnValues(keys);
                            if (parms.ParentKeys != null && parms.ParentKeys.Count > 0)
                            {
                                entity.SetKeyValues(parms.ParentKeys);
                            }

                            var record_result = await entity.UpdateData(ct, options: _options, server_claims: parms.ServerClaims, api: this, app_id: app_id);
                            if (record_result != null)
                            {
                                if (!failed) failed = record_result.Failed;
                                results.AddRange(record_result.Results!);
                            }
                        }
                        if (results.Count > 0)
                        {
                            result = new() { Failed = failed, Results = results };
                        }

                    }
                }
                else
                {
                    _log.LogError("UpdateEntity ERROR: {entity_name} not found in entities type cache.", entity_name);
                }

            }
            finally
            {
                await ec.Disconnect();
            }

            return result;
        }

        public async Task<DBStatusResult?> HandleUpdateEntity(IAuthenticationProvider auth, string app_id, string entity_name, DataWebAPIRequest parms, IEntityClient ec, CancellationToken ct)
        {
            return await UpdateEntity(auth, app_id, entity_name, parms, ec, ct);
        }


        private async Task<DBStatusResult?> DeleteEntity(IAuthenticationProvider auth, string app_id, string entity_name, DataWebAPIRequest parms, IEntityClient ec, CancellationToken ct)
        {
            DBStatusResult? result = null;

            try
            {
                ApplicationOption? app = _app_config.GetAppConfiguration(app_id, ct);

                if (app == null) return null;

                var entity = CreateEntity(app, entity_name, parms.ServerClaims, auth, ec);
                if (entity != null)
                {

                    EnsureApplicationKeys(app_id, parms.Values);
                    if (parms.ParentKeys != null && parms.ParentKeys.Count > 0) EnsureApplicationKeys(app_id, parms.ParentKeys);

                    if (parms.RecordsSelection == null || parms.RecordsSelection.Count == 0)
                    {
                        entity.SetKeyValues(parms.Values);
                        if (parms.ParentKeys != null && parms.ParentKeys.Count > 0)
                        {
                            entity.SetKeyValues(parms.ParentKeys);
                        }
                        result = await entity.DeleteData(ct, options: _options, server_claims: parms.ServerClaims, api: this, app_id: app_id);
                    }
                    else
                    {
                        List<DBStatus> results = [];
                        bool failed = false;
                        var clonedValues = entity.Def.Columns.ToDictionary(new(SystemColumnNames.AsStringArray)).ToDictionary(kvp => kvp.Key, kvp => kvp.Value!);

                        await entity.Client.Connect(ct);

                        foreach (var keys in parms.RecordsSelection)
                        {
                            EnsureApplicationKeys(app_id, keys);
                            entity.SetKeyValues(keys);
                            if (parms.ParentKeys != null && parms.ParentKeys.Count > 0)
                            {
                                entity.SetKeyValues(parms.ParentKeys);
                            }

                            var record_result = await entity.DeleteData(ct, options: _options, server_claims: parms.ServerClaims, api: this, app_id: app_id);
                            if (record_result != null)
                            {
                                if (!failed) failed = record_result.Failed;
                                results.AddRange(record_result.Results!);
                            }
                        }
                        if (results.Count > 0)
                        {
                            result = new() { Failed = failed, Results = results };
                        }

                    }
                }
                else
                {
                    _log.LogError("DeleteEntity ERROR: {entity_name} not found in entities type cache.", entity_name);
                }
            }
            finally
            {
                await ec.Disconnect();
            }

            return result;

        }

        public async Task<DBStatusResult?> HandleDeleteEntity(IAuthenticationProvider auth, string app_id, string entity_name, DataWebAPIRequest parms, IEntityClient ec, CancellationToken ct)
        {
            return await DeleteEntity(auth, app_id, entity_name, parms, ec, ct);
        }

        private async Task<string?> LookupEntity(IAuthenticationProvider auth, string app_id, string entity_name, DataWebAPIRequest parms, IEntityClient ec, CancellationToken ct, string? lookup_name = null)
        {
            string? result = null;

            try
            {
                ApplicationOption? app = _app_config.GetAppConfiguration(app_id, ct);

                if (app == null) return null;

                var entity = CreateEntity(app, entity_name, parms.ServerClaims, auth, ec);
                if (entity != null)
                {

                    EnsureApplicationKeys(app_id, parms.Values);
                    entity.SetKeyValues(parms.Values);
                    if (parms.ParentKeys != null && parms.ParentKeys.Count > 0)
                    {
                        EnsureApplicationKeys(app_id, parms.ParentKeys);
                        entity.SetKeyValues(parms.ParentKeys);
                    }
                    result = await entity.LookupData(ct, lookup_name, options: _options, server_claims: parms.ServerClaims, api: this, app_id: app_id);
                }
                else
                {
                    _log.LogError("LookupEntity ERROR: {entity_name} not found in entities type cache.", entity_name);
                }
            }
            finally
            {
                await ec.Disconnect();
            }

            return result;
        }

        public async Task<LookupResult> HandleLookupEntity(IAuthenticationProvider auth, string app_id, string entity_name, DataWebAPIRequest parms, IEntityClient ec, CancellationToken ct, string? lookup_name = null)
        {
            var description = await LookupEntity(auth, app_id, entity_name, parms, ec, ct, lookup_name);

            var result = new LookupResult() { Description = description ?? "" };

            return result;
        }

        private async Task<List<DataResult>?> ExecuteView(IAuthenticationProvider auth, string app_id, string entity_name, string view_name, DataWebAPIRequest parms, IEntityClient ec, CancellationToken ct)
        {
            List<DataResult>? result = null;

            try
            {
                ApplicationOption? app = _app_config.GetAppConfiguration(app_id, ct);

                if (app == null) return null;

                var entity = CreateEntity(app, entity_name, parms.ServerClaims, auth, ec);
                if (entity != null)
                {
                    if (entity.Def.Views.ContainsKey(view_name))
                    {
                        int row_limit = DataDefaults.DefaultRowLimitForViews;
                        // MMC: Extract special parameter @row_limit
                        if (parms.Values.TryGetValue(DataDefaults.RowLimitParameterName, out object? value))
                        {
                            if (value is JsonElement element) element.TryConvertFromJsonElement<int>(out row_limit);
                            else int.TryParse((string)value, System.Globalization.NumberStyles.Integer, System.Globalization.CultureInfo.InvariantCulture, out row_limit);

                            parms.Values.Remove(DataDefaults.RowLimitParameterName);
                        }
                        var view = entity.Def.Views[view_name];
                        EnsureApplicationKeys(app_id, parms.Values);
                        view.Proc.SetParmsValues(parms.Values);
                        if (parms.ParentKeys != null && parms.ParentKeys.Count > 0)
                        {
                            EnsureApplicationKeys(app_id, parms.ParentKeys);
                            entity.SetKeyValues(parms.ParentKeys);
                        }
                        result = await entity.ExecuteView(ct, view, row_limit, options: _options, server_claims: parms.ServerClaims, api: this, app_id: app_id);
                    }
                    else
                    {
                        _log.LogError("ExecuteView ERROR: Entity: {entity_name} Mneo: {entity_def} View: {view_name} not found in entity definition.", entity_name, entity.Def.Mneo, view_name);
                    }
                }
                else
                {
                    _log.LogError("ExecuteView ERROR: {entity_name} not found in entities type cache.", entity_name);
                }
            }
            finally
            {
                await ec.Disconnect();
            }

            return result;

        }

        public async Task<List<DataResult>?> HandleExecuteView(IAuthenticationProvider auth, string app_id, string entity_name, string view_name, DataWebAPIRequest parms, IEntityClient ec, CancellationToken ct)
        {
            return await ExecuteView(auth, app_id, entity_name, view_name, parms, ec, ct);
        }


        private async Task<List<DataResult>?> ExecuteProc(IAuthenticationProvider auth, string app_id, string entity_name, string proc_name, DataWebAPIRequest parms, IEntityClient ec, CancellationToken ct)
        {
            List<DataResult> result = [];

            try
            {
                ApplicationOption? app = _app_config.GetAppConfiguration(app_id, ct);

                if (app == null) return result;

                var entity = CreateEntity(app, entity_name, parms.ServerClaims, auth, ec);
                if (entity != null)
                {
                    if (entity.Def.Procs.ContainsKey(proc_name))
                    {
                        var proc = entity.Def.Procs[proc_name];

                        EnsureApplicationKeys(app_id, parms.Values);
                        if (parms.ParentKeys != null && parms.ParentKeys.Count > 0) EnsureApplicationKeys(app_id, parms.ParentKeys);

                        if (parms.RecordsSelection == null || parms.RecordsSelection.Count == 0)
                        {
                            proc.SetParmsValues(parms.Values);
                            if (parms.ParentKeys != null && parms.ParentKeys.Count > 0)
                            {
                                entity.SetKeyValues(parms.ParentKeys);
                            }
                            result = await entity.ExecuteProc(ct, proc, options: _options, server_claims: parms.ServerClaims, api: this, set_parms_from_columns: false, app_id: app_id);
                        }
                        else
                        {
                            List<DataResult> results = [];
                            foreach (var keys in parms.RecordsSelection)
                            {
                                EnsureApplicationKeys(app_id, keys);
                                proc.SetParmsValues(parms.Values);
                                proc.SetParmsValues(keys);
                                if (parms.ParentKeys != null && parms.ParentKeys.Count > 0)
                                {
                                    entity.SetKeyValues(parms.ParentKeys);
                                }

                                var record_result = await entity.ExecuteProc(ct, proc, options: _options, server_claims: parms.ServerClaims, api: this, set_parms_from_columns: false, app_id: app_id);
                                if (record_result != null)
                                {
                                    results.AddRange(record_result);
                                }
                            }

                        }
                    }
                    else
                    {
                        _log.LogError("ExecuteProc ERROR: Entity: {entity_name} Mneo: {entity_def} Proc: {proc_name} not found in entity definition.", entity_name, entity.Def.Mneo, proc_name);
                    }
                }
                else
                {
                    _log.LogError("ExecuteProc ERROR: {entity_name} not found in entities type cache.", entity_name);
                }
            }
            finally
            {
                await ec.Disconnect();
            }

            return result;

        }

        public async Task<List<DataResult>?> HandleExecuteProc(IAuthenticationProvider auth, string app_id, string entity_name, string proc_name, DataWebAPIRequest parms, IEntityClient ec, CancellationToken ct)
        {
            return await ExecuteProc(auth, app_id, entity_name, proc_name, parms, ec, ct);
        }

        private async Task<DBStatusResult?> ExecuteProcDBStatus(IAuthenticationProvider auth, string app_id, string entity_name, string proc_name, DataWebAPIRequest parms, IEntityClient ec, CancellationToken ct)
        {
            DBStatusResult result = new();

            try
            {
                ApplicationOption? app = _app_config.GetAppConfiguration(app_id, ct);

                if (app == null) return result;

                var entity = CreateEntity(app, entity_name, parms.ServerClaims, auth, ec);
                if (entity != null)
                {
                    if (entity.Def.Procs.ContainsKey(proc_name))
                    {
                        var proc = entity.Def.Procs[proc_name];

                        EnsureApplicationKeys(app_id, parms.Values);
                        if (parms.ParentKeys != null && parms.ParentKeys.Count > 0) EnsureApplicationKeys(app_id, parms.ParentKeys);

                        if (parms.RecordsSelection == null || parms.RecordsSelection.Count == 0)
                        {
                            proc.SetParmsValues(parms.Values);
                            if (parms.ParentKeys != null && parms.ParentKeys.Count > 0)
                            {
                                entity.SetKeyValues(parms.ParentKeys);
                            }
                            result = await entity.ExecuteProcessDBStatus(ct, proc, options: _options, server_claims: parms.ServerClaims, api: this, set_parms_from_columns: false, app_id: app_id);
                        }
                        else
                        {
                            bool failed = false;
                            List<DBStatus> results = [];
                            foreach (var keys in parms.RecordsSelection)
                            {
                                EnsureApplicationKeys(app_id, keys);
                                proc.SetParmsValues(parms.Values);
                                proc.SetParmsValues(keys);
                                if (parms.ParentKeys != null && parms.ParentKeys.Count > 0)
                                {
                                    entity.SetKeyValues(parms.ParentKeys);
                                }

                                var record_result = await entity.ExecuteProcessDBStatus(ct, proc, options: _options, server_claims: parms.ServerClaims, api: this, set_parms_from_columns: false, app_id: app_id);
                                if (record_result != null)
                                {
                                    if (record_result.Failed) failed = true;
                                    if (record_result.Results != null) results.Add(record_result.Results[0]);
                                }
                                result = new() { Failed = failed, Results = results };
                            }

                        }
                    }
                    else
                    {
                        _log.LogError("ExecuteProc ERROR: Entity: {entity_name} Mneo: {entity_def} Proc: {proc_name} not found in entity definition.", entity_name, entity.Def.Mneo, proc_name);
                    }
                }
                else
                {
                    _log.LogError("ExecuteProc ERROR: {entity_name} not found in entities type cache.", entity_name);
                }
            }
            finally
            {
                await ec.Disconnect();
            }

            return result;

        }

        public async Task<DBStatusResult?> HandleExecuteProcDBStatus(IAuthenticationProvider auth, string app_id, string entity_name, string proc_name, DataWebAPIRequest parms, IEntityClient ec, CancellationToken ct)
        {
            return await ExecuteProcDBStatus(auth, app_id, entity_name, proc_name, parms, ec, ct);
        }

        public async Task<CSVImportResult?> HandleImportData(IAuthenticationProvider auth, string app_id, string entity_name, string? import_proc, DataWebAPIRequest parms, IEntityClient ec, CancellationToken ct)
        {
            try
            {
                ApplicationOption? app = _app_config.GetAppConfiguration(app_id, ct);

                if (app == null)
                {
                    _log.LogError("ImportData ERROR: {app_id} not found.", app_id);
                    return null;
                }

                // validate entity
                var entity = CreateEntity(app, entity_name, parms.ServerClaims, auth, ec);
                if (entity != null)
                {

                    if (!string.IsNullOrEmpty(import_proc) && !entity.Def.Procs.ContainsKey(import_proc))
                    {
                        _log.LogError("ImportData ERROR: {entity_name} {import_proc} not defined.", entity_name, import_proc);
                    }
                    else if (!string.IsNullOrEmpty(import_proc) && entity.Def.Procs.ContainsKey(import_proc) && entity.Def.Procs[import_proc].isImport == false)
                    {
                        _log.LogError("ImportData ERROR: {entity_name} {import_proc} not marked (isImport == false).", entity_name, import_proc);
                    }
                    else
                    {
                        await ec.Connect(ct);
                        var import_process = new ImportProcess(ec);

                        import_process.SetColumnValue(import_process.Def.c_fileprocess_id.Name, parms.Values);
                        import_process.Def.vc_assemblytypename.Value = entity_name;
                        import_process.Def.vc_import_procname.Value = import_proc;

                        var procresult = await import_process.InsertData(ct, options: _options, server_claims: parms.ServerClaims, api: this, app_id: app_id);
                        if (!procresult.Failed)
                        {
                            // get the file guid
                            await import_process.GetData(ct);

                            if (string.IsNullOrEmpty(import_process.Def.vc_fileguid.Value))
                            {
                                await import_process.UpdateStatus(nameof(ImportStatus.Error), ct);
                                _log.LogError("ImportData ERROR: {entity_name} {import_proc} no file guid found.", entity_name, import_proc);
                                return null;
                            }

                            var file_path = await _upload.GetFilePath(app_id, import_process.Def.vc_fileguid.Value, ec, ct);
                            if (string.IsNullOrEmpty(file_path))
                            {
                                await import_process.UpdateStatus(nameof(ImportStatus.Error), ct);
                                _log.LogError("ImportData ERROR: {entity_name} {import_proc} no file path found.", entity_name, import_proc);
                                return null;
                            }

                            // Get extension from file_path, only support .xls, .xlsx, .csv
                            var ext = Path.GetExtension(file_path);
                            if (ext != ".csv" && ext != ".xls" && ext != ".xlsx")
                            {
                                await import_process.UpdateStatus(nameof(ImportStatus.Error), ct);
                                _log.LogError("ImportData ERROR: {entity_name} {import_proc} invalid file extension: {ext}.", entity_name, import_proc, ext);
                                return null;
                            }

                            // Override the parentkeys
                            if (parms.ParentKeys != null && parms.ParentKeys.Count > 0)
                            {
                                EnsureApplicationKeys(app_id, parms.ParentKeys);
                            }

                            try
                            {
                                await import_process.UpdateStatus(nameof(ImportStatus.Importing), ct);

                                if(ext == ".csv")
                                {
                                    var csv = await CSVParser.ParseFile(file_path, ct);

                                    if (csv != null)
                                    {
                                        var result = await entity.ImportDataFromCSV(csv, _options, parms.ServerClaims, this, app_id, parms.ParentKeys, ct);

                                        if (result != null)
                                        {
                                            await import_process.UpdateStatus(nameof(ImportStatus.Completed), ct);
                                            return result;
                                        }
                                        else
                                        {
                                            await import_process.UpdateStatus(nameof(ImportStatus.Error), ct);
                                            _log.LogError("ImportData ERROR (null result): {app_id} {entity_name} {import_proc}.", app_id, entity_name, import_proc);
                                        }
                                    }
                                    else
                                    {
                                        await import_process.UpdateStatus(nameof(ImportStatus.FormatError), ct);
                                        _log.LogError("ImportData FormatERROR: {app_id} {entity_name} {import_proc}.", app_id, entity_name, import_proc);
                                    }
                                }
                                else
                                {
                                    using var stream = new FileStream(file_path, FileMode.Open, FileAccess.Read);
                                    var result = await entity.ImportDataFromExcel(stream, null, null, _options, parms.ServerClaims, this, app_id, parms.ParentKeys, ct);
                                    if (result != null)
                                    {
                                        await import_process.UpdateStatus(nameof(ImportStatus.Completed), ct);
                                        return result;
                                    }
                                    else
                                    {
                                        await import_process.UpdateStatus(nameof(ImportStatus.Error), ct);
                                        _log.LogError("ImportData ERROR (null result): {app_id} {entity_name} {import_proc}.", app_id, entity_name, import_proc);
                                    }
                                }

                            }
                            catch (Exception ex)
                            {
                                _log.LogError(ex, "ImportData ERROR: {app_id} {entity_name} {import_proc}.", app_id, entity_name, import_proc);
                                await import_process.UpdateStatus(nameof(ImportStatus.FormatError), ct);
                            }


                        }

                        return null;
                    }
                }
                else
                {
                    _log.LogError("ImportData ERROR: {entity_name} not found in entities type cache.", entity_name);
                }

            }
            finally
            {
                await ec.Disconnect();
            }

            return null;
        }

        #endregion

        #region "Actions"


        private async Task<EntityActionResult?> ExecuteAction(IAuthenticationProvider auth, string app_id, string entity_name, string action_name, DataWebAPIRequest parms, IEntityClient ec, CancellationToken ct)
        {
            EntityActionResult? result = null;
            try
            {
                ApplicationOption? app = _app_config.GetAppConfiguration(app_id, ct);

                if (app == null) return null;
                auth.GetAuthenticator(app)?.UnencryptClaims(parms.ServerClaims);

                var entity = CreateEntity(app, entity_name, parms.ServerClaims, auth, ec);
                if (entity != null)
                {

                    if (parms.Values != null)
                    {
                        EnsureApplicationKeys(app_id, parms.Values);
                        entity.SetColumnValues(parms.Values);
                        if (parms.ParentKeys != null && parms.ParentKeys.Count > 0)
                        {
                            EnsureApplicationKeys(app_id, parms.ParentKeys);
                            entity.SetKeyValues(parms.ParentKeys);
                        }
                    }

                    result = await entity.ExecuteAction(action_name, parms, _options, this, _encryptor, ct, app_id: app_id);
                }
                else
                {
                    _log.LogError("UpdateEntity ERROR: {entity_name} not found in entities type cache.", entity_name);
                }

            }
            finally
            {
                await ec.Disconnect();
            }

            return result;

        }

        public async Task<EntityActionResult?> HandleExecuteAction(IAuthenticationProvider auth, string app_id, string entity_name, string entity_action, DataWebAPIRequest parms, IEntityClient ec, CancellationToken ct)
        {
            return await ExecuteAction(auth, app_id, entity_name, entity_action, parms, ec, ct);
        }

        #endregion

        #region "File Upload / Serve"
        public async Task<UploadFileResult> HandleUpload(string app_id, string fileprocess_id, string file_name, Stream file_data, int? maxSize, int? quality, IEntityClient ec, CancellationToken ct)
        {
            return await _upload.UploadFile(app_id, fileprocess_id, file_name, file_data, maxSize, quality, ec, ct);
        }

        public async Task<ServeFileResult?> HandleServe(string app_id, string fileguid, IEntityClient ec, CancellationToken ct)
        {
            return await _upload.ServeFile(app_id, fileguid, ec, ct);
        }

        public async Task<ServeFileResult?> HandleServeThumbnail(string app_id, string fileguid, int? maxSize, int? quality, IEntityClient ec, CancellationToken ct)
        {
            return await _upload.ServeThumbnail(app_id, fileguid, maxSize, quality, ec, ct);
        }

        #endregion

    }




}

