using MicroM.Configuration;
using MicroM.Core;
using MicroM.Data;
using MicroM.DataDictionary;
using MicroM.DataDictionary.CategoriesDefinitions;
using MicroM.DataDictionary.StatusDefs;
using MicroM.Extensions;
using MicroM.ImportData;
using MicroM.Web.Authentication;
using MicroM.Web.Services.Security;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Collections.Concurrent;
using System.Text.Json;

namespace MicroM.Web.Services;

/// <summary>
/// Central logic for executing entity CRUD operations, lookups, imports and actions.
/// Maintains application keys and time zone offsets in thread-safe caches.
/// </summary>
/// <remarks>
/// Updates global <see cref="DataDefaults"/> based on configured options.
/// </remarks>
public class EntitiesService : IEntitiesService
{
    private readonly ConcurrentDictionary<string, Dictionary<string, object>> _ApplicationKeys = new(StringComparer.OrdinalIgnoreCase);
    private readonly ConcurrentDictionary<string, int> _ApplicationsTimeZoneOffset = new(StringComparer.OrdinalIgnoreCase);

    private readonly WebAPIServices _api;
    private readonly MicroMOptions _options;

    /// <summary>
    /// Initializes a new instance of the <see cref="EntitiesService"/> class with required services and options.
    /// </summary>
    /// <param name="options">Application-wide configuration values.</param>
    /// <param name="logger">Logger used by lower level API services.</param>
    /// <param name="encryptor">Encryption service for protecting sensitive values.</param>
    /// <param name="app_config">Application configuration provider.</param>
    /// <param name="queue">Background task queue for asynchronous jobs.</param>
    /// <param name="upload">File upload service.</param>
    /// <param name="emailService">Email service used for notifications.</param>
    /// <param name="securityService">Security service for permission checks.</param>
    /// <param name="deviceIdService">Service used to resolve client device information.</param>
    /// <param name="authenticationService">Authentication service for user context.</param>
    /// <remarks>
    /// Updates static values in <see cref="DataDefaults"/> based on the provided options.
    /// </remarks>
    public EntitiesService(IOptions<MicroMOptions> options,
            ILogger<WebAPIServices> logger,
            IMicroMEncryption encryptor,
            IMicroMAppConfiguration app_config,
            IBackgroundTaskQueue queue,
            IFileUploadService upload,
            IEmailService emailService,
            ISecurityService securityService,
            IDeviceIdService deviceIdService,
            IAuthenticationService authenticationService
            )
    {
        _api = new(logger, encryptor, app_config, queue, upload, emailService, securityService, deviceIdService, this, authenticationService);

        ArgumentNullException.ThrowIfNull(options);

        _options = options.Value;

        if (_options.DefaultConnectionTimeOutInSecs != -1) DataDefaults.DefaultConnectionTimeOutInSecs = _options.DefaultConnectionTimeOutInSecs;
        if (_options.DefaultRowLimitForViews != -1) DataDefaults.DefaultRowLimitForViews = _options.DefaultRowLimitForViews;
        if (_options.DefaultCommandTimeOutInMins != -1) DataDefaults.DefaultCommandTimeOutInMins = _options.DefaultCommandTimeOutInMins;
    }

