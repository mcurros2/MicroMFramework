using MicroM.Configuration;
using MicroM.Configuration.CategoriesDefinitions;
using MicroM.Configuration.Entities;
using MicroM.Core;
using MicroM.Data;
using MicroM.Database;
using MicroM.DataDictionary.Entities;
using MicroM.Extensions;
using MicroM.Web.Services.Security;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Collections.Concurrent;
using System.Reflection;
using static System.ArgumentNullException;

namespace MicroM.Web.Services;

public class MicroMAppConfigurationProvider : IHostedService, IMicroMAppConfiguration
{
    private static readonly Dictionary<string, ApplicationOption> _ApplicationsCache = new(StringComparer.OrdinalIgnoreCase);
    private static readonly Dictionary<string, Type> _EntityTypesCache = new(StringComparer.OrdinalIgnoreCase);
    private static readonly Dictionary<string, Type> _DDTypesCache = new(StringComparer.OrdinalIgnoreCase);
    private static readonly Dictionary<string, PublicEndpointSecurityRecord> _PublicAccessCache = new(StringComparer.OrdinalIgnoreCase);

    private static readonly Dictionary<string, List<IMicroMApplicationServices>> _ApplicationServicesCache = new(StringComparer.OrdinalIgnoreCase);

    private CancellationTokenSource? _reloadAfterLastChangeCts;

    private static readonly HashSet<string> _globalAllowedURLS = new(StringComparer.OrdinalIgnoreCase);

    private readonly ConcurrentDictionary<string, HashSet<string>> _appAllowedURLS = new(StringComparer.OrdinalIgnoreCase);

    private readonly MicroMOptions _options;
    private readonly ILogger<MicroMAppConfigurationProvider> _log;
    private readonly IMicroMEncryption _encryptor;
    private readonly IBackgroundTaskQueue _backgroundTaskQueue;
    private readonly IConfiguration _config;
    private readonly string _jwtkey;
    private readonly PathString _basePathString;

    private readonly IMemoryEventsService _bus;

    private readonly IAppAssemblyRuntimeManager _assemblyRuntime;
    private readonly IAssemblyShadowCopyService _assemblyShadowCopy;

    private bool _startupShadowCopyCleaned;


    private static string NormalizeURL(string url)
    {
        if (string.IsNullOrWhiteSpace(url)) return "";
        url = url.Trim().TrimEnd('/');
        return url;
    }

    public MicroMAppConfigurationProvider(
        IOptions<MicroMOptions> options,
        ILogger<MicroMAppConfigurationProvider> logger,
        IMicroMEncryption encryptor,
        IBackgroundTaskQueue queue,
        IMemoryEventsService bus,
        IConfiguration config,
        IAppAssemblyRuntimeManager assemblyRuntime,
        IAssemblyShadowCopyService assemblyShadowCopy)
    {
        ThrowIfNull(options);

        _log = logger;
        _options = options.Value;
        _encryptor = encryptor;
        _backgroundTaskQueue = queue;
        _config = config;
        _bus = bus;
        _assemblyRuntime = assemblyRuntime;
        _assemblyShadowCopy = assemblyShadowCopy;

        _jwtkey = CryptClass.GenerateRandomBase64String(32);

        var raw = _options.MicroMAPIBaseRootPath ?? string.Empty;
        var trimmed = raw.Trim().Trim('/');

        _basePathString = string.IsNullOrEmpty(trimmed) ? PathString.Empty : new PathString("/" + trimmed);

        _options.UploadsFolder ??= Path.Combine(ConfigurationDefaults.UploadsFolder, _options.ConfigSQLServerDB ?? ConfigurationDefaults.SQLConfigDatabaseName, "uploads");

        _options.DiskFileCacheOptions ??= new DiskFileCacheOptions();

        if (string.IsNullOrEmpty(_options.DiskFileCacheOptions.RootPath))
        {
            _options.DiskFileCacheOptions.RootPath = Path.Combine(ConfigurationDefaults.DiskFileCacheFolder, _options.ConfigSQLServerDB ?? ConfigurationDefaults.SQLConfigDatabaseName, "disk_cache");
        }

        _log.LogTrace("initialized");
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        _log.LogInformation("MicroM APP Configuration Service starting");

        if (!_startupShadowCopyCleaned)
        {
            await _assemblyShadowCopy.DeleteAllGenerationsAsync(cancellationToken);
            _startupShadowCopyCleaned = true;
        }

        await ReloadConfiguration(cancellationToken);

    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        _reloadAfterLastChangeCts?.Cancel();
        _reloadAfterLastChangeCts?.Dispose();
        _assemblyRuntime.DisableFileWatchers();

        await StopApplicationServices();

        _log.LogInformation("MicroM APP Configuration Service stopped");

    }

