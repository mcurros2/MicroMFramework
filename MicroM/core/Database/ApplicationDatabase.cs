using MicroM.Configuration;
using MicroM.Configuration.CategoriesDefinitions;
using MicroM.Configuration.Entities;
using MicroM.Data;
using MicroM.DataDictionary.CategoriesDefinitions;
using MicroM.DataDictionary.Entities;
using MicroM.Extensions;
using MicroM.Web.Authentication;
using MicroM.Web.Services;
using Microsoft.Extensions.Logging;
using static MicroM.Database.DatabaseManagement;
using static MicroM.Validators.Expressions;

namespace MicroM.Database;

public class ApplicationDatabase
{
    private static string? GetConfigSqlUser(IWebAPIServices api)
    {
        var controlPanelCfg = api.app_config.GetAppConfiguration(ConfigurationDefaults.ControlPanelAppID);
        return controlPanelCfg?.SQLUser;
    }

    private static async Task<DBStatusResult?> EnsureConfigUserDbOwnerOnAppDb(
        IEntityClient admin_dbc,
        string appDbName,
        IWebAPIServices api,
        CancellationToken ct)
    {
        var configSqlUser = GetConfigSqlUser(api);
        if (string.IsNullOrWhiteSpace(configSqlUser))
        {
            return DBStatusResult.FailedStatus("Configuration SQL user was not found in control panel configuration.");
        }

        await EnsureDbOwnerMembership(admin_dbc, appDbName, configSqlUser, ct);
        return null;
    }

    private static async Task<List<IDatabaseSchema>> InitializeApplication(
        IEntityClient ec, ApplicationOption app_config, string grant_user, bool seed_test_data, IMicroMAppConfiguration config,
        ILogger log,
        CancellationToken ct)
    {
        using var timing = new InitializationTiming(log, "InitializeApplication", app_config.ApplicationID);
        List<IDatabaseSchema> seedInitializers = [];
        // MMC: Clone here is used to create a connection to the same server with the app database name
        using var app_ec = ec.Clone(new_db: app_config.SQLDB);

        try
        {
            await app_ec.Connect(ct);

            var assemblies = config.GetAllAPPAssemblies(app_config.ApplicationID);

            foreach (var assembly in assemblies)
            {
                var inits = assembly.GetInterfaceTypes<IDatabaseSchema>();

                foreach (var init_type in inits)
                {
                    if (init_type != null)
                    {
                        var result = Activator.CreateInstance(init_type);
                        if (result != null)
                        {
                            IDatabaseSchema instance = (IDatabaseSchema)result;

                            DatabaseMigrationResult migration_result;
                            using (new InitializationTiming(log, $"{init_type.FullName}.MigrateDatabase", app_config.ApplicationID))
                                migration_result = await instance.MigrateDatabase(app_ec, app_config.SchemaConfiguration, ct);

                            var entities = await instance.GetEntitiesInstances(app_ec, app_config.SchemaConfiguration, ct);
                            try
                            {
                                if (entities == null || entities.Count == 0)
                                {
                                    throw new InvalidOperationException($"No entities found in assembly {assembly}");
                                }

                                if (migration_result == DatabaseMigrationResult.NoMigrationNeeded)
                                {
                                    using (new InitializationTiming(log, $"{init_type.FullName}.CreateDBSchemaAndProcs", app_config.ApplicationID))
                                        await instance.CreateDBSchemaAndProcs(app_ec, entities, app_config.SchemaConfiguration, ct);

                                    if (app_config.EnableSeedTestData && seed_test_data)
                                    {
                                        seedInitializers.Add(instance);
                                    }
                                }

                                using (new InitializationTiming(log, $"{init_type.FullName}.GrantPermissions", app_config.ApplicationID))
                                    await instance.GrantPermissions(app_ec, entities, grant_user, app_config.SchemaConfiguration, ct);

                                var del_result = await MicromRoutes.DeleteAllRoutes(app_ec, app_config.SchemaConfiguration.DDSchema, ct);

                                using (new InitializationTiming(log, $"{init_type.FullName}.CreateMenus", app_config.ApplicationID))
                                    await instance.CreateMenus(app_ec, entities, app_config.SchemaConfiguration, ct);

                                entities.Clear();
                            }
                            finally
                            {
                                if (entities?.Count > 0) entities.Clear();
                            }
                        }
                    }
                }
            }
        }
        finally
        {
            await app_ec.Disconnect();
        }
        return seedInitializers;
    }

