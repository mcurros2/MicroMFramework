using MicroM.Configuration;
using MicroM.Data;
using MicroM.DataDictionary;
using MicroM.DataDictionary.CategoriesDefinitions;
using MicroM.Extensions;
using MicroM.Web.Authentication;
using MicroM.Web.Services;
using System.Reflection;
using static MicroM.Database.DatabaseManagement;
using static MicroM.Validators.Expressions;

namespace MicroM.Database
{
    /// <summary>
    /// Provides helpers to create and manage application databases.
    /// </summary>
    public class ApplicationDatabase
    {
        private static async Task InitializeDatabase(IEntityClient admin_dbc, Applications app, string grant_user, CancellationToken ct)
        {
            // MMC: Clone here is used to create a connection to the same server with the app database name
            using var app_ec = admin_dbc.Clone(new_db: app.Def.vc_database.Value);

            try
            {
                await app_ec.Connect(ct);

                List<string> assemblies = [app.Def.vc_assembly1.Value, app.Def.vc_assembly2.Value, app.Def.vc_assembly3.Value, app.Def.vc_assembly4.Value, app.Def.vc_assembly5.Value];

                foreach (var assembly in assemblies)
                {
                    if (string.IsNullOrEmpty(assembly)) continue;
                    var inits = Assembly.LoadFrom(assembly).GetInterfaceTypes<IDatabaseSchema>();

                    foreach (var init_type in inits)
                    {
                        if (init_type != null)
                        {
                            var result = Activator.CreateInstance(init_type);
                            if (result != null)
                            {
                                IDatabaseSchema instance = (IDatabaseSchema)result;
                                var migration_result = await instance.MigrateDatabase(app_ec, ct);

                                var entities = await instance.GetEntitiesTypes(app_ec, ct);
                                try
                                {
                                    if (entities == null || entities.Count == 0)
                                    {
                                        throw new InvalidOperationException($"No entities found in assembly {assembly}");
                                    }

                                    if (migration_result == DatabaseMigrationResult.NoMigrationNeeded)
                                    {
                                        await instance.CreateDBSchemaAndProcs(app_ec, entities, ct);
                                    }

                                    await instance.GrantPermissions(app_ec, entities, grant_user, ct);

                                    await app_ec.ExecuteSQLNonQuery("delete microm_menus_items_allowed_routes; delete microm_routes;", ct);

                                    await instance.CreateMenus(app_ec, entities, ct);

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
        }


        /// <summary>
        /// Retrieves and updates the status of the application's database.
        /// </summary>
        /// <param name="app">The application instance.</param>
        /// <param name="ct">Cancellation token.</param>
        /// <param name="options">Optional MicroM options.</param>
        /// <param name="server_claims">Server credentials.</param>
        /// <param name="api">Optional API services.</param>
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
                // Verify we have admin rights
                app.Def.b_adminuserhasrights.Value = await LoggedInUserHasAdminRights(admin_dbc, ct);
                if (app.Def.b_adminuserhasrights.Value)
                {
                    app.Def.b_appdbexists.Value = await DatabaseExists(admin_dbc, app.Def.vc_database.Value, ct);
                    app.Def.b_appuserexists.Value = await UserExists(admin_dbc, app.Def.vc_user.Value, ct);
                }
            }
        }

        /// <summary>
        /// Drops the application's database and associated login.
        /// </summary>
        /// <param name="app">The application definition.</param>
        /// <param name="ct">Cancellation token.</param>
        /// <param name="options">Optional MicroM options.</param>
        /// <param name="server_claims">Server credentials.</param>
        /// <param name="api">Optional API services.</param>
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
        /// Creates a new database for the application using its configuration.
        /// </summary>
        /// <param name="app">Application definition.</param>
        /// <param name="drop_and_recreate">If true, an existing database is dropped and recreated.</param>
        /// <param name="ct">Cancellation token.</param>
        /// <param name="options">Optional MicroM options.</param>
        /// <param name="server_claims">Server credentials.</param>
        /// <param name="api">Optional API services.</param>
        /// <returns>Status of the database creation.</returns>
        public static async Task<DBStatusResult> CreateAppDatabase(Applications app, bool drop_and_recreate, CancellationToken ct, MicroMOptions? options = null, Dictionary<string, object>? server_claims = null, IWebAPIServices? api = null)
        {

            ArgumentNullException.ThrowIfNull(server_claims);
            // MMC: this is the logged in user to the control panel, it should be admin
            server_claims.TryGetValue(MicroMServerClaimTypes.MicroMUsername, out var admin_user_obj);
            server_claims.TryGetValue(MicroMServerClaimTypes.MicroMPassword, out var admin_password_obj);

            string? admin_user = (string?)admin_user_obj;
            string? admin_password = (string?)admin_password_obj;

            if (string.IsNullOrEmpty(admin_user)) throw new ArgumentNullException(nameof(server_claims));


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
                return new() { Failed = true, Results = errors };
            }


            using var admin_dbc = app.Client.Clone(app.Def.vc_server.Value, app.Client.MasterDatabase, admin_user, admin_password ?? "");

            if (await admin_dbc.Connect(ct, false) == false)
            {
                return new() { Failed = true, Results = [new() { Status = DBStatusCodes.Error, Message = $"Can't connect to the APP Server {app.Def.vc_server.Value}." }] };
            }

            try
            {
                // MMC: app.Client should be the database configuration user (not sa)
                // re read record.
                var result = await app.GetData(ct, options, server_claims, api);

                if (!result)
                {
                    errors.Add(new() { Status = DBStatusCodes.Error, Message = $"Application {app.Def.c_application_id.Value.Trim()} not found" });
                    return new() { Failed = true, Results = errors };
                }

                // No admin rigths
                if (app.Def.b_adminuserhasrights.Value == false)
                {
                    return new() { Failed = true, Results = [new() { Status = DBStatusCodes.Error, Message = "The logged in user has no admin user rights" }] };
                }

                // DB Exists but drop and recreate is not specified
                if (app.Def.b_appdbexists.Value && drop_and_recreate == false)
                {
                    return new() { Failed = true, Results = [new() { Status = DBStatusCodes.Error, Message = $"The APP Database {app.Def.vc_database.Value} already exists." }] };
                }

                // APP user exists, but the APP DB don't, this can lead to a configuration password mismatch
                if (app.Def.b_appuserexists.Value && app.Def.b_appdbexists.Value == false && drop_and_recreate == false)
                {
                    return new() { Failed = true, Results = [new() { Status = DBStatusCodes.Error, Message = $"The APP Database user {app.Def.vc_user.Value} exists, but the config database don't. Please delete the existing login to reconfigure the database." }] };
                }

                // Create the APP DB
                await CreateDatabase(admin_dbc, app.Def.vc_database.Value, options?.DefaultSQLDatabaseCollation, ct);
                if (!app.Def.b_appuserexists.Value)
                {
                    await DropLogin(admin_dbc, app.Def.vc_user.Value, ct);
                }
                await CreateLoginAndDatabaseUser(admin_dbc, app.Def.vc_database.Value, app.Def.vc_user.Value, app.Def.vc_password.Value ?? "", ct);

                // Create tables and procs
                await InitializeDatabase(admin_dbc, app, app.Def.vc_user.Value, ct);

                // Create a MicroM Admin User
                if (app.Def.c_authenticationtype_id.Value.Equals(nameof(AuthenticationTypes.MicroMAuthentication), StringComparison.OrdinalIgnoreCase)
                    && !string.IsNullOrEmpty(app.Def.vc_app_admin_user.Value)
                    && !string.IsNullOrEmpty(app.Def.vc_app_admin_password.Value)
                    )
                {
                    using var app_dbc = admin_dbc.Clone(new_db: app.Def.vc_database.Value);
                    var usr = new MicromUsers(app_dbc);
                    usr.Def.vc_username.Value = app.Def.vc_app_admin_user.Value;
                    usr.Def.vc_password.Value = app.Def.vc_app_admin_password.Value;
                    usr.Def.c_usertype_id.Value = nameof(UserTypes.ADMIN);
                    await usr.InsertData(ct);
                }
            }
            finally
            {
                await app.Client.Disconnect();
            }

            return new() { Results = [new() { Status = DBStatusCodes.OK }] };
        }

        /// <summary>
        /// Updates the application's database schema and data.
        /// </summary>
        /// <param name="app">The application instance.</param>
        /// <param name="ct">Cancellation token.</param>
        /// <param name="server_claims">Server credentials.</param>
        /// <param name="options">Optional MicroM options.</param>
        /// <param name="api">Optional API services.</param>
        /// <returns>Status of the update operation.</returns>
        public static async Task<DBStatusResult> UpdateAppDatabase(Applications app, CancellationToken ct, Dictionary<string, object>? server_claims = null, MicroMOptions? options = null, IWebAPIServices? api = null)
        {
            ArgumentNullException.ThrowIfNull(server_claims);
            // MMC: this is the logged in user to the control panel, it should be admin
            server_claims.TryGetValue(MicroMServerClaimTypes.MicroMUsername, out var admin_user_obj);
            server_claims.TryGetValue(MicroMServerClaimTypes.MicroMPassword, out var admin_password_obj);

            string? admin_user = (string?)admin_user_obj;
            string? admin_password = (string?)admin_password_obj;

            if (string.IsNullOrEmpty(admin_user)) throw new ArgumentNullException(nameof(server_claims));

            using var admin_dbc = app.Client.Clone(app.Def.vc_server.Value, app.Client.MasterDatabase, admin_user, admin_password ?? "");

            if (await admin_dbc.Connect(ct, false) == false)
            {
                return new() { Failed = true, Results = [new() { Status = DBStatusCodes.Error, Message = $"Can't connect to the APP Server {app.Def.vc_server.Value}." }] };
            }

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

            // Create tables and procs
            await InitializeDatabase(admin_dbc, app, app.Def.vc_user.Value, ct);

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
                    using var app_dbc = admin_dbc.Clone(new_db: app.Def.vc_database.Value);
                    var usr = new MicromUsers(app_dbc);
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

            return new() { Results = [new() { Status = DBStatusCodes.OK }] };
        }

    }
}
