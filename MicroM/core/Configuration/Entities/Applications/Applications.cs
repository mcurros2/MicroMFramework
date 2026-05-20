using MicroM.Core;
using MicroM.Data;
using MicroM.DataDictionary.CategoriesDefinitions;
using MicroM.Extensions;
using MicroM.Generators.ReactGenerator;
using MicroM.Web.Services;
using System.Data;
using System.Text.Json;
using static MicroM.Database.ApplicationDatabase;


namespace MicroM.Configuration.Entities;

public class ApplicationsDef : EntityDefinition
{
    public ApplicationsDef() : base("app", nameof(Applications)) { SQLCreationOptions = SQLCreationOptionsMetadata.WithIUpdate; }

    public readonly Column<string> c_application_id = Column<string>.PK();
    public readonly Column<string> vc_appname = Column<string>.Text();
    public readonly Column<string[]> vc_appurls = Column<string[]>.Text(size: 0, isArray: true, fake: true);
    public readonly Column<string> vc_apiurl = Column<string>.Text(size: 2048);
    public readonly Column<string> vc_server = Column<string>.Text();
    public readonly Column<string> vc_user = Column<string>.Text();
    public readonly Column<string?> vc_password = Column<string?>.Text(size: 2048, encrypted: true, nullable: true);
    public readonly Column<string> vc_database = Column<string>.Text();

    public readonly Column<string?> vc_app_admin_user = Column<string?>.Text(nullable: true);
    public readonly Column<string?> vc_app_admin_password = Column<string?>.Text(size: 2048, encrypted: true, nullable: true);

    public readonly Column<string> vc_JWTIssuer = Column<string>.Text();
    public readonly Column<string?> vc_JWTAudience = Column<string?>.Text(nullable: true);
    public readonly Column<string?> vc_JWTKey = Column<string?>.Text(nullable: true);

    public readonly Column<int> i_JWTTokenExpirationMinutes = new();
    public readonly Column<int> i_JWTRefreshExpirationHours = new();
    public readonly Column<int> i_AccountLockoutMinutes = new();
    public readonly Column<int> i_MaxBadLogonAttempts = new();
    public readonly Column<int> i_MaxRefreshTokenAttempts = new();

    public readonly Column<string?> vc_app_schema = Column<string?>.Text(size: 50, nullable: true);
    public readonly Column<string?> vc_datadictionary_schema = Column<string?>.Text(size: 50, nullable: true);

    public readonly Column<bool?> b_enable_seed_test_data = new(nullable: true);
    public readonly Column<bool?> b_enable_developer_tools = new(nullable: true);
    public readonly Column<bool?> b_enable_update_on_hotreload = new(nullable: true);

    public readonly Column<string?> vc_ts_categories_folder = Column<string?>.Text(nullable: true);
    public readonly Column<string?> vc_ts_dd_categories_values_class_name = Column<string?>.Text(nullable: true);
    public readonly Column<string?> vc_ts_dd_categories_values_class_import = Column<string?>.Text(nullable: true);

    public readonly Column<string> c_authenticationtype_id = Column<string>.EmbedCategory(nameof(AuthenticationTypes));

    // application assemblies embedded columns
    public readonly Column<string?> vc_assembly1 = Column<string?>.Text(size: 2048, nullable: true, fake: true);
    public readonly Column<string?> vc_assembly2 = Column<string?>.Text(size: 2048, nullable: true, fake: true);
    public readonly Column<string?> vc_assembly3 = Column<string?>.Text(size: 2048, nullable: true, fake: true);
    public readonly Column<string?> vc_assembly4 = Column<string?>.Text(size: 2048, nullable: true, fake: true);
    public readonly Column<string?> vc_assembly5 = Column<string?>.Text(size: 2048, nullable: true, fake: true);

    // Fakes for actions and status
    public readonly Column<bool> b_createdatabase = new(fake: true, column_flags: ColumnFlags.None, value: false);
    public readonly Column<bool> b_dropdatabase = new(fake: true, column_flags: ColumnFlags.None, value: false);
    public readonly Column<bool> b_updatedatabase = new(fake: true, column_flags: ColumnFlags.None, value: false);