    public ApplicationOption? GetAppConfiguration(string app_id)
    {
        _ApplicationsCache.TryGetValue(app_id, out var app);
        return app;
    }

    private async Task<(SecretsOptions? secrets, string? thumbprint)> ReadConfigurationDBParms(CancellationToken ct)
    {

        // MMC: a certificate was found, try to get sql configuration user from secrets
        SecretsOptions? result = null;
        string? thumbprint = _encryptor.CertificateThumbprint;

        string config_path = Path.Combine(ConfigurationDefaults.SecretsFilePath, ConfigurationDefaults.MicroMCommonID, ConfigurationDefaults.SecretsFilename);
        if (File.Exists(config_path))
        {
            string encrypted = await File.ReadAllTextAsync(config_path, ct);
            result = _encryptor.DecryptObject<SecretsOptions>(encrypted);
        }

        return (result, thumbprint);
    }

    private async Task AddControlPanelApp(CancellationToken ct)
    {

        var (secrets, thumbprint) = await ReadConfigurationDBParms(ct);
        if (thumbprint == null)
        {
            _log.LogError("ERROR: There is no certificate configured");
        }
        if (secrets == null)
        {
            _log.LogError("ERROR: The API is not configured. Secrets not found {secrets}", Path.Combine(ConfigurationDefaults.SecretsFilePath, ConfigurationDefaults.MicroMCommonID, ConfigurationDefaults.SecretsFilename));
        }

        _ApplicationsCache.TryGetValue(ConfigurationDefaults.ControlPanelAppID, out var control_panel);

        if (control_panel == null)
        {
            control_panel = new()
            {
                ApplicationID = ConfigurationDefaults.ControlPanelAppID,
                ApplicationName = "MicroM Control Panel",
                AuthenticationType = nameof(AuthenticationTypes.SQLServerAuthentication),
                AccountLockoutMinutes = 15,
                JWTAudience = "localhost",
                JWTIssuer = "localhost",
                JWTKey = _jwtkey,
                JWTRefreshExpirationHours = 60,
                JWTTokenExpirationMinutes = 60,
                MaxBadLogonAttempts = 5,
                SQLDB = _options.ConfigSQLServerDB ?? ConfigurationDefaults.SQLConfigDatabaseName,
                SQLServer = _options.ConfigSQLServer ?? "",
                SQLUser = secrets?.ConfigSQLUser ?? "",
                SQLPassword = secrets?.ConfigSQLPassword ?? "",
                MaxRefreshTokenAttempts = 15,
                SchemaConfiguration = ConfigurationDefaults.SchemaConfiguration,
            };
        }
        else
        {
            control_panel.SQLUser = secrets?.ConfigSQLUser ?? "";
            control_panel.SQLPassword = secrets?.ConfigSQLPassword ?? "";
        }

        _ApplicationsCache.TryAdd(control_panel.ApplicationID, control_panel);
    }


    public static IReadOnlyDictionary<string, Type> EntityTypesCache => _EntityTypesCache;

    private static string GetCoreAssemblyPath()
    {
        return typeof(MicromUsers).Assembly.Location;
    }

    private static readonly TimeSpan _reloadAfterLastChangeDelay = TimeSpan.FromSeconds(2);