    /// <summary>
    /// Creates a database connection using application settings and optional server claims.
    /// </summary>
    /// <param name="app">Application-specific configuration.</param>
    /// <param name="server_claims">Server claims that may include user and device information.</param>
    /// <returns>A configured <see cref="IEntityClient"/>.</returns>
    /// <remarks>
    /// Retrieves device information to populate connection metadata. Thread-safe.
    /// </remarks>
    public IEntityClient CreateDbConnection(ApplicationOption app, Dictionary<string, object>? server_claims)
    {
        string user = app.AuthenticationType == nameof(AuthenticationTypes.SQLServerAuthentication) && string.IsNullOrEmpty(app.SQLUser) ? (string?)server_claims?[MicroMServerClaimTypes.MicroMUsername] ?? "" : app.SQLUser;
        string pass = app.AuthenticationType == nameof(AuthenticationTypes.SQLServerAuthentication) && string.IsNullOrEmpty(app.SQLUser) ? (string?)server_claims?[MicroMServerClaimTypes.MicroMPassword] ?? "" : app.SQLPassword;

        string local_device_id = "";
        if (server_claims != null)
        {
            server_claims.TryGetValue(MicroMServerClaimTypes.MicroMUserDeviceID, out var local_device_claim);

            local_device_id = local_device_claim?.ToString() ?? "";
        }

        var (device_id, ipaddress, user_agent) = _api.deviceIdService.GetDeviceID(local_device_id);
        string workstation_id = $"{ipaddress} {device_id} {user_agent}".Truncate(128);

        DatabaseClient dbc = new(server: app.SQLServer, user: user, password: pass, db: app.SQLDB, logger: _api.log, server_claims: server_claims)
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

    /// <summary>
    /// Asynchronously creates a database connection with user and device context.
    /// </summary>
    /// <param name="app">Application-specific configuration.</param>
    /// <param name="server_claims">Server claims that may include user and device information.</param>
    /// <param name="ct">Token used to cancel the operation.</param>
    /// <returns>A task that returns the configured <see cref="IEntityClient"/>.</returns>
    /// <remarks>Thread-safe.</remarks>
    public Task<IEntityClient> CreateDbConnection(ApplicationOption app, Dictionary<string, object>? server_claims, CancellationToken ct)
    {
        return Task.FromResult(CreateDbConnection(app, server_claims));
    }

    /// <summary>
    /// Instantiates an entity type and binds it to the supplied or newly created connection.
    /// </summary>
    /// <param name="app">Application-specific configuration.</param>
    /// <param name="entity_name">Name of the entity to instantiate.</param>
    /// <param name="server_claims">Server claims for user context.</param>
    /// <param name="ec">Existing client connection to reuse; if <c>null</c>, a new connection is created.</param>
    /// <returns>The instantiated entity or <c>null</c> if the type is not found.</returns>
    public EntityBase? CreateEntity(ApplicationOption app, string entity_name, Dictionary<string, object>? server_claims, IEntityClient? ec = null)
    {
        EntityBase? entity = null;
        ec ??= CreateDbConnection(app, server_claims);
        Type? ent_type = _api.app_config.GetEntityType(app.ApplicationID, entity_name);
        if (ent_type != null) entity = (EntityBase?)Activator.CreateInstance(ent_type, ec, _api.encryptor);

        return entity;
    }

    /// <summary>
    /// Asynchronously creates an entity using a newly established connection.
    /// </summary>
    /// <param name="app">Application-specific configuration.</param>
    /// <param name="entity_name">Name of the entity to instantiate.</param>
    /// <param name="server_claims">Server claims for user context.</param>
    /// <param name="ct">Token used to cancel the operation.</param>
    /// <returns>The instantiated entity or <c>null</c> if the type is not found.</returns>
    public EntityBase? CreateEntity(ApplicationOption app, string entity_name, Dictionary<string, object>? server_claims, CancellationToken ct)
    {
        EntityBase? entity = null;
        var ec = CreateDbConnection(app, server_claims, ct);
        Type? ent_type = _api.app_config.GetEntityType(app.ApplicationID, entity_name);
        if (ent_type != null) entity = (EntityBase?)Activator.CreateInstance(ent_type, ec, _api.encryptor);

        return entity;
    }

    /// <summary>
    /// Merges cached application-level keys into the provided dictionary.
    /// </summary>
    /// <param name="app_id">Application identifier whose keys are applied.</param>
    /// <param name="values">Dictionary to augment with key values. Modified in place.</param>
    /// <remarks>Thread-safe.</remarks>
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

    /// <summary>
    /// Retrieves cached application-level keys for the specified application.
    /// </summary>
    /// <param name="app_id">Application identifier.</param>
    /// <returns>A dictionary containing the application's keys or an empty dictionary.</returns>
    /// <remarks>Thread-safe.</remarks>
    public Dictionary<string, object> GetApplicationKeys(string app_id)
    {
        _ApplicationKeys.TryGetValue(app_id, out Dictionary<string, object>? app_keys);
        return app_keys ?? new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Deletes entity records matching the supplied parameters.
    /// </summary>
    /// <param name="app">Application context.</param>
    /// <param name="entity_name">Name of the entity to delete.</param>
    /// <param name="parms">Request values and selection criteria.</param>
    /// <param name="ec">Database client used for the operation.</param>
    /// <param name="ct">Token used to cancel the operation.</param>
    /// <returns>Aggregated status for the delete operation or <c>null</c> if the entity is not found.</returns>
    /// <remarks>Disconnects the provided client when finished.</remarks>
    public async Task<DBStatusResult?> HandleDeleteEntity(ApplicationOption app, string entity_name, DataWebAPIRequest parms, IEntityClient ec, CancellationToken ct)
    {
        DBStatusResult? result = null;

        try
        {
            string app_id = app.ApplicationID;

            var entity = CreateEntity(app, entity_name, parms.ServerClaims, ec);
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
                    result = await entity.DeleteData(ct, options: _options, server_claims: parms.ServerClaims, api: _api, app_id: app_id);
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

                        var record_result = await entity.DeleteData(ct, options: _options, server_claims: parms.ServerClaims, api: _api, app_id: app_id);
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
                _api.log.LogError("DeleteEntity ERROR: {entity_name} not found in entities type cache.", entity_name);
            }
        }
        finally
        {
            await ec.Disconnect();
        }

        return result;

    }

    /// <summary>
    /// Executes a named action on the entity using the supplied parameters.
    /// </summary>
    /// <param name="app">Application context.</param>
    /// <param name="entity_name">Name of the entity.</param>
    /// <param name="entity_action">Action identifier to execute.</param>
    /// <param name="parms">Request values passed to the action.</param>
    /// <param name="ec">Database client used for the operation.</param>
    /// <param name="ct">Token used to cancel the operation.</param>
    /// <returns>The action result or <c>null</c> if the entity is not found.</returns>
    /// <remarks>Disconnects the provided client when finished.</remarks>
    public async Task<EntityActionResult?> HandleExecuteAction(ApplicationOption app, string entity_name, string entity_action, DataWebAPIRequest parms, IEntityClient ec, CancellationToken ct)
    {
        EntityActionResult? result = null;
        try
        {
            string app_id = app.ApplicationID;

            var entity = CreateEntity(app, entity_name, parms.ServerClaims, ec);
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

                result = await entity.ExecuteAction(entity_action, parms, _options, _api, _api.encryptor, ct, app_id: app_id);
            }
            else
            {
                _api.log.LogError("UpdateEntity ERROR: {entity_name} not found in entities type cache.", entity_name);
            }

        }
        finally
        {
            await ec.Disconnect();
        }

        return result;

    }