    public readonly Column<bool> b_adminuserhasrights = new(fake: true, column_flags: ColumnFlags.None, value: false);
    public readonly Column<bool> b_appdbexists = new(fake: true, column_flags: ColumnFlags.None, value: false);
    public readonly Column<bool> b_appuserexists = new(fake: true, column_flags: ColumnFlags.None, value: false);
    public readonly Column<bool> b_serverup = new(fake: true, column_flags: ColumnFlags.None, value: false);

    // Indentity provider embedded columns
    public readonly Column<string> c_identity_provider_role_id = Column<string>.EmbedCategory(nameof(IdentityProviderRole));

    // if acting as a client, this is the URL to the OIDC well-known configuration
    public readonly Column<string?> vc_oidc_url_wellknown = Column<string?>.Text(size: 2048, nullable: true, fake: true);

    // if login in to IdP app this is the subject pepper to use when creating subject claim
    public readonly Column<string?> vc_oidc_idp_subject_pepper = Column<string?>.Text(size: 2048, nullable: true, fake: true, encrypted: true);

    // certificate embedded columns
    public readonly Column<string?> vc_certificate_unique_id = Column<string?>.Text(size: 2048, fake: true);
    public readonly Column<byte[]> vb_certificate_blob = new(size: 0, fake: true);
    public readonly Column<string> vc_certificate_password = Column<string>.Text(size: 2048, encrypted: true, fake: true);

    public readonly ViewDefinition app_brwStandard = new(nameof(c_application_id));

    public readonly ProcedureDefinition app_GetConfiguration = new(readonly_locks: true);
    public readonly ProcedureDefinition app_GetOIDCClients = new(readonly_locks: true);
    public readonly ProcedureDefinition app_GetADConfiguration = new(readonly_locks: true);

    public readonly APPOIDCDiagnostics APPOIDCDiagnostics = new();
}

public class Applications : Entity<ApplicationsDef>
{
    public Applications() : base() { }
    public Applications(string? schema_name) : base(schema_name) { }
    public Applications(IEntityClient ec, IMicroMEncryption? encryptor = null, string? schema_name = null) : base(ec, encryptor, schema_name) { }

    private ApplicationOption GetAppConfig(IWebAPIServices? api)
    {
        ArgumentNullException.ThrowIfNull(api, nameof(api));
        ApplicationOption? app_cfg = api.app_config.GetAppConfiguration(Def.c_application_id.Value);
        ArgumentNullException.ThrowIfNull(app_cfg, nameof(app_cfg));

        return app_cfg;
    }

    private async Task<DBStatusResult> PerformCreateOrDropDatabase(CancellationToken ct, MicroMOptions? options = null, Dictionary<string, object>? server_claims = null, IWebAPIServices? api = null)
    {
        bool dropDatabaseEnabled = options?.EnableDropDatabases ?? false;

        if (Def.b_createdatabase.Value && Def.b_appdbexists.Value)
        {
            return new() { Failed = true, Results = [new() { Status = DBStatusCodes.Error, Message = "Can't create the APP Database. Reason: The database already exists." }] };
        }

        if (dropDatabaseEnabled && Def.b_dropdatabase.Value && Def.b_appdbexists.Value == false)
        {
            return new() { Failed = true, Results = [new() { Status = DBStatusCodes.Error, Message = "Can't drop the APP Database. Reason: The database doesn't exist." }] };
        }

        if (dropDatabaseEnabled == false && Def.b_dropdatabase.Value)
        {
            return new() { Failed = true, Results = [new() { Status = DBStatusCodes.Error, Message = "Can't drop the APP Database. Reason: Dropping existing databases has been disabled." }] };
        }

        if (dropDatabaseEnabled && Def.b_dropdatabase.Value)
        {
            await DropAppDatabase(this, ct, options, server_claims, api);
        }

        if (Def.b_createdatabase.Value)
        {
            var app_cfg = GetAppConfig(api);
            var result = await CreateAppDatabase(this, drop_and_recreate: false, app_cfg, ct, options, server_claims, api);
            if (result.Failed) return result;
        }

        if (Def.b_createdatabase.Value == false && Def.b_dropdatabase.Value == false && Def.b_appdbexists.Value && Def.b_updatedatabase.Value == true)
        {
            var app_cfg = GetAppConfig(api);
            var result = await UpdateAppDatabase(this, app_cfg, ct, server_claims, options, api);
            if (result.Failed) return result;
        }

        return new() { Results = [new() { Status = DBStatusCodes.OK }] };
    }