    private async Task ScheduleReloadAfterLastChange()
    {
        try
        {
            _reloadAfterLastChangeCts?.Cancel();
            _reloadAfterLastChangeCts?.Dispose();

            _reloadAfterLastChangeCts = new CancellationTokenSource();

            await Task.Delay(_reloadAfterLastChangeDelay, _reloadAfterLastChangeCts.Token);

            await RefreshConfiguration(null, CancellationToken.None);

            _reloadAfterLastChangeCts?.Dispose();
            _reloadAfterLastChangeCts = null;
        }
        catch (OperationCanceledException)
        {
            // Expected
        }
        catch (Exception ex)
        {
            _log.LogError(ex, "ERROR: Scheduled reload after last change failed.");
        }
    }


    private static (Dictionary<string, Type> entities, Dictionary<string, Type> ddTypes) GetControlPanelEntitiesTypes()
    {
        var assembly = typeof(Objects).Assembly;
        var types = assembly.GetEntitiesTypes();
        var dd_core = DataDictionarySchema.GetCoreEntitiesTypes();

        var entities = new Dictionary<string, Type>(StringComparer.OrdinalIgnoreCase);
        var ddTypes = new Dictionary<string, Type>(StringComparer.OrdinalIgnoreCase);

        foreach (var type in types)
        {
            entities.TryAdd($"{ConfigurationDefaults.ControlPanelAppID}.{type.Key}", type.Value);
            if (dd_core.ContainsKey(type.Key))
            {
                ddTypes.TryAdd(type.Key, type.Value);
            }
        }

        return (entities, ddTypes);
    }

    private Dictionary<string, PublicEndpointSecurityRecord> GetPublicEndpointsInstances(string app_id, Assembly assembly)
    {
        var inits = assembly.GetInterfaceTypes<IPublicEndpoints>();
        var target = new Dictionary<string, PublicEndpointSecurityRecord>(StringComparer.OrdinalIgnoreCase);

        foreach (var init_type in inits)
        {
            if (Activator.CreateInstance(init_type) is not IPublicEndpoints pe) continue;
            var endpoints = pe.AddAllowedPublicEndpointRoutes();
            if (endpoints == null) continue;

            for (int i = 0; i < endpoints.Count; i++)
            {
                endpoints[i] = $"/{_options.MicroMAPIBaseRootPath}/{app_id}/public/{endpoints[i]}";
            }

            var rec = new PublicEndpointSecurityRecord(app_id);
            rec.AddAllowedRoutes(endpoints);
            target[app_id] = rec;
        }
        return target;
    }

    private async Task PersistEntitiesAssembliesTypes(string appId, string assemblyId, string assemblyPath, DatabaseClient client, Dictionary<string, Type> capturedTypes)
    {
        _backgroundTaskQueue.Enqueue($"PersistEntitiesAssembliesTypes.{appId}.{assemblyId}", async (innerCt) =>
        {
            using DatabaseClient dbc = (DatabaseClient)client.Clone();
            var assembly_types = new EntitiesAssembliesTypes(dbc);

            await dbc.Connect(innerCt);
            assembly_types.Def.c_assembly_id.Value = assemblyId;
            await assembly_types.ExecuteProcessDBStatus(assembly_types.Def.eat_deleteAllTypes, innerCt, throw_dbstat_exception: true);

            int count = 0;
            foreach (var type in capturedTypes)
            {
                innerCt.ThrowIfCancellationRequested();
                assembly_types.Def.c_assembly_id.Value = assemblyId;
                assembly_types.Def.vc_assemblytypename.Value = type.Value.Name;
                await assembly_types.InsertData(innerCt, true);
                count++;
            }

            return $"Processed {assemblyId} Types processed: {count} Assembly path: {assemblyPath}";
        }, true);
    }