    private static async Task<List<IDatabaseSchema>> InitializeDatabase(
        IEntityClient admin_dbc, Applications app, ApplicationOption app_config, string grant_user, IWebAPIServices api,
        bool seed_test_data, ILogger log, CancellationToken ct)
    {
        // MMC: Clone here is used to create a connection to the same server with the app database name
        using var app_ec = admin_dbc.Clone(new_db: app.Def.vc_database.Value);

        try
        {
            await app_ec.Connect(ct);

            var assemblies = api.app_config.GetAllAPPAssemblies(app.Def.c_application_id.Value);
            bool hasAssemblies = new string?[] { app.Def.vc_assembly1.Value, app.Def.vc_assembly2.Value, app.Def.vc_assembly3.Value, app.Def.vc_assembly4.Value, app.Def.vc_assembly5.Value }.Any(s => !string.IsNullOrEmpty(s));
            if (assemblies.Count == 0 && hasAssemblies)
            {
                throw new InvalidOperationException($"No runtime assemblies loaded for app '{app.Def.c_application_id.Value}' and there are assemblies defined.");
            }

            return await InitializeApplication(app_ec, app_config, grant_user, seed_test_data, api.app_config, log, ct);
        }
        finally
        {
            await app_ec.Disconnect();
        }
    }

    public static async Task GetAppDatabaseStatus(Applications app, CancellationToken ct, MicroMOptions? options = null, Dictionary<string, object>? server_claims = null, IWebAPIServices? api = null)
    {
        ArgumentNullException.ThrowIfNull(server_claims);
        // MMC: this is the logged in user to the control panel
        server_claims.TryGetValue(MicroMServerClaimTypes.MicroMUsername, out var admin_user_obj);
        server_claims.TryGetValue(MicroMServerClaimTypes.MicroMPassword, out var admin_password_obj);

        string? admin_user = (string?)admin_user_obj;
        string? admin_password = (string?)admin_password_obj;


        if (string.IsNullOrEmpty(admin_user)) throw new ArgumentNullException(nameof(server_claims));


        using IEntityClient admin_dbc = app.Client.Clone(app.Def.vc_server.Value, app.Client.MasterDatabase, new_user: admin_user, new_password: admin_password ?? "");

        // Check if the server is up and we can connect
        app.Def.b_serverup.Value = await admin_dbc.Connect(ct);
        if (app.Def.b_serverup.Value)
        {
            app.Def.b_appdbexists.Value = await DatabaseExists(admin_dbc, app.Def.vc_database.Value, ct);

            // Verify we have admin rights
            app.Def.b_adminuserhasrights.Value = await LoggedInUserHasAdminRights(admin_dbc, ct);
            if (app.Def.b_adminuserhasrights.Value)
            {
                app.Def.b_appuserexists.Value = await UserExists(admin_dbc, app.Def.vc_user.Value, ct);
            }
        }
    }

    public static async Task DropAppDatabase(Applications app, CancellationToken ct, MicroMOptions? options = null, Dictionary<string, object>? server_claims = null, IWebAPIServices? api = null)
    {
        ArgumentNullException.ThrowIfNull(server_claims);
        // MMC: this is the logged in user to the control panel, it should be admin
        server_claims.TryGetValue(MicroMServerClaimTypes.MicroMUsername, out var admin_user_obj);
        server_claims.TryGetValue(MicroMServerClaimTypes.MicroMPassword, out var admin_password_obj);

        string? admin_user = (string?)admin_user_obj;
        string? admin_password = (string?)admin_password_obj;

        if (string.IsNullOrEmpty(admin_user)) throw new ArgumentNullException(nameof(server_claims));

        using IEntityClient admin_dbc = app.Client.Clone(app.Def.vc_server.Value, app.Client.MasterDatabase, admin_user, admin_password ?? "");
        await DropDatabase(admin_dbc, app.Def.vc_database.Value, ct);
        await DropLogin(admin_dbc, app.Def.vc_user.Value, ct);
    }

