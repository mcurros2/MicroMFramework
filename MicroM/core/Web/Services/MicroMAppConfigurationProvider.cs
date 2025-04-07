using MicroM.Configuration;
using MicroM.Core;
using MicroM.Data;
using MicroM.DataDictionary;
using MicroM.DataDictionary.CategoriesDefinitions;
using MicroM.Extensions;
using MicroM.Web.Services.Security;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Reflection;
using System.Security.Cryptography.X509Certificates;
using static MicroM.Extensions.DataExtensions;
using static System.ArgumentNullException;

namespace MicroM.Web.Services
{
    public class MicroMAppConfigurationProvider : IHostedService, IMicroMAppConfiguration
    {
        private static readonly Dictionary<string, ApplicationOption> _ApplicationsCache = new(StringComparer.OrdinalIgnoreCase);
        private static readonly Dictionary<string, Type> _EntityTypesCache = new(StringComparer.OrdinalIgnoreCase);
        private static readonly Dictionary<string, PublicEndpointSecurityRecord> _PublicAccessCache = new(StringComparer.OrdinalIgnoreCase);


        private readonly MicroMOptions _options;
        private readonly ILogger<MicroMAppConfigurationProvider> _log;
        private readonly IMicroMEncryption _encryptor;
        private readonly IBackgroundTaskQueue _backgroundTaskQueue;
        private readonly string _jwtkey;

        public MicroMAppConfigurationProvider(IOptions<MicroMOptions> options, ILogger<MicroMAppConfigurationProvider> logger, IMicroMEncryption encryptor, IBackgroundTaskQueue queue)
        {
            ThrowIfNull(options);

            _log = logger;
            _options = options.Value;
            _encryptor = encryptor;
            _backgroundTaskQueue = queue;
            // MMC: the tokens for the control panel are encrypted with this key. Every time the service is restarted will yiel existing tokens invalid
            _jwtkey = CryptClass.GenerateRandomBase64String(32);
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            return ReloadConfiguration(cancellationToken);
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            // Cleanup or stop tasks.
            return Task.CompletedTask;
        }

        public ApplicationOption? GetAppConfiguration(string app_id, CancellationToken ct)
        {
            _ApplicationsCache.TryGetValue(app_id, out var app);
            return app;
        }


        private async Task<(SecretsOptions? secrets, string? thumbprint)> ReadConfigurationDBParms(CancellationToken ct)
        {

            // MMC: try to find the configured certificate first
            X509Certificate2? cert = _encryptor.GetCertificate();
            string? thumbprint = cert?.Thumbprint;

            // MMC: a certificate was found, try to get sql configuration user from secrets
            SecretsOptions? result = null;
            if (cert != null)
            {
                string config_path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, ConfigurationDefaults.SecretsFilename);
                if (Path.Exists(config_path))
                {
                    string encrypted = await File.ReadAllTextAsync(config_path, ct);
                    result = CryptClass.DecryptObject<SecretsOptions>(encrypted, cert);
                }

            }

            return (result, thumbprint);
        }