    private async Task PersistAppEntityTypes(string appId, List<(string assemblyId, string? assemblyPath, Dictionary<string, Type> types)> assembliesForApp, HashSet<string> coreEntityNames)
    {
        var core_entity_names = coreEntityNames;
        _backgroundTaskQueue.Enqueue($"PersistAppEntityTypes.{appId}", async (innerCt) =>
        {
            int appEntitiesCount = 0;
            if (_ApplicationsCache.TryGetValue(appId, out var app_config))
            {
                using DatabaseClient app_ec = app_config.CreateDatabaseClient(_log, null, null);

                var entities = new CustomOrderedDictionary<DatabaseSchemaCreationOptions<EntityBase>>();

                foreach (var type in assembliesForApp
                    .SelectMany(x => x.types.Values)
                    .Where(t => typeof(EntityBase).IsAssignableFrom(t) && !t.IsAbstract)
                    .Where(t => !core_entity_names.Contains(t.Name))
                    .DistinctBy(t => t.Name, StringComparer.OrdinalIgnoreCase))
                {
                    innerCt.ThrowIfCancellationRequested();

                    if (Activator.CreateInstance(type) is not EntityBase ent) continue;

                    ent.Init(app_ec, schema_name: app_config.SchemaConfiguration.APPSchema);
                    entities.Add(type.Name, new DatabaseSchemaCreationOptions<EntityBase>(ent, create_or_alter: true));
                    appEntitiesCount++;
                }

                await MicromEntitiesTypes.FillEntitiesTypes(app_ec, app_config.SchemaConfiguration.DDSchema, entities, innerCt);
            }
            else
            {
                _log.LogWarning("APP config not found for {appId}. Skipping MicromEntitiesTypes sync.", appId);
            }

            return $"Processed APP {appId}. DevTools types: {appEntitiesCount}. Assemblies: {assembliesForApp.Count}";
        }, true);
    }

