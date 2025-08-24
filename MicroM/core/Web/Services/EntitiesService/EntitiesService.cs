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
/// Represents the EntitiesService.
/// </summary>
public class EntitiesService : IEntitiesService
{
    private readonly ConcurrentDictionary<string, Dictionary<string, object>> _ApplicationKeys = new(StringComparer.OrdinalIgnoreCase);
    private readonly ConcurrentDictionary<string, int> _ApplicationsTimeZoneOffset = new(StringComparer.OrdinalIgnoreCase);

    private readonly WebAPIServices _api;
    private readonly MicroMOptions _options;

    /// <summary>
    /// Initializes a new instance of <see cref="EntitiesService"/>.
    /// </summary>
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
    /// Creates a new database client configured for the specified application.
    /// </summary>
    /// <param name="app">Application configuration.</param>
    /// <param name="server_claims">Claims used to resolve SQL credentials when required.</param>
    /// <returns>A configured <see cref="IEntityClient"/> ready for use.</returns>
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
    /// Asynchronously creates a new database client configured for the specified application.
    /// </summary>
    /// <param name="app">Application configuration.</param>
    /// <param name="server_claims">Claims used to resolve SQL credentials when required.</param>
    /// <param name="ct">Token to observe for cancellation.</param>
    /// <returns>A task producing a configured <see cref="IEntityClient"/>.</returns>
    /// <remarks>Throws <see cref="OperationCanceledException"/> when <paramref name="ct"/> is cancelled.</remarks>
    public Task<IEntityClient> CreateDbConnection(ApplicationOption app, Dictionary<string, object>? server_claims, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        return Task.FromResult(CreateDbConnection(app, server_claims));
    }

    /// <summary>
    /// Creates an Entity if it exists in the configured assembly.
    /// </summary>
    /// <param name="entity_name"></param>
    /// <param name="ec"></param>
    /// <returns></returns>
    public EntityBase? CreateEntity(ApplicationOption app, string entity_name, Dictionary<string, object>? server_claims, IEntityClient? ec = null)
    {
        EntityBase? entity = null;
        ec ??= CreateDbConnection(app, server_claims);
        Type? ent_type = _api.app_config.GetEntityType(app.ApplicationID, entity_name);
        if (ent_type != null) entity = (EntityBase?)Activator.CreateInstance(ent_type, ec, _api.encryptor);

        return entity;
    }

    /// <summary>
    /// Creates an entity instance for the specified application.
    /// </summary>
    public EntityBase? CreateEntity(ApplicationOption app, string entity_name, Dictionary<string, object>? server_claims, CancellationToken ct)
    {
        EntityBase? entity = null;
        var ec = CreateDbConnection(app, server_claims, ct);
        Type? ent_type = _api.app_config.GetEntityType(app.ApplicationID, entity_name);
        if (ent_type != null) entity = (EntityBase?)Activator.CreateInstance(ent_type, ec, _api.encryptor);

        return entity;
    }

    /// <summary>
    /// Ensures required application keys are present in the supplied values.
    /// </summary>
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
    /// Retrieves cached application key values.
    /// </summary>
    public Dictionary<string, object> GetApplicationKeys(string app_id)
    {
        _ApplicationKeys.TryGetValue(app_id, out Dictionary<string, object>? app_keys);
        return app_keys ?? new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Deletes records from the specified entity.
    /// </summary>
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
    /// Executes an action on the specified entity.
    /// </summary>
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
    /// Executes a stored procedure on the entity and returns its results.
    /// </summary>
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
    /// Executes a stored procedure and returns a database status.
    /// </summary>
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
    /// Executes a view and returns the resulting data.
    /// </summary>
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
    /// Retrieves an entity record from the database.
    /// </summary>
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
    /// Gets metadata definition for an entity.
    /// </summary>
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
    /// Obtains the time zone offset for the application.
    /// </summary>
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
    /// Imports data using the provided CSV information.
    /// </summary>
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
    /// Inserts a new record for the specified entity.
    /// </summary>
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
    /// Performs a lookup operation for the specified entity.
    /// </summary>
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
    /// Updates an existing entity record.
    /// </summary>
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
    /// Replaces the value of an application key.
    /// </summary>
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