    /// <summary>
    /// Creates a new database for the application. It will use the app existing connection
    /// </summary>
    public static async Task<DBStatusResult> CreateAppDatabase(Applications app, bool drop_and_recreate, ApplicationOption app_config, CancellationToken ct, MicroMOptions? options = null, Dictionary<string, object>? server_claims = null, IWebAPIServices? api = null)
    {

        ArgumentNullException.ThrowIfNull(server_claims);
        // MMC: this is the logged in user to the control panel, it should be admin
        server_claims.TryGetValue(MicroMServerClaimTypes.MicroMUsername, out var admin_user_obj);
        server_claims.TryGetValue(MicroMServerClaimTypes.MicroMPassword, out var admin_password_obj);

        string? admin_user = (string?)admin_user_obj;
        string? admin_password = (string?)admin_password_obj;

        ArgumentException.ThrowIfNullOrEmpty(admin_user, nameof(admin_user));
        ArgumentNullException.ThrowIfNull(api, nameof(api));

        List<DBStatus> errors = [];

        if ("".AreAllEqual([app.Def.vc_assembly1.Value, app.Def.vc_assembly2.Value, app.Def.vc_assembly3.Value, app.Def.vc_assembly4.Value, app.Def.vc_assembly5.Value], ignore_null: true))
        {
            errors.Add(new() { Status = DBStatusCodes.Error, Message = "There are no assemblies configured for this application. Please configure at least one Entities assembly" });
        }

        if (!OnlyDigitNumbersAndUnderscore().IsMatch(app.Def.vc_database.Value))
        {
            errors.Add(new() { Status = DBStatusCodes.Error, Message = $"Database Name {app.Def.vc_database.Value} is invalid" });
        }

        if (!ValidSQLServerLogin().IsMatch(app.Def.vc_user.Value))
        {
            errors.Add(new() { Status = DBStatusCodes.Error, Message = $"SQL Username {app.Def.vc_user.Value} is invalid" });
        }

        if (errors.Count > 0)
        {
            return DBStatusResult.FailedStatus(errors);
        }

        using var admin_dbc = app.Client.Clone(app.Def.vc_server.Value, app.Client.MasterDatabase, admin_user, admin_password ?? "");

        if (await admin_dbc.Connect(ct, false) == false)
        {
            return DBStatusResult.FailedStatus("Can't connect to the APP Server " + app.Def.vc_server.Value + ".");
        }

        bool databaseInitializationExecuted = false;
        List<IDatabaseSchema> seedInitializers = [];
        try
        {
            // MMC: app.Client should be the database configuration user (not sa)
            // re read record.
            var result = await app.GetData(ct, options, server_claims, api);

            if (!result)
            {
                errors.Add(new() { Status = DBStatusCodes.Error, Message = $"Application {app.Def.c_application_id.Value.Trim()} not found" });
                return DBStatusResult.FailedStatus(errors);
            }

            // No admin rigths
            if (app.Def.b_adminuserhasrights.Value == false)
            {
                return DBStatusResult.FailedStatus("The logged in user has no admin user rights");
            }

            // DB Exists but drop and recreate is not specified
            if (app.Def.b_appdbexists.Value && drop_and_recreate == false)
            {
                return DBStatusResult.FailedStatus($"The APP Database {app.Def.vc_database.Value} already exists.");
            }

            // APP user exists, but the APP DB don't, this can lead to a configuration password mismatch
            if (app.Def.b_appuserexists.Value && app.Def.b_appdbexists.Value == false && drop_and_recreate == false)
            {
                return DBStatusResult.FailedStatus($"The APP Database user {app.Def.vc_user.Value} exists, but the config database don't. Please delete the existing login to reconfigure the database.");
            }

            // Create the APP DB
            await CreateDatabase(admin_dbc, app.Def.vc_database.Value, options?.DefaultSQLDatabaseCollation, ct);
            if (!app.Def.b_appuserexists.Value)
            {
                await DropLogin(admin_dbc, app.Def.vc_user.Value, ct);
            }
            await CreateLoginAndDatabaseUser(admin_dbc, app.Def.vc_database.Value, app.Def.vc_user.Value, app.Def.vc_password.Value ?? "", ct);

            var configGrantResult = await EnsureConfigUserDbOwnerOnAppDb(admin_dbc, app.Def.vc_database.Value, api, ct);
            if (configGrantResult != null) return configGrantResult;

            // Create tables and procs
            databaseInitializationExecuted = true;
            seedInitializers = await InitializeDatabase(admin_dbc, app, app_config, app.Def.vc_user.Value, api, seed_test_data: app_config.EnableSeedTestData, api.log, ct);

            // Create a MicroM Admin User
            if (app.Def.c_authenticationtype_id.Value.Equals(nameof(AuthenticationTypes.MicroMAuthentication), StringComparison.OrdinalIgnoreCase)
                && !string.IsNullOrEmpty(app.Def.vc_app_admin_user.Value)
                && !string.IsNullOrEmpty(app.Def.vc_app_admin_password.Value)
                )
            {
                var schema_config = app_config.SchemaConfiguration;

                using var app_dbc = admin_dbc.Clone(new_db: app.Def.vc_database.Value);
                var usr = new MicromUsers(app_dbc, schema_name: schema_config.DDSchema);
                usr.Def.vc_username.Value = app.Def.vc_app_admin_user.Value;
                usr.Def.vc_password.Value = app.Def.vc_app_admin_password.Value;
                usr.Def.c_usertype_id.Value = nameof(UserTypes.ADMIN);
                await usr.InsertData(ct);
            }
        }
        finally
        {
            try { await app.Client.Disconnect(); }
            finally
            {
                if (databaseInitializationExecuted)
                {
                    System.Runtime.GCSettings.LargeObjectHeapCompactionMode = System.Runtime.GCLargeObjectHeapCompactionMode.CompactOnce;
                    GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced, blocking: true, compacting: true);
                }
            }
        }