        private async Task AddControlPanelApp(CancellationToken? ct)
        {

            var (secrets, thumbprint) = await ReadConfigurationDBParms(ct ?? CancellationToken.None);
            if (thumbprint == null)
            {
                _log.LogError("ERROR: There is no certificate configured");
            }
            if (secrets == null)
            {
                _log.LogError("ERROR: The API is not configured. Secrets not found {secrets}", ConfigurationDefaults.SecretsFilename);
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


        private void LoadPublicEndpoints(string app_id, Assembly assembly)
        {
            var inits = assembly.GetInterfaceTypes<IPublicEndpoints>();

            foreach (var init_type in inits)
            {
                var result = Activator.CreateInstance(init_type);
                if (result is IPublicEndpoints pe)
                {
                    var endpoints = pe.AddAllowedPublicEndpointRoutes();
                    if (endpoints != null)
                    {
                        // add to the start of each endpoint $"/{options.value.MicroMAPIBaseRootPath}/{app_id}/"
                        for (int i = 0; i < endpoints.Count; i++)
                        {
                            endpoints[i] = $"/{_options.MicroMAPIBaseRootPath}/{app_id}/public/{endpoints[i]}";
                        }

                        var security_record = new PublicEndpointSecurityRecord(app_id);

                        security_record.AddAllowedRoutes(endpoints);

                        _PublicAccessCache.TryAdd(app_id, security_record);
                    }
                }
            }
        }


        // This method is called when the service is constructed so the API will have all types cached ahead
        private async Task<bool> LoadEntitiesAssemblies(string? update_app_id, CancellationToken ct)
        {
            bool ret = false;

            _ApplicationsCache.TryGetValue(ConfigurationDefaults.ControlPanelAppID, out var control_panel);

            if (control_panel != null)
            {
                _EntityTypesCache.Clear();
                LoadControlPanelAssemblies();

                ret = true;
                using DatabaseClient client = new
                    (
                    server: control_panel.SQLServer,
                    user: control_panel.SQLUser ?? "",
                    password: control_panel.SQLPassword ?? "",
                    db: control_panel.SQLDB ?? "",
                    integrated_security: control_panel.SQLPassword == null,
                    connection_timeout_secs: 15,
                    logger: _log
                    );

                try
                {
                    // Drop any unused assemblies first
                    var eas = new EntitiesAssemblies(client, _encryptor);
                    await eas.ExecuteProcessDBStatus(ct, eas.Def.eas_dropUnusedAssemblies, false, true);

                    var assemblies = new ApplicationsAssemblies(client, _encryptor);

                    var data = await assemblies.ExecuteProc(ct, assemblies.Def.apa_GetAssemblies);

                    if (data.HasData())
                    {

                        var result = data[0];
                        HashSet<string> processed = new(StringComparer.OrdinalIgnoreCase);

                        foreach (var res in result.records)
                        {
                            string app_id = (string)res[0]!;
                            string assembly_path = (string)res[1]!;
                            string assembly_id = (string)res[2]!;

                            Dictionary<string, Type>? types;
                            if (assembly_path.Equals(GetCoreAssemblyPath(), StringComparison.OrdinalIgnoreCase))
                            {
                                types = DatabaseSchema.GetCoreEntitiesTypes();
                            }
                            else
                            {
                                Assembly? assembly = Assembly.LoadFrom(assembly_path);
                                types = assembly.GetEntitiesTypes();
                                LoadPublicEndpoints(app_id, assembly);
                            }

                            foreach (var type in types)
                            {
                                _EntityTypesCache.TryAdd($"{app_id}.{type.Key}", type.Value);
                            }

                            // ensure that each assembly is processed once, as it may be referenced by other APPS
                            if (app_id == update_app_id && !processed.Contains(assembly_id))
                            {
                                processed.Add(assembly_id);

                                // Queue the task to update every type in the DB. This will eventually be run on a separate thread by the background queue
                                _backgroundTaskQueue.Enqueue($"PersistEntitiesAssembliesTypes.{app_id}.{assembly_id}", async (ct) =>
                                {
                                    if (string.IsNullOrEmpty(assembly_id)) throw new InvalidOperationException("No assembly_id received");

                                    using DatabaseClient dbc = new(client);
                                    var assembly_types = new EntitiesAssembliesTypes(dbc);

                                    await dbc.Connect(ct);

                                    assembly_types.Def.c_assembly_id.Value = assembly_id;
                                    await assembly_types.ExecuteProcessDBStatus(ct, assembly_types.Def.eat_deleteAllTypes, throw_dbstat_exception: true);

                                    int count = 0;
                                    foreach (var type in types)
                                    {
                                        ct.ThrowIfCancellationRequested();
                                        assembly_types.Def.c_assembly_id.Value = assembly_id;
                                        assembly_types.Def.vc_assemblytypename.Value = type.Value.Name;
                                        await assembly_types.InsertData(ct, true);
                                        count++;
                                    }

                                    return $"Processed {assembly_id} Types processed: {count} Assembly path: {assembly_path}";
                                }, true);
                            }
                        }

                    }
                    else
                    {
                        _log.LogError("WARNING: No applications defined in {server}, DB: {DB}. Assemblies won't be loaded and the API won't work.", control_panel.SQLServer, control_panel.SQLDB);
                    }
                }
                catch (Exception ex)
                {
                    _log.LogError(ex, "ERROR: Fatal error trying to read configured applications from configuration DB. Server {server} DB {DB}", control_panel.SQLServer, control_panel.SQLDB);
                }
            }

            return ret;
        }

        private void LoadControlPanelAssemblies()
        {
            try
            {
                var assemblies = AppDomain.CurrentDomain.GetAssemblies();
                foreach (var assembly in assemblies)
                {
                    var types = assembly.GetEntitiesTypes();
                    foreach (var type in types)
                    {
                        _EntityTypesCache.TryAdd($"{ConfigurationDefaults.ControlPanelAppID}.{type.Key}", type.Value);
                    }
                }
            }
            catch (Exception ex)
            {
                _log.LogError(ex, "ERROR: Fatal error trying to load control panes assemblies");
            }
        }

        public Type? GetEntityType(string app_id, string entity_name)
        {
            _EntityTypesCache.TryGetValue($"{app_id}.{entity_name}", out var type);
            return type;
        }

        public List<Assembly> GetAllAPPAssemblies(string app_id)
        {
            return _EntityTypesCache
                .Where(kvp => kvp.Key.StartsWith(app_id + ".", StringComparison.OrdinalIgnoreCase))
                .Select(kvp => kvp.Value.Assembly)
                .Distinct()
                .ToList();
        }

        private async Task<bool> LoadAppsConfiguration(CancellationToken ct)
        {
            bool ret = false;

            _ApplicationsCache.TryGetValue(ConfigurationDefaults.ControlPanelAppID, out var control_panel);

            if (control_panel != null && control_panel.SQLServer != "" && control_panel.SQLUser != "")
            {
                ret = true;
                using DatabaseClient client = new
                    (
                    server: control_panel.SQLServer,
                    user: control_panel.SQLUser ?? "",
                    password: control_panel.SQLPassword ?? "",
                    db: control_panel.SQLDB ?? "",
                    integrated_security: control_panel.SQLPassword == null,
                    connection_timeout_secs: 5,
                    logger: _log
                    );

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
                            string uploads_path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, _options.UploadsFolder ?? ConfigurationDefaults.UploadsFolder, app.ApplicationID);
                            if (!Path.Exists(uploads_path)) Directory.CreateDirectory(uploads_path);

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
            }

            return ret;
        }


        private static readonly SemaphoreSlim _refreshSemaphore = new(1, 1);
        public async Task<bool> RefreshConfiguration(string? app_id, CancellationToken ct)
        {
            bool result = false;

            var aquired = await _refreshSemaphore.WaitAsync(0, ct);
            if (aquired)
            {
                try
                {
                    await AddControlPanelApp(ct);

                    await LoadEntitiesAssemblies(app_id, ct);
                    result = await LoadAppsConfiguration(ct);
                    return result;
                }
                finally
                {
                    _refreshSemaphore.Release();
                }
            }

            return result;
        }

        private static readonly SemaphoreSlim _reloadSemaphore = new(1, 1);
        private async Task ReloadConfiguration(CancellationToken ct)
        {
            var aquired = await _reloadSemaphore.WaitAsync(0, ct);
            if (aquired)
            {
                try
                {
                    _ApplicationsCache.Clear();
                    await RefreshConfiguration(null, ct);
                }
                finally
                {
                    _reloadSemaphore.Release();
                }
            }
        }

        public DatabaseClient? GetDatabaseClient(string app_id, int? connection_timeour_secs = 15)
        {
            _ApplicationsCache.TryGetValue(app_id, out var app_config);
            if (app_config != null && app_config.SQLServer != "" && app_config.SQLDB != "")
            {
                return new DatabaseClient(
                    server: app_config.SQLServer,
                    user: app_config.SQLUser ?? "",
                    password: app_config.SQLPassword ?? "",
                    db: app_config.SQLDB ?? "",
                    integrated_security: app_config.SQLPassword == null,
                    connection_timeout_secs: 15,
                    logger: _log
                    );
            }

            return null;

        }

        public List<string> GetAppIDs()
        {
            return [.. _ApplicationsCache.Keys];
        }

        public PublicEndpointSecurityRecord? GetPublicAccessAllowedRoutes(string app_id)
        {
            return _PublicAccessCache.TryGetValue(app_id, out var record) ? record : null;
        }
    }
}