    public override async Task<DBStatusResult> InsertData(CancellationToken ct, bool throw_dbstat_exception = false, MicroMOptions? options = null, Dictionary<string, object>? server_claims = null, IWebAPIServices? api = null, string? app_id = null)
    {
        var ec = Client;
        try
        {
            await ec.Connect(ct);

            Def.vc_password.Value = CryptClass.CreateRandomPassword();
            Def.vc_JWTKey.Value = CryptClass.CreateRandomPassword();
            Def.vc_app_admin_password.Value = CryptClass.CreateRandomPassword();


            var cert = MicromApplicationCertificates.CreateNewApplicationCertificate(Def.c_application_id.Value);

            Def.vc_certificate_unique_id.Value = cert.guid.ToString();
            Def.vb_certificate_blob.Value = cert.certificate;
            Def.vc_certificate_password.Value = cert.password;

            if (Def.c_identity_provider_role_id.Value == nameof(IdentityProviderRole.IDPServer))
            {
                Def.vc_oidc_idp_subject_pepper.Value = CryptClass.CreateRandomPassword();
            }
            else
            {
                Def.vc_oidc_idp_subject_pepper.Value = null;
            }

            if (Def.c_identity_provider_role_id.Value != nameof(IdentityProviderRole.IDPClient))
            {
                Def.vc_oidc_url_wellknown.Value = null;
            }

            var result = await base.InsertData(ct, throw_dbstat_exception, options, server_claims, api);
            if (!result.Failed && api != null)
            {
                var refreshed = await api.app_config.RefreshConfiguration(Def.c_application_id.Value.Trim(), ct);
                if (!refreshed)
                {
                    return new() { Failed = true, Results = [new() { Status = DBStatusCodes.Error, Message = "Failed to refresh application configuration after insert." }] };
                }

                result = await PerformCreateOrDropDatabase(ct, options, server_claims, api);
                if (!result.Failed)
                {
                    await api.securityService.RefreshGroupsSecurityRecords(Def.c_application_id.Value.Trim(), ct);
                }
            }
            return result;
        }
        finally
        {
            await ec.Disconnect();
        }
    }

    public override async Task<DBStatusResult> UpdateData(CancellationToken ct, bool throw_dbstat_exception = false, MicroMOptions? options = null, Dictionary<string, object>? server_claims = null, IWebAPIServices? api = null, string? app_id = null)
    {
        var ec = Client;
        try
        {
            await ec.Connect(ct);

            if (Def.b_createdatabase.Value)
            {
                Def.vc_app_admin_password.Value = CryptClass.CreateRandomPassword();
            }

            var cert = MicromApplicationCertificates.CreateNewApplicationCertificate(Def.c_application_id.Value);

            Def.vc_certificate_unique_id.Value = cert.guid.ToString();
            Def.vb_certificate_blob.Value = cert.certificate;
            Def.vc_certificate_password.Value = cert.password;

            if (Def.c_identity_provider_role_id.Value == nameof(IdentityProviderRole.IDPServer))
            {
                if (Def.vc_oidc_idp_subject_pepper.Value.IsNullOrEmpty())
                {
                    Def.vc_oidc_idp_subject_pepper.Value = CryptClass.CreateRandomPassword();
                }
            }
            else
            {
                Def.vc_oidc_idp_subject_pepper.Value = null;
            }

            if (Def.c_identity_provider_role_id.Value != nameof(IdentityProviderRole.IDPClient))
            {
                Def.vc_oidc_url_wellknown.Value = null;
            }

            var result = await base.UpdateData(ct, throw_dbstat_exception, options, server_claims, api);

            if (!result.Failed && api != null)
            {
                var refreshed = await api.app_config.RefreshConfiguration(Def.c_application_id.Value, ct);
                if (!refreshed)
                {
                    return new() { Failed = true, Results = [new() { Status = DBStatusCodes.Error, Message = "Failed to refresh application configuration after update." }] };
                }

                result = await PerformCreateOrDropDatabase(ct, options, server_claims, api);
                if (!result.Failed)
                {
                    await api.securityService.RefreshGroupsSecurityRecords(Def.c_application_id.Value, ct);
                }
            }
            return result;
        }
        finally
        {
            await ec.Disconnect();
        }
    }