        api.app_config.QueuePersistAppEntityTypes(app_config.ApplicationID);
        api.app_config.QueueSeedTestData(app_config.ApplicationID, seedInitializers);
        return new() { Results = [new() { Status = DBStatusCodes.OK }] };
    }

    public static async Task<DBStatusResult> UpdateAppDatabase(
        Applications app, ApplicationOption app_config, CancellationToken ct, Dictionary<string, object>? server_claims = null,
        MicroMOptions? options = null, IWebAPIServices? api = null)
    {
        ArgumentNullException.ThrowIfNull(server_claims);
        // MMC: this is the logged in user to the control panel, it should be admin
        server_claims.TryGetValue(MicroMServerClaimTypes.MicroMUsername, out var admin_user_obj);
        server_claims.TryGetValue(MicroMServerClaimTypes.MicroMPassword, out var admin_password_obj);

        string? admin_user = (string?)admin_user_obj;
        string? admin_password = (string?)admin_password_obj;

        if (string.IsNullOrEmpty(admin_user)) throw new ArgumentNullException(nameof(server_claims));
        ArgumentNullException.ThrowIfNull(api, nameof(api));

        using var admin_dbc = app.Client.Clone(app.Def.vc_server.Value, app.Client.MasterDatabase, admin_user, admin_password ?? "");

        if (await admin_dbc.Connect(ct, false) == false)
        {
            return new() { Failed = true, Results = [new() { Status = DBStatusCodes.Error, Message = $"Can't connect to the APP Server {app.Def.vc_server.Value}." }] };
        }

        bool databaseInitializationExecuted = false;
        List<IDatabaseSchema> seedInitializers = [];
        try
        {
            try
            {
                if (!app.Def.b_appuserexists.Value)
                {
                    // re-read an unencrypt password
                    var result = await app.GetData(ct, options, server_claims, api);
                    await CreateLoginAndDatabaseUser(admin_dbc, app.Def.vc_database.Value, app.Def.vc_user.Value, app.Def.vc_password.Value ?? "", ct);
                }
            }
            catch
            {

            }

            var configGrantResult = await EnsureConfigUserDbOwnerOnAppDb(admin_dbc, app.Def.vc_database.Value, api, ct);
            if (configGrantResult != null) return configGrantResult;

            // Create tables and procs
            databaseInitializationExecuted = true;
            seedInitializers = await InitializeDatabase(admin_dbc, app, app_config, app.Def.vc_user.Value, api, seed_test_data: app_config.EnableSeedTestData, api.log, ct);

            // try to recreate user if for any reason has been deleted
            try
            {
                // re-read an unencrypt password
                var result = await app.GetData(ct, options, server_claims, api);

                // re-Create a MicroM Admin User, this can launch an exception as we are not checking if the user exists
                if (app.Def.c_authenticationtype_id.Value.Equals(nameof(AuthenticationTypes.MicroMAuthentication), StringComparison.OrdinalIgnoreCase)
                    && !string.IsNullOrEmpty(app.Def.vc_app_admin_user.Value)
                    && !string.IsNullOrEmpty(app.Def.vc_app_admin_password.Value)
                    )
                {
                    var schema_config = app_config.SchemaConfiguration;

                    using var app_dbc = admin_dbc.Clone(new_db: app.Def.vc_database.Value);
                    var usr = new MicromUsers(app_dbc, schema_name: schema_config.DDSchema);
                    usr.Def.vc_username.Value = app.Def.vc_app_admin_user.Value;
                    usr.Def.vc_password.Value = app.Def.vc_app_admin_password.Value;
                    usr.Def.c_usertype_id.Value = nameof(UserTypes.ADMIN);
                    await usr.InsertData(ct);
                }

            }
            catch
            {
                //ignore exceptions here
            }

            await admin_dbc.Disconnect();
        }
        finally
        {
            if (databaseInitializationExecuted)
            {
                System.Runtime.GCSettings.LargeObjectHeapCompactionMode = System.Runtime.GCLargeObjectHeapCompactionMode.CompactOnce;
                GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced, blocking: true, compacting: true);
            }
        }
        api.app_config.QueuePersistAppEntityTypes(app_config.ApplicationID);
        api.app_config.QueueSeedTestData(app_config.ApplicationID, seedInitializers);
        return new() { Results = [new() { Status = DBStatusCodes.OK }] };
    }

    public static Task<DBStatusResult> UpdateAppDatabaseOnHotReload(
        ApplicationOption app_config, IMicroMAppConfiguration config, ILogger log, MicroMOptions microm_config, CancellationToken ct)
        => UpdateAppDatabaseOnHotReload(app_config, config, log, microm_config, ct, skipSeedTestData: false);

    public static Task<DBStatusResult> UpdateAppDatabaseOnHotReload(
        ApplicationOption app_config, IMicroMAppConfiguration config, ILogger log, MicroMOptions microm_config, CancellationToken ct,
        bool skipSeedTestData)
        => UpdateAppDatabaseOnHotReload(app_config, config, log, microm_config, skipSeedTestData, skipGarbageCollection: false, ct: ct);

    public static async Task<DBStatusResult> UpdateAppDatabaseOnHotReload(
        ApplicationOption app_config, IMicroMAppConfiguration config, ILogger log, MicroMOptions microm_config,
        bool skipSeedTestData, bool skipGarbageCollection, CancellationToken ct)
    {
        var (result, initializers) = await InitializeAppDatabaseOnHotReload(app_config, config, log, skipSeedTestData, skipGarbageCollection, ct);
        if (!result.Failed)
        {
            config.QueuePersistAppEntityTypes(app_config.ApplicationID);
            config.QueueSeedTestData(app_config.ApplicationID, initializers);
        }
        return result;
    }

    internal static async Task<(DBStatusResult Result, List<IDatabaseSchema> Initializers)> InitializeAppDatabaseOnHotReload(
        ApplicationOption app_config, IMicroMAppConfiguration config, ILogger log,
        bool skipSeedTestData, bool skipGarbageCollection, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(app_config);

        var controlPanelCfg = config.GetAppConfiguration(ConfigurationDefaults.ControlPanelAppID);
        if (controlPanelCfg == null || string.IsNullOrWhiteSpace(controlPanelCfg.SQLUser) || string.IsNullOrWhiteSpace(controlPanelCfg.SQLPassword))
        {
            return (DBStatusResult.FailedStatus("Configuration SQL user credentials were not found."), []);
        }

        if (string.IsNullOrWhiteSpace(app_config.SQLServer) || string.IsNullOrWhiteSpace(app_config.SQLDB))
        {
            return (DBStatusResult.FailedStatus($"Invalid SQL configuration for app '{app_config.ApplicationID}'."), []);
        }

        bool databaseInitializationExecuted = false;
        List<IDatabaseSchema> seedInitializers = [];
        using var config_dbc = new DatabaseClient(app_config.SQLServer, app_config.SQLDB, controlPanelCfg.SQLUser, controlPanelCfg.SQLPassword, logger: log);

        if (await config_dbc.Connect(ct, throw_exception: false) == false)
        {
            return (DBStatusResult.FailedStatus($"Can't connect to APP DB '{app_config.SQLDB}' in server '{app_config.SQLServer}' using configuration SQL user."), []);
        }

        try
        {
            if (!await DatabaseExists(config_dbc, app_config.SQLDB, ct))
            {
                return (DBStatusResult.FailedStatus($"APP Database '{app_config.SQLDB}' does not exist."), []);
            }

            databaseInitializationExecuted = true;
            seedInitializers = await InitializeApplication(config_dbc, app_config, app_config.SQLUser, app_config.EnableSeedTestData && !skipSeedTestData, config, log, ct);
        }
        catch (Exception ex)
        {
            return (DBStatusResult.FailedStatus($"Hot reload update failed for app '{app_config.ApplicationID}'. {ex.Message}"), []);
        }
        finally
        {
            try { await config_dbc.Disconnect(); }
            finally
            {
                if (databaseInitializationExecuted && !skipGarbageCollection)
                {
                    System.Runtime.GCSettings.LargeObjectHeapCompactionMode = System.Runtime.GCLargeObjectHeapCompactionMode.CompactOnce;
                    GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced, blocking: true, compacting: true);
                }
            }
        }
        return (DBStatusResult.SuccessStatus(), seedInitializers);
    }
}