    // This method is called when the service is constructed so the API will have all types cached ahead
    private async Task<bool> LoadEntitiesAssemblies(CancellationToken ct)
    {
        bool ret = true;

        _ApplicationsCache.TryGetValue(ConfigurationDefaults.ControlPanelAppID, out var control_panel);

        var assemblies_folders = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        if (control_panel != null)
        {
            _EntityTypesCache.Clear();
            LoadControlPanelEntitiesTypes();

            using DatabaseClient client = control_panel.CreateDatabaseClient(_log, null, null);

            PreparedAssemblyGeneration? prepared = null;

            try
            {
                // Drop any unused assemblies first
                var eas = new EntitiesAssemblies(client, _encryptor);
                await eas.ExecuteProcessDBStatus(eas.Def.eas_dropUnusedAssemblies, ct, set_parms_from_columns: false, throw_dbstat_exception: true);

                var assemblies_to_copy = await GetAssembliesToCopy(client, ct);

                if (assemblies_to_copy.Count == 0)
                {
                    _log.LogError("WARNING: No applications defined in {server}, DB: {DB}.", control_panel.SQLServer, control_panel.SQLDB);
                    return false;
                }

                var requests = assemblies_to_copy
                    .Where(r => !r.source_assembly_path.Equals(GetCoreAssemblyPath(), StringComparison.OrdinalIgnoreCase))
                    .Select(r => new AssemblyCopyRequest(r.app_id, r.source_assembly_path, r.assembly_id))
                    .DistinctBy(r => $"{r.app_id}|{r.source_assembly_path}", StringComparer.OrdinalIgnoreCase)
                    .ToList();

                prepared = await _assemblyRuntime.PrepareGenerationAsync(requests, ct);

                Dictionary<string, PublicEndpointSecurityRecord> tmpPublic = new();

                var (tmpEntities, tmpDdTypes) = GetControlPanelEntitiesTypes();

                HashSet<string> processed = new(StringComparer.OrdinalIgnoreCase);

                var coreEntityNames = new HashSet<string>(DataDictionarySchema.GetCoreEntitiesTypes().Keys, StringComparer.OrdinalIgnoreCase);
                coreEntityNames.UnionWith(ConfigurationDatabaseSchema.GetCoreConfigurationEntitiesTypes().Keys);

                var appAssembliesToPersist = new Dictionary<string, List<(string assemblyId, string? assemblyPath, Dictionary<string, Type> types)>>(StringComparer.OrdinalIgnoreCase);

                foreach (var row in assemblies_to_copy)
                {
                    Dictionary<string, Type>? types;
                    Assembly? asmForEndpoints = null;
                    string? assembly_path = null;

                    if (row.source_assembly_path.Equals(GetCoreAssemblyPath(), StringComparison.OrdinalIgnoreCase))
                    {
                        types = DataDictionarySchema.GetCoreEntitiesTypes();
                        if (_options.CreateConfigEntitiesCodeGen)
                        {
                            var cfg = ConfigurationDatabaseSchema.GetCoreConfigurationEntitiesTypes();
                            foreach (var t in cfg) types.TryAdd(t.Key, t.Value);
                        }
                        assembly_path = row.source_assembly_path;
                    }
                    else
                    {
                        var key = $"{row.app_id}|{row.source_assembly_path}";
                        if (!prepared.assemblies_by_key.TryGetValue(key, out var loadedAsm))
                        {
                            _log.LogError("Assembly not loaded for key {key}", key);
                            continue;
                        }

                        asmForEndpoints = loadedAsm;
                        types = loadedAsm.GetEntitiesTypes();
                        assembly_path = loadedAsm.Location;
                    }

                    foreach (var type in types)
                    {
                        tmpEntities.TryAdd($"{row.app_id}.{type.Key}", type.Value);
                    }

                    if (asmForEndpoints != null)
                    {
                        var perAssemblyPublic = GetPublicEndpointsInstances(row.app_id, asmForEndpoints);
                        foreach (var kv in perAssemblyPublic)
                        {
                            tmpPublic[kv.Key] = kv.Value;
                        }
                    }

                    if (processed.Add(row.assembly_id))
                    {
                        var capturedTypes = types;
                        var assemblyId = row.assembly_id;
                        var assemblyPath = assembly_path;
                        var appId = row.app_id;

                        if (!appAssembliesToPersist.TryGetValue(row.app_id, out var appAssemblies))
                        {
                            appAssemblies = [];
                            appAssembliesToPersist[row.app_id] = appAssemblies;
                        }

                        appAssemblies.Add((row.assembly_id, assembly_path, capturedTypes));

                        await PersistEntitiesAssembliesTypes(appId, assemblyId, assemblyPath, client, capturedTypes);
                    }
                }

                foreach (var appBatch in appAssembliesToPersist)
                {
                    var appId = appBatch.Key;
                    var assembliesForApp = appBatch.Value;

                    await PersistAppEntityTypes(appId, assembliesForApp, coreEntityNames);
                }

                _EntityTypesCache.Clear();
                foreach (var kv in tmpEntities) _EntityTypesCache[kv.Key] = kv.Value;

                _DDTypesCache.Clear();
                foreach (var kv in tmpDdTypes) _DDTypesCache[kv.Key] = kv.Value;

                _PublicAccessCache.Clear();
                foreach (var kv in tmpPublic) _PublicAccessCache[kv.Key] = kv.Value;

                await _assemblyRuntime.CommitGenerationAsync(prepared.generation_id, ct);

                if (_options.EnableHotReloadForEntitiesAssemblies == true)
                {
                    _assemblyRuntime.EnableFileWatchers(prepared.generation_id, _ => ScheduleReloadAfterLastChange());
                }

                return true;
            }
            catch (Exception ex)
            {
                if (prepared != null)
                {
                    try
                    {
                        await _assemblyRuntime.RollbackGenerationAsync(prepared.generation_id, ct);
                    }
                    catch (Exception rollbackEx)
                    {
                        _log.LogError(rollbackEx, "ERROR: Rollback failed for generation {generation_id}", prepared.generation_id);
                    }
                }

                _log.LogError(ex, "ERROR: Fatal error trying to read configured applications from configuration DB. Server {server} DB {DB}", control_panel.SQLServer, control_panel.SQLDB);
                ret = false;
            }
        }

        return ret;
    }