    public override async Task<bool> GetData(CancellationToken ct, MicroMOptions? options = null, Dictionary<string, object>? server_claims = null, IWebAPIServices? api = null, string? app_id = null)
    {
        bool should_close = !(Client.ConnectionState == System.Data.ConnectionState.Open);
        bool result = false;

        try
        {
            await Client.Connect(ct);

            result = await base.GetData(ct, options, server_claims, api);
            if (result)
            {
                await GetAppDatabaseStatus(this, ct, options, server_claims, api);
            }

        }
        finally
        {
            if (should_close) await Client.Disconnect();
        }

        return result;
    }

    public async static Task<List<ApplicationOption>?> GetAPPSConfiguration(IEntityClient ec, CancellationToken ct, IMicroMEncryption? encryptor = null)
    {
        List<ApplicationOption> result = [];
        Applications app = new(ec);

        try
        {
            await ec.Connect(ct);
            result = await app.Data.ExecuteProc(app.Def.app_GetConfiguration, ct, set_parms_from_columns: false, mapper: async (IValueReader fv, string[] headers, string[] typeInfo, CancellationToken ct) =>
            {
                ApplicationOption app_result = new()
                {
                    ApplicationID = await fv.GetFieldValueAsync<string>(nameof(app_result.ApplicationID), ct),
                    ApplicationName = await fv.GetFieldValueAsync<string>(nameof(app_result.ApplicationName), ct),
                    SQLServer = await fv.GetFieldValueAsync<string>(nameof(app_result.SQLServer), ct),
                    SQLDB = await fv.GetFieldValueAsync<string>(nameof(app_result.SQLDB), ct),
                    SQLUser = await fv.GetFieldValueAsync<string>(nameof(app_result.SQLUser), ct),
                    SQLPassword = await fv.GetFieldValueAsync<string>(nameof(app_result.SQLPassword), ct),
                    JWTAudience = await fv.GetFieldValueAsync<string?>(nameof(app_result.JWTAudience), ct),
                    JWTIssuer = await fv.GetFieldValueAsync<string>(nameof(app_result.JWTIssuer), ct),
                    JWTKey = await fv.GetFieldValueAsync<string>(nameof(app_result.JWTKey), ct),
                    JWTRefreshExpirationHours = await fv.GetFieldValueAsync<int>(nameof(app_result.JWTRefreshExpirationHours), ct),
                    JWTTokenExpirationMinutes = await fv.GetFieldValueAsync<int>(nameof(app_result.JWTTokenExpirationMinutes), ct),
                    AccountLockoutMinutes = await fv.GetFieldValueAsync<int>(nameof(app_result.AccountLockoutMinutes), ct),
                    MaxBadLogonAttempts = await fv.GetFieldValueAsync<int>(nameof(app_result.MaxBadLogonAttempts), ct),
                    MaxRefreshTokenAttempts = await fv.GetFieldValueAsync<int>(nameof(app_result.MaxRefreshTokenAttempts), ct),
                    IdentityProviderRoleType = await fv.GetFieldValueAsync<string>(nameof(app_result.IdentityProviderRoleType), ct),
                    AuthenticationType = await fv.GetFieldValueAsync<string?>(nameof(app_result.AuthenticationType), ct),
                    OIDCWellKnownURL = await fv.GetFieldValueAsync<string?>(nameof(app_result.OIDCWellKnownURL), ct),
                    OIDCCertificateUniqueID = await fv.GetFieldValueAsync<string?>(nameof(app_result.OIDCCertificateUniqueID), ct),
                    OIDCCertificateBlob = await fv.GetFieldValueAsync<byte[]?>(nameof(app_result.OIDCCertificateBlob), ct),
                    OIDCCertificatePassword = await fv.GetFieldValueAsync<string>(nameof(app_result.OIDCCertificatePassword), ct),
                    OIDCIdPSubjectPepper = await fv.GetFieldValueAsync<string?>(nameof(app_result.OIDCIdPSubjectPepper), ct),
                    EnableDeveloperTools = await fv.GetFieldValueAsync<bool?>(nameof(app_result.EnableDeveloperTools), ct) ?? false,
                    EnableSeedTestData = await fv.GetFieldValueAsync<bool?>(nameof(app_result.EnableSeedTestData), ct) ?? false,
                    EnableUpdateOnHotReload = await fv.GetFieldValueAsync<bool?>(nameof(app_result.EnableUpdateOnHotReload), ct) ?? false,
                    TypeScriptCategoriesFolder = await fv.GetFieldValueAsync<string?>(nameof(app_result.TypeScriptCategoriesFolder), ct) ?? TemplateValues.CONST_EMBEDDED_CATEGORIES_FOLDER,
                    TypeScriptDDCategoriesValuesClassName = await fv.GetFieldValueAsync<string?>(nameof(app_result.TypeScriptDDCategoriesValuesClassName), ct) ?? TemplateValues.CONST_CATEGORIES_VALUES_CLASS,
                    TypeScriptDDCategoriesValuesClassImport = await fv.GetFieldValueAsync<string?>(nameof(app_result.TypeScriptDDCategoriesValuesClassImport), ct) ?? TemplateValues.CONST_MICROM_LIB_PACKAGE,

                    SchemaConfiguration = new AppDBSchemaConfiguration
                    (
                        APPSchema: await fv.GetFieldValueAsync<string?>(nameof(AppDBSchemaConfiguration.APPSchema), ct) ?? "dbo",
                        DDSchema: await fv.GetFieldValueAsync<string?>(nameof(AppDBSchemaConfiguration.DDSchema), ct) ?? "dbo"
                    )
                };

                var appurls = await fv.GetFieldValueAsync<string?>(nameof(app_result.FrontendURLS), ct);
                if (!string.IsNullOrEmpty(appurls))
                {
                    app_result.FrontendURLS = JsonSerializer.Deserialize<List<string>>(appurls) ?? [];
                }

                if (encryptor != null)
                {
                    app_result.SQLPassword = encryptor.Decrypt(app_result.SQLPassword);
                    app_result.OIDCCertificatePassword = encryptor.Decrypt(app_result.OIDCCertificatePassword);
                    app_result.OIDCIdPSubjectPepper = encryptor.Decrypt(app_result.OIDCIdPSubjectPepper!);
                }

                return app_result;
            });

            List<OIDCClientConfigurationOption> oidc_clients = [];
            oidc_clients = await app.Data.ExecuteProc(app.Def.app_GetOIDCClients, ct, set_parms_from_columns: false, mapper: async (IValueReader fv, string[] headers, string[] typeInfo, CancellationToken ct) =>
                    {
                        OIDCClientConfigurationOption client_result = new()
                        {
                            ApplicationID = await fv.GetFieldValueAsync<string>(nameof(client_result.ApplicationID), ct),
                            ClientAPPID = await fv.GetFieldValueAsync<string>(nameof(client_result.ClientAPPID), ct),
                            URLFrontChannelLogout = await fv.GetFieldValueAsync<string>(nameof(client_result.URLFrontChannelLogout), ct),
                            URLBackchannelLogout = await fv.GetFieldValueAsync<string>(nameof(client_result.URLBackchannelLogout), ct),
                            URLClientJWKS = await fv.GetFieldValueAsync<string>(nameof(client_result.URLClientJWKS), ct),
                            CertificateUniqueID = await fv.GetFieldValueAsync<string>(nameof(client_result.CertificateUniqueID), ct),
                            APIKey = await fv.GetFieldValueAsync<string>(nameof(client_result.APIKey), ct),
                            APISecret = await fv.GetFieldValueAsync<string>(nameof(client_result.APISecret), ct),
                            OIDCSubjectPepper = await fv.GetFieldValueAsync<string>(nameof(client_result.OIDCSubjectPepper), ct),
                        };

                        var redirect_urls = await fv.GetFieldValueAsync<string?>(nameof(client_result.URLAuthorizedRedirects), ct);
                        if (!string.IsNullOrEmpty(redirect_urls))
                        {
                            client_result.URLAuthorizedRedirects = JsonSerializer.Deserialize<List<string>>(redirect_urls) ?? [];
                        }

                        if (encryptor != null)
                        {
                            client_result.APIKey = encryptor.Decrypt(client_result.APIKey);
                            client_result.APISecret = encryptor.Decrypt(client_result.APISecret);
                            client_result.OIDCSubjectPepper = encryptor.Decrypt(client_result.OIDCSubjectPepper);
                        }

                        return client_result;
                    });

            // Assign clients to the right application
            foreach (var app_item in result)
            {
                var clients = oidc_clients.Where(c => c.ApplicationID == app_item.ApplicationID).ToList();
                if (clients.Count > 0)
                {
                    app_item.OIDCClientConfiguration = clients.ToDictionary(c => c.ClientAPPID, c => c);
                }
            }

            // AD configuration
            List<ADConfigurationOption> ad_configs = [];
            ad_configs = await app.Data.ExecuteProc(app.Def.app_GetADConfiguration, ct, set_parms_from_columns: false, mapper: async (fv, headers, typeInfo, ct) =>
            {
                ADConfigurationOption ad_result = new()
                {
                    ADConfigurationID = await fv.GetFieldValueAsync<string>(nameof(ad_result.ADConfigurationID), ct),
                    ApplicationID = await fv.GetFieldValueAsync<string>(nameof(ad_result.ApplicationID), ct),
                    ADDomain = await fv.GetFieldValueAsync<string>(nameof(ad_result.ADDomain), ct),
                    ADUserPrincipalDomain = await fv.GetFieldValueAsync<string>(nameof(ad_result.ADUserPrincipalDomain), ct),
                    ADContainer = await fv.GetFieldValueAsync<string>(nameof(ad_result.ADContainer), ct),
                    ADServerIP = await fv.GetFieldValueAsync<string>(nameof(ad_result.ADServerIP), ct),
                    ADUser = await fv.GetFieldValueAsync<string>(nameof(ad_result.ADUser), ct),
                    ADPassword = await fv.GetFieldValueAsync<string>(nameof(ad_result.ADPassword), ct),
                    CreateUserOnLogin = await fv.GetFieldValueAsync<bool>(nameof(ad_result.CreateUserOnLogin), ct),
                    DefaultUserGroupID = await fv.GetFieldValueAsync<string?>(nameof(ad_result.DefaultUserGroupID), ct),
                };
                if (encryptor != null)
                {
                    ad_result.ADUser = encryptor.Decrypt(ad_result.ADUser);
                    ad_result.ADPassword = encryptor.Decrypt(ad_result.ADPassword);
                }
                return ad_result;
            });

            // Assign AD configs to the right application
            foreach (var app_item in result)
            {
                var ad_config = ad_configs.Where(c => c.ApplicationID == app_item.ApplicationID).ToList();
                if (ad_config.Count > 0)
                {
                    app_item.ADConfiguration = ad_config.ToDictionary(c => c.ADUserPrincipalDomain, c => c);
                }
            }
        }
        finally
        {
            await ec.Disconnect();
        }

        return result;
    }

}