    /// <summary>
    /// Executes an entity-defined stored procedure and returns its result sets.
    /// </summary>
    /// <param name="app">Application context.</param>
    /// <param name="entity_name">Name of the entity that defines the procedure.</param>
    /// <param name="proc_name">Name of the procedure to execute.</param>
    /// <param name="parms">Request values applied to the procedure.</param>
    /// <param name="ec">Database client used for the operation.</param>
    /// <param name="ct">Token used to cancel the operation.</param>
    /// <returns>A list of <see cref="DataResult"/> objects or <c>null</c> if the entity is not found.</returns>
    /// <remarks>Disconnects the provided client when finished.</remarks>
    public async Task<List<DataResult>?> HandleExecuteProc(ApplicationOption app, string entity_name, string proc_name, DataWebAPIRequest parms, IEntityClient ec, CancellationToken ct)
    {
        List<DataResult> result = [];

        try
        {
            string app_id = app.ApplicationID;

            var entity = CreateEntity(app, entity_name, parms.ServerClaims, ec);
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
                        result = await entity.ExecuteProc(ct, proc, options: _options, server_claims: parms.ServerClaims, api: _api, set_parms_from_columns: false, app_id: app_id);
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

                            var record_result = await entity.ExecuteProc(ct, proc, options: _options, server_claims: parms.ServerClaims, api: _api, set_parms_from_columns: false, app_id: app_id);
                            if (record_result != null)
                            {
                                results.AddRange(record_result);
                            }
                        }

                    }
                }
                else
                {
                    _api.log.LogError("ExecuteProc ERROR: Entity: {entity_name} Mneo: {entity_def} Proc: {proc_name} not found in entity definition.", entity_name, entity.Def.Mneo, proc_name);
                }
            }
            else
            {
                _api.log.LogError("ExecuteProc ERROR: {entity_name} not found in entities type cache.", entity_name);
            }
        }
        finally
        {
            await ec.Disconnect();
        }

        return result;


    }

    /// <summary>
    /// Executes an entity-defined stored procedure and returns database status information.
    /// </summary>
    /// <param name="app">Application context.</param>
    /// <param name="entity_name">Name of the entity that defines the procedure.</param>
    /// <param name="proc_name">Name of the procedure to execute.</param>
    /// <param name="parms">Request values applied to the procedure.</param>
    /// <param name="ec">Database client used for the operation.</param>
    /// <param name="ct">Token used to cancel the operation.</param>
    /// <returns>Status results produced by the procedure or <c>null</c> if the entity is not found.</returns>
    /// <remarks>Disconnects the provided client when finished.</remarks>
    public async Task<DBStatusResult?> HandleExecuteProcDBStatus(ApplicationOption app, string entity_name, string proc_name, DataWebAPIRequest parms, IEntityClient ec, CancellationToken ct)
    {
        DBStatusResult result = new();

        try
        {
            string app_id = app.ApplicationID;

            var entity = CreateEntity(app, entity_name, parms.ServerClaims, ec);
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
                        result = await entity.ExecuteProcessDBStatus(ct, proc, options: _options, server_claims: parms.ServerClaims, api: _api, set_parms_from_columns: false, app_id: app_id);
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

                            var record_result = await entity.ExecuteProcessDBStatus(ct, proc, options: _options, server_claims: parms.ServerClaims, api: _api, set_parms_from_columns: false, app_id: app_id);
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
                    _api.log.LogError("ExecuteProc ERROR: Entity: {entity_name} Mneo: {entity_def} Proc: {proc_name} not found in entity definition.", entity_name, entity.Def.Mneo, proc_name);
                }
            }
            else
            {
                _api.log.LogError("ExecuteProc ERROR: {entity_name} not found in entities type cache.", entity_name);
            }
        }
        finally
        {
            await ec.Disconnect();
        }

        return result;

    }

    /// <summary>
    /// Executes a view defined on the entity and returns its result sets.
    /// </summary>
    /// <param name="app">Application context.</param>
    /// <param name="entity_name">Name of the entity that defines the view.</param>
    /// <param name="view_name">Name of the view to execute.</param>
    /// <param name="parms">Request values applied to the view.</param>
    /// <param name="ec">Database client used for the operation.</param>
    /// <param name="ct">Token used to cancel the operation.</param>
    /// <returns>A list of <see cref="DataResult"/> objects or <c>null</c> if the entity is not found.</returns>
    /// <remarks>Disconnects the provided client when finished.</remarks>
    public async Task<List<DataResult>?> HandleExecuteView(ApplicationOption app, string entity_name, string view_name, DataWebAPIRequest parms, IEntityClient ec, CancellationToken ct)
    {
        List<DataResult>? result = null;

        try
        {
            string app_id = app.ApplicationID;

            var entity = CreateEntity(app, entity_name, parms.ServerClaims, ec);
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
                    result = await entity.ExecuteView(ct, view, row_limit, options: _options, server_claims: parms.ServerClaims, api: _api, app_id: app_id);
                }
                else
                {
                    _api.log.LogError("ExecuteView ERROR: Entity: {entity_name} Mneo: {entity_def} View: {view_name} not found in entity definition.", entity_name, entity.Def.Mneo, view_name);
                }
            }
            else
            {
                _api.log.LogError("ExecuteView ERROR: {entity_name} not found in entities type cache.", entity_name);
            }
        }
        finally
        {
            await ec.Disconnect();
        }

        return result;

    }

    /// <summary>
    /// Retrieves an entity record using the provided key values.
    /// </summary>
    /// <param name="app">Application context.</param>
    /// <param name="entity_name">Name of the entity to retrieve.</param>
    /// <param name="parms">Request containing key values and additional options.</param>
    /// <param name="ec">Database client used for the operation.</param>
    /// <param name="ct">Token used to cancel the operation.</param>
    /// <returns>A dictionary of column values or <c>null</c> if the entity is not found.</returns>
    /// <remarks>Disconnects the provided client when finished.</remarks>
    public async Task<Dictionary<string, object?>?> HandleGetEntity(ApplicationOption app, string entity_name, DataWebAPIRequest parms, IEntityClient ec, CancellationToken ct)
    {
        Dictionary<string, object?>? result = null;
        try
        {
            string app_id = app.ApplicationID;

            var entity = CreateEntity(app, entity_name, parms.ServerClaims, ec);
            if (entity != null)
            {

                EnsureApplicationKeys(app_id, parms.Values);
                entity.SetKeyValues(parms.Values);
                if (parms.ParentKeys != null && parms.ParentKeys.Count > 0)
                {
                    EnsureApplicationKeys(app_id, parms.ParentKeys);
                    entity.SetKeyValues(parms.ParentKeys);
                }
                await entity.GetData(ct, _options, parms.ServerClaims, api: _api, app_id: app_id);
                result = entity.Def.Columns.ToDictionary(new HashSet<string>(StringComparer.Ordinal) { SystemColumnNames.webusr });
            }
            else
            {
                _api.log.LogError("GetEntity ERROR: {entity_name} not found in entities type cache.", entity_name);
            }
        }
        finally
        {
            await ec.Disconnect();
        }

        return result;
    }

    /// <summary>
    /// Retrieves metadata definition for the specified entity.
    /// </summary>
    /// <param name="app">Application context.</param>
    /// <param name="entity_name">Name of the entity.</param>
    /// <returns>The entity definition or <c>null</c> if the type is not found.</returns>
    public EntityDefinition? HandleGetEntityDefinition(ApplicationOption app, string entity_name)
    {
        EntityDefinition? result = null;

        string app_id = app.ApplicationID;

        Type? ent_type = _api.app_config.GetEntityType(app.ApplicationID, entity_name);
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
            _api.log.LogError("GetEntityDefinition ERROR: {entity_name} not found in entities type cache for application {app_id}.", entity_name, app_id);
        }

        return result;
    }

    /// <summary>
    /// Gets the application's time zone offset, caching the result for subsequent calls.
    /// </summary>
    /// <param name="app">Application context.</param>
    /// <param name="ec">Database client used to query the offset.</param>
    /// <param name="ct">Token used to cancel the operation.</param>
    /// <returns>The time zone offset in minutes.</returns>
    /// <remarks>Results are cached per application and the client is disconnected when finished.</remarks>
    public async Task<int> HandleGetTimeZoneOffset(ApplicationOption app, IEntityClient ec, CancellationToken ct)
    {
        try
        {
            string app_id = app.ApplicationID;

            if (_ApplicationsTimeZoneOffset.TryGetValue(app_id, out var offset))
            {
                return offset;
            }
            else
            {
                var sys = new SystemProcs(ec);
                var new_offset = await sys.ExecuteProcSingleColumn<int>(ct, sys.Def.sys_GetTimeZoneOffset);
                _ApplicationsTimeZoneOffset.TryAdd(app_id, new_offset);
                return new_offset;
            }
        }
        finally
        {
            await ec.Disconnect();
        }
    }


    /// <summary>
    /// Imports data for an entity from an uploaded file using an optional import procedure.
    /// </summary>
    /// <param name="app">Application context.</param>
    /// <param name="entity_name">Name of the target entity.</param>
    /// <param name="import_proc">Optional name of the import procedure to execute.</param>
    /// <param name="parms">Request containing file and parameter information.</param>
    /// <param name="ec">Database client used for the operation.</param>
    /// <param name="ct">Token used to cancel the operation.</param>
    /// <returns>Result of the import process or <c>null</c> on failure.</returns>
    /// <remarks>Disconnects the provided client when finished.</remarks>
    public async Task<CSVImportResult?> HandleImportData(ApplicationOption app, string entity_name, string? import_proc, DataWebAPIRequest parms, IEntityClient ec, CancellationToken ct)
    {
        try
        {
            string app_id = app.ApplicationID;

            // validate entity
            var entity = CreateEntity(app, entity_name, parms.ServerClaims, ec);
            if (entity != null)
            {

                if (!string.IsNullOrEmpty(import_proc) && !entity.Def.Procs.ContainsKey(import_proc))
                {
                    _api.log.LogError("ImportData ERROR: {entity_name} {import_proc} not defined.", entity_name, import_proc);
                }
                else if (!string.IsNullOrEmpty(import_proc) && entity.Def.Procs.ContainsKey(import_proc) && entity.Def.Procs[import_proc].isImport == false)
                {
                    _api.log.LogError("ImportData ERROR: {entity_name} {import_proc} not marked (isImport == false).", entity_name, import_proc);
                }
                else
                {
                    await ec.Connect(ct);
                    var import_process = new ImportProcess(ec);

                    import_process.SetColumnValue(import_process.Def.c_fileprocess_id.Name, parms.Values);
                    import_process.Def.vc_assemblytypename.Value = entity_name;
                    import_process.Def.vc_import_procname.Value = import_proc;

                    var procresult = await import_process.InsertData(ct, options: _options, server_claims: parms.ServerClaims, api: _api, app_id: app_id);
                    if (!procresult.Failed)
                    {
                        // get the file guid
                        await import_process.GetData(ct);

                        if (string.IsNullOrEmpty(import_process.Def.vc_fileguid.Value))
                        {
                            await import_process.UpdateStatus(nameof(ImportStatus.Error), ct);
                            _api.log.LogError("ImportData ERROR: {entity_name} {import_proc} no file guid found.", entity_name, import_proc);
                            return null;
                        }

                        var file_path = await _api.upload.GetFilePath(app_id, import_process.Def.vc_fileguid.Value, ec, ct);
                        if (string.IsNullOrEmpty(file_path))
                        {
                            await import_process.UpdateStatus(nameof(ImportStatus.Error), ct);
                            _api.log.LogError("ImportData ERROR: {entity_name} {import_proc} no file path found.", entity_name, import_proc);
                            return null;
                        }

                        // Get extension from file_path, only support .xls, .xlsx, .csv
                        var ext = Path.GetExtension(file_path);
                        if (ext != ".csv" && ext != ".xls" && ext != ".xlsx")
                        {
                            await import_process.UpdateStatus(nameof(ImportStatus.Error), ct);
                            _api.log.LogError("ImportData ERROR: {entity_name} {import_proc} invalid file extension: {ext}.", entity_name, import_proc, ext);
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

                            if (ext == ".csv")
                            {
                                var csv = await CSVParser.ParseFile(file_path, ct);

                                if (csv != null)
                                {
                                    var result = await entity.ImportDataFromCSV(csv, _options, parms.ServerClaims, _api, app_id, parms.ParentKeys, ct);

                                    if (result != null)
                                    {
                                        await import_process.UpdateStatus(nameof(ImportStatus.Completed), ct);
                                        return result;
                                    }
                                    else
                                    {
                                        await import_process.UpdateStatus(nameof(ImportStatus.Error), ct);
                                        _api.log.LogError("ImportData ERROR (null result): {app_id} {entity_name} {import_proc}.", app_id, entity_name, import_proc);
                                    }
                                }
                                else
                                {
                                    await import_process.UpdateStatus(nameof(ImportStatus.FormatError), ct);
                                    _api.log.LogError("ImportData FormatERROR: {app_id} {entity_name} {import_proc}.", app_id, entity_name, import_proc);
                                }
                            }
                            else
                            {
                                using var stream = new FileStream(file_path, FileMode.Open, FileAccess.Read);
                                var result = await entity.ImportDataFromExcel(stream, null, null, _options, parms.ServerClaims, _api, app_id, parms.ParentKeys, ct);
                                if (result != null)
                                {
                                    await import_process.UpdateStatus(nameof(ImportStatus.Completed), ct);
                                    return result;
                                }
                                else
                                {
                                    await import_process.UpdateStatus(nameof(ImportStatus.Error), ct);
                                    _api.log.LogError("ImportData ERROR (null result): {app_id} {entity_name} {import_proc}.", app_id, entity_name, import_proc);
                                }
                            }

                        }
                        catch (Exception ex)
                        {
                            _api.log.LogError(ex, "ImportData ERROR: {app_id} {entity_name} {import_proc}.", app_id, entity_name, import_proc);
                            await import_process.UpdateStatus(nameof(ImportStatus.FormatError), ct);
                        }


                    }

                    return null;
                }
            }
            else
            {
                _api.log.LogError("ImportData ERROR: {entity_name} not found in entities type cache.", entity_name);
            }

        }
        finally
        {
            await ec.Disconnect();
        }

        return null;

    }

    /// <summary>
    /// Inserts new entity records using the supplied values.
    /// </summary>
    /// <param name="app">Application context.</param>
    /// <param name="entity_name">Name of the entity to insert.</param>
    /// <param name="parms">Request containing column values and selection data.</param>
    /// <param name="ec">Database client used for the operation.</param>
    /// <param name="ct">Token used to cancel the operation.</param>
    /// <returns>Aggregated status for the insert operation or <c>null</c> if the entity is not found.</returns>
    /// <remarks>Disconnects the provided client when finished.</remarks>
    public async Task<DBStatusResult?> HandleInsertEntity(ApplicationOption app, string entity_name, DataWebAPIRequest parms, IEntityClient ec, CancellationToken ct)
    {
        DBStatusResult? result = null;
        try
        {
            string app_id = app.ApplicationID;

            var entity = CreateEntity(app, entity_name, parms.ServerClaims, ec);
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
                    result = await entity.InsertData(ct, options: _options, server_claims: parms.ServerClaims, api: _api, app_id: app_id);
                }
                else
                {
                    List<DBStatus> results = [];
                    bool failed = false;
                    var clonedValues = entity.Def.Columns.ToDictionary([.. SystemColumnNames.AsStringArray]).ToDictionary(kvp => kvp.Key, kvp => kvp.Value!);

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

                        var record_result = await entity.InsertData(ct, options: _options, server_claims: parms.ServerClaims, api: _api, app_id: app_id);
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
                _api.log.LogError("InsertEntity ERROR: {entity_name} not found in entities type cache.", entity_name);
            }

        }
        finally
        {
            await ec.Disconnect();
        }

        return result;
    }

    /// <summary>
    /// Performs a lookup for an entity and returns a descriptive value.
    /// </summary>
    /// <param name="app">Application context.</param>
    /// <param name="entity_name">Name of the entity.</param>
    /// <param name="parms">Request containing key values.</param>
    /// <param name="ec">Database client used for the operation.</param>
    /// <param name="ct">Token used to cancel the operation.</param>
    /// <param name="lookup_name">Optional lookup definition name.</param>
    /// <returns>A <see cref="LookupResult"/> containing the description.</returns>
    /// <remarks>Disconnects the provided client when finished.</remarks>
    public async Task<LookupResult> HandleLookupEntity(ApplicationOption app, string entity_name, DataWebAPIRequest parms, IEntityClient ec, CancellationToken ct, string? lookup_name = null)
    {
        string? result = null;

        try
        {
            string app_id = app.ApplicationID;

            var entity = CreateEntity(app, entity_name, parms.ServerClaims, ec);
            if (entity != null)
            {

                EnsureApplicationKeys(app_id, parms.Values);
                entity.SetKeyValues(parms.Values);
                if (parms.ParentKeys != null && parms.ParentKeys.Count > 0)
                {
                    EnsureApplicationKeys(app_id, parms.ParentKeys);
                    entity.SetKeyValues(parms.ParentKeys);
                }
                result = await entity.LookupData(ct, lookup_name, options: _options, server_claims: parms.ServerClaims, api: _api, app_id: app_id);
            }
            else
            {
                _api.log.LogError("LookupEntity ERROR: {entity_name} not found in entities type cache.", entity_name);
            }
        }
        finally
        {
            await ec.Disconnect();
        }

        return new LookupResult() { Description = result ?? "" };

    }

    /// <summary>
    /// Updates existing entity records with the supplied values.
    /// </summary>
    /// <param name="app">Application context.</param>
    /// <param name="entity_name">Name of the entity to update.</param>
    /// <param name="parms">Request containing column values and selection data.</param>
    /// <param name="ec">Database client used for the operation.</param>
    /// <param name="ct">Token used to cancel the operation.</param>
    /// <returns>Aggregated status for the update operation or <c>null</c> if the entity is not found.</returns>
    /// <remarks>Disconnects the provided client when finished.</remarks>
    public async Task<DBStatusResult?> HandleUpdateEntity(ApplicationOption app, string entity_name, DataWebAPIRequest parms, IEntityClient ec, CancellationToken ct)
    {
        DBStatusResult? result = null;

        try
        {
            string app_id = app.ApplicationID;

            var entity = CreateEntity(app, entity_name, parms.ServerClaims, ec);
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
                    result = await entity.UpdateData(ct, options: _options, server_claims: parms.ServerClaims, api: _api, app_id: app_id);
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

                        var record_result = await entity.UpdateData(ct, options: _options, server_claims: parms.ServerClaims, api: _api, app_id: app_id);
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
                _api.log.LogError("UpdateEntity ERROR: {entity_name} not found in entities type cache.", entity_name);
            }

        }
        finally
        {
            await ec.Disconnect();
        }

        return result;

    }

    /// <summary>
    /// Stores or replaces an application-level key value in the cache.
    /// </summary>
    /// <param name="app_id">Application identifier.</param>
    /// <param name="key">Key name to replace.</param>
    /// <param name="value">New value for the key.</param>
    /// <remarks>Thread-safe.</remarks>
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
}