    private void LoadControlPanelEntitiesTypes()
    {
        try
        {
            var assembly = typeof(Objects).Assembly;

            var types = assembly.GetEntitiesTypes();

            var dd_types = DataDictionarySchema.GetCoreEntitiesTypes();

            foreach (var type in types)
            {
                if (!_EntityTypesCache.TryAdd($"{ConfigurationDefaults.ControlPanelAppID}.{type.Key}", type.Value))
                {
                    _log.LogWarning("WARNING: APP: {app} - Type {type} from assembly {assembly} already exists in the cache. All types in the same application must have unique names even if in different assemblies.",
                        ConfigurationDefaults.ControlPanelAppID, type.Key, assembly.FullName);
                }
                if (dd_types.ContainsKey(type.Key))
                {
                    _DDTypesCache.TryAdd(type.Key, type.Value);
                }
            }

        }
        catch (Exception ex)
        {
            _log.LogError(ex, "ERROR: Fatal error trying to load control panes assemblies");
        }
    }

    public bool IsDDType(string type_name)
    {
        return _DDTypesCache.ContainsKey(type_name);
    }

    public Type? GetEntityType(string app_id, string entity_name)
    {
        _EntityTypesCache.TryGetValue($"{app_id}.{entity_name}", out var type);
        return type;
    }

    public List<Assembly> GetAllAPPAssemblies(string app_id)
    {
        return [.. _EntityTypesCache
            .Where(kvp => kvp.Key.StartsWith(app_id + ".", StringComparison.OrdinalIgnoreCase))
            .Select(kvp => kvp.Value.Assembly)
            .Distinct()];
    }

    private async Task<bool> LoadAppsConfiguration(CancellationToken ct)
    {
        bool ret = false;

        _ApplicationsCache.TryGetValue(ConfigurationDefaults.ControlPanelAppID, out var control_panel);

        if (control_panel != null && control_panel.SQLServer != "" && control_panel.SQLUser != "")
        {

            ret = true;
            using DatabaseClient client = control_panel.CreateDatabaseClient(_log, null, null);

            try
            {
                var apps = await Applications.GetAPPSConfiguration(client, ct, _encryptor);

                if (apps != null && apps.Count > 0)
                {
                    _ApplicationsCache.Clear();
                    _ApplicationsCache.TryAdd(control_panel.ApplicationID, control_panel);
                    foreach (var app in apps)
                    {
                        // MMC: create uploads folder for each app
                        string uploads_path = Path.Combine(_options.UploadsFolder!, app.ApplicationID);
                        Directory.CreateDirectory(uploads_path);

                        _ApplicationsCache.TryAdd(app.ApplicationID, app);
                    }
                }
                else
                {
                    _log.LogError("ERROR: There are no configured apps in server {server}, DB: {DB}", control_panel.SQLServer, control_panel.SQLDB);
                }
            }
            catch (Exception ex)
            {
                _log.LogError(ex, "ERROR: Trying to read configured applications from configuration DB. Server {server} DB {DB}", control_panel.SQLServer, control_panel.SQLDB);
            }
            finally
            {
                _bus.Publish(new MicroMConfigurationReloadedEvent());
            }
        }

        return ret;
    }

    private void ReloadCors()
    {
        _globalAllowedURLS.Clear();
        _log.LogInformation("Initializing CORS");
        var fromSettings = _config.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? Array.Empty<string>();
        foreach (var origin in fromSettings)
        {
            var n_origin = NormalizeURL(origin);
            if (n_origin != "")
            {
                _globalAllowedURLS.Add(n_origin);
                _log.LogInformation("Global CORS Allowed Origin: {origin}", n_origin);
            }
        }

        _appAllowedURLS.Clear();
        foreach (var app in _ApplicationsCache.Values)
        {
            var app_origins = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var url in app.FrontendURLS)
            {
                var n_url = NormalizeURL(url);
                if (n_url != "")
                {
                    app_origins.Add(n_url);
                    _log.LogInformation("App {app_id} CORS Allowed Origin: {origin}", app.ApplicationID, n_url);
                }
            }

            _appAllowedURLS.TryAdd(app.ApplicationID, app_origins);
        }
    }


    private static readonly SemaphoreSlim _refreshSemaphore = new(1, 1);

    public async Task<bool> RefreshConfiguration(string? app_id, CancellationToken ct)
    {
        await _refreshSemaphore.WaitAsync(ct);
        _log.LogTrace("RefreshConfiguration Called");

        try
        {
            // We disable file watchers here to prevent reloads while refreshing the configuration.
            // LoadEntitiesAssemblies will re-enable them if hot reload is enabled and assemblies are loaded successfully.
            _assemblyRuntime.DisableFileWatchers();

            await StopApplicationServices();
            _ApplicationsCache.Clear();

            await AddControlPanelApp(ct);

            bool result = await LoadAppsConfiguration(ct);

            await LoadEntitiesAssemblies(ct);

            await RunHotReloadDbUpdates(ct, app_id);

            ReloadCors();

            await StartApplicationServices();
            return result;
        }
        finally
        {
            _refreshSemaphore.Release();
        }
    }

    private static readonly SemaphoreSlim _reloadSemaphore = new(1, 1);
    private async Task ReloadConfiguration(CancellationToken ct)
    {
        await _reloadSemaphore.WaitAsync(ct);
        try
        {
            await RefreshConfiguration(null, ct);
        }
        finally
        {
            _reloadSemaphore.Release();
        }
    }

    public DatabaseClient? GetDatabaseClient(string app_id, int connection_timeout_secs = 5)
    {
        _ApplicationsCache.TryGetValue(app_id, out var app_config);
        if (app_config != null && app_config.SQLServer != "" && app_config.SQLDB != "")
        {
            return app_config.CreateDatabaseClient(_log, null, null, connection_timeout_secs);
        }

        return null;
    }

    public List<string> GetAppIDs() => [.. _ApplicationsCache.Keys];

    public PublicEndpointSecurityRecord? GetPublicAccessAllowedRoutes(string app_id)
    {
        return _PublicAccessCache.TryGetValue(app_id, out var record) ? record : null;
    }

    public bool IsCORSOriginAllowed(string? app_id, string origin)
    {
        var n_origin = NormalizeURL(origin);
        if (n_origin == "") return false;
        if (_globalAllowedURLS.Contains(n_origin)) return true;
        if (!string.IsNullOrEmpty(app_id) && _appAllowedURLS.TryGetValue(app_id, out var app_urls))
        {
            if (app_urls.Contains(n_origin)) return true;
        }
        return false;
    }

    public string? GetTenantPath(HttpContext context)
    {
        if (_basePathString == PathString.Empty)
        {
            _log.LogWarning("MicroMAPIBaseRootPath is not configured.");
            return null;
        }

        var fullPath = context.Request.PathBase.Add(context.Request.Path);

        if (fullPath.StartsWithSegments(_basePathString, StringComparison.OrdinalIgnoreCase, out var remainingPath))
        {
            if (string.IsNullOrEmpty(remainingPath.Value))
            {
                _log.LogWarning("No APP_ID found in path {path}", fullPath);
                return null;
            }

            // remainingPath starts with "/APP_ID/..." or "/APP_ID"
            var remaining = remainingPath.Value.TrimStart('/');

            // Get APP_ID, from first segment in remainingPath
            var appIdEnd = remaining.IndexOf('/');
            string appId;

            if (appIdEnd == -1)
            {
                appId = remaining; // last segment
            }
            else
            {
                // Get app_id from first segment
                appId = remaining[..appIdEnd];
            }

            if (string.IsNullOrEmpty(appId))
            {
                _log.LogWarning("No APP_ID found in path {path}", fullPath);
                return null;
            }

            // Assemble tenant fullPath
            var tenantPath = $"{_basePathString.Value}/{appId}/";
            return tenantPath;
        }

        _log.LogWarning("The path {path} does not match the configured base path {basePath}", fullPath, _basePathString.Value);
        return null;
    }

    private async Task StartApplicationServices(ApplicationOption app)
    {
        if (!_ApplicationServicesCache.TryGetValue(app.ApplicationID, out var services))
        {
            services = await app.CreateApplicationServices(_backgroundTaskQueue, this, _log);
            if (!_ApplicationServicesCache.TryAdd(app.ApplicationID, services))
            {
                _log.LogError("Failed to cache services for app {app_id}", app.ApplicationID);
            }
        }

        foreach (var service in services)
        {
            try
            {
                await service.InitiateStartupServices(_backgroundTaskQueue, this, _log);
            }
            catch (Exception ex)
            {
                _log.LogError(ex, "Failed to start service {service} for application {app_id}", service.GetType().FullName, app.ApplicationID);
            }
        }

    }

    public async Task StartApplicationServices()
    {
        foreach (var app in _ApplicationsCache.Values)
        {
            await StartApplicationServices(app);
        }
    }

    private async Task StopApplicationServices(string app_id)
    {
        if (!_ApplicationServicesCache.TryGetValue(app_id, out var services)) return;

        foreach (var service in services)
        {
            try
            {
                await service.StopStartupServices(_backgroundTaskQueue, this, _log);
            }
            catch (Exception ex)
            {
                _log.LogError(ex, "Failed to stop service {service} for application {app_id}", service.GetType().FullName, app_id);
            }

            try
            {
                if (service is IAsyncDisposable asyncDisposable)
                {
                    await asyncDisposable.DisposeAsync();
                }
                else if (service is IDisposable disposable)
                {
                    disposable.Dispose();
                }
            }
            catch (Exception ex)
            {
                _log.LogWarning(ex, "Failed to dispose service {service} for application {app_id}", service.GetType().FullName, app_id);
            }
        }

    }

    public async Task StopApplicationServices()
    {
        foreach (var app_services in _ApplicationsCache.Values)
        {
            await StopApplicationServices(app_services.ApplicationID);
        }

        _ApplicationServicesCache.Clear();
    }


    private sealed record AssemblyDbRow(string AppId, string AssemblyPath, string AssemblyId);

    private async Task<List<AssemblyCopyRequest>> GetAssembliesToCopy(DatabaseClient client, CancellationToken ct)
    {
        var assemblies = new ApplicationsAssemblies(client, _encryptor);
        var data = await assemblies.ExecuteProc(assemblies.Def.apa_GetAssemblies, ct);

        List<AssemblyCopyRequest> rows = [];
        if (!data.HasData()) return rows;

        foreach (var res in data[0].records)
        {
            rows.Add(new AssemblyCopyRequest(app_id: (string)res[0]!, source_assembly_path: Path.GetFullPath((string)res[1]!), assembly_id: (string)res[2]!));
        }

        return rows;
    }

    private async Task RunHotReloadDbUpdates(CancellationToken ct, string? app_id = null)
    {
        var targets = _ApplicationsCache.Values
            .Where(a => !a.ApplicationID.Equals(ConfigurationDefaults.ControlPanelAppID, StringComparison.OrdinalIgnoreCase))
            .Where(a => a.EnableUpdateOnHotReload);

        if (!string.IsNullOrWhiteSpace(app_id))
        {
            targets = targets.Where(a => a.ApplicationID.Equals(app_id, StringComparison.OrdinalIgnoreCase));
        }

        foreach (var app in targets)
        {
            var result = await ApplicationDatabase.UpdateAppDatabaseOnHotReload(app, this, _log, ct);
            if (!result.Failed)
            {
                _bus.Publish(new AppDatabaseUpdatedOnHotReloadEvent(app.ApplicationID));
            }
        }
    }
}
