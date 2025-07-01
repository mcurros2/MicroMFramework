using MicroM.Configuration;
using MicroM.Core;
using MicroM.Data;
using MicroM.DataDictionary.CategoriesDefinitions;
using MicroM.Web.Services;
using System.Data;
using static MicroM.Database.ApplicationDatabase;


namespace MicroM.DataDictionary
{
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
        public readonly Column<string> c_authenticationtype_id = Column<string>.EmbedCategory(nameof(AuthenticationTypes));

        public readonly Column<string?> vc_assembly1 = Column<string?>.Text(size: 2048, nullable: true, fake: true);
        public readonly Column<string?> vc_assembly2 = Column<string?>.Text(size: 2048, nullable: true, fake: true);
        public readonly Column<string?> vc_assembly3 = Column<string?>.Text(size: 2048, nullable: true, fake: true);
        public readonly Column<string?> vc_assembly4 = Column<string?>.Text(size: 2048, nullable: true, fake: true);
        public readonly Column<string?> vc_assembly5 = Column<string?>.Text(size: 2048, nullable: true, fake: true);



        public readonly Column<bool> b_createdatabase = new(sql_type: SqlDbType.Bit, fake: true, column_flags: ColumnFlags.None, value: false);
        public readonly Column<bool> b_dropdatabase = new(sql_type: SqlDbType.Bit, fake: true, column_flags: ColumnFlags.None, value: false);

        public readonly Column<bool> b_adminuserhasrights = new(sql_type: SqlDbType.Bit, fake: true, column_flags: ColumnFlags.None, value: false);
        public readonly Column<bool> b_appdbexists = new(sql_type: SqlDbType.Bit, fake: true, column_flags: ColumnFlags.None, value: false);
        public readonly Column<bool> b_appuserexists = new(sql_type: SqlDbType.Bit, fake: true, column_flags: ColumnFlags.None, value: false);
        public readonly Column<bool> b_serverup = new(sql_type: SqlDbType.Bit, fake: true, column_flags: ColumnFlags.None, value: false);

        public ViewDefinition app_brwStandard { get; private set; } = new(nameof(c_application_id));

        public ProcedureDefinition app_GetConfiguration { get; private set; } = new();
    }

    public class Applications : Entity<ApplicationsDef>
    {
        public Applications() : base() { }
        public Applications(IEntityClient ec, IMicroMEncryption? encryptor = null) : base(ec, encryptor) { }

        private async Task<DBStatusResult> PerformCreateOrDropDatabase(CancellationToken ct, MicroMOptions? options = null, Dictionary<string, object>? server_claims = null, IMicroMWebAPI? api = null)
        {

            if (this.Def.b_createdatabase.Value && this.Def.b_appdbexists.Value)
            {
                return new() { Failed = true, Results = [new() { Status = DBStatusCodes.Error, Message = "Can't create the APP Database. Reason: The database already exists." }] };
            }
            if (this.Def.b_dropdatabase.Value && this.Def.b_appdbexists.Value == false)
            {
                return new() { Failed = true, Results = [new() { Status = DBStatusCodes.Error, Message = "Can't drop the APP Database. Reason: The database doesn't exist." }] };
            }

            if (this.Def.b_dropdatabase.Value)
            {
                await DropAppDatabase(this, ct, options, server_claims, api);
            }
            if (this.Def.b_createdatabase.Value)
            {
                var result = await CreateAppDatabase(this, false, ct, options, server_claims, api);
                if (result.Failed) return result;
            }
            if (this.Def.b_createdatabase.Value == false && this.Def.b_dropdatabase.Value == false && this.Def.b_appdbexists.Value)
            {
                var result = await UpdateAppDatabase(this, ct, server_claims);
                if (result.Failed) return result;
            }
            return new() { Results = [new() { Status = DBStatusCodes.OK }] };
        }

        public override async Task<DBStatusResult> InsertData(CancellationToken ct, bool throw_dbstat_exception = false, MicroMOptions? options = null, Dictionary<string, object>? server_claims = null, IMicroMWebAPI? api = null, string? app_id = null)
        {
            this.Def.vc_password.Value = CryptClass.CreateRandomPassword();
            this.Def.vc_JWTKey.Value = CryptClass.CreateRandomPassword();
            this.Def.vc_app_admin_password.Value = CryptClass.CreateRandomPassword();

            var result = await base.InsertData(ct, throw_dbstat_exception, options, server_claims, api);
            if (!result.Failed && api != null)
            {
                result = await PerformCreateOrDropDatabase(ct, options, server_claims, api);
                if (!result.Failed) await api.RefreshConfig(this.Def.c_application_id.Value.Trim(), ct);
            }
            return result;
        }

        public override async Task<DBStatusResult> UpdateData(CancellationToken ct, bool throw_dbstat_exception = false, MicroMOptions? options = null, Dictionary<string, object>? server_claims = null, IMicroMWebAPI? api = null, string? app_id = null)
        {
            if (this.Def.b_createdatabase.Value)
            {
                this.Def.vc_app_admin_password.Value = CryptClass.CreateRandomPassword();
            }

            var result = await base.UpdateData(ct, throw_dbstat_exception, options, server_claims, api);

            if (!result.Failed && api != null)
            {
                result = await PerformCreateOrDropDatabase(ct, options, server_claims, api);
                if (!result.Failed)
                {
                    await api.RefreshConfig(this.Def.c_application_id.Value.Trim(), ct);
                }
            }
            return result;
        }

        public override async Task<bool> GetData(CancellationToken ct, MicroMOptions? options = null, Dictionary<string, object>? server_claims = null, IMicroMWebAPI? api = null, string? app_id = null)
        {
            bool should_close = !(this.Client.ConnectionState == System.Data.ConnectionState.Open);
            bool result = false;

            try
            {
                await this.Client.Connect(ct);

                result = await base.GetData(ct, options, server_claims, api);
                if (result)
                {
                    await GetAppDatabaseStatus(this, ct, options, server_claims, api);
                }

            }
            finally
            {
                if (should_close) await this.Client.Disconnect();
            }

            return result;
        }

        public async static Task<List<ApplicationOption>?> GetAPPSConfiguration(IEntityClient ec, CancellationToken ct, IMicroMEncryption? encryptor = null)
        {
            List<ApplicationOption> result = [];
            Applications app = new(ec);

            var proc = app.Def.app_GetConfiguration;
            try
            {
                await ec.Connect(ct);
                result = await app.Data.ExecuteProc<ApplicationOption>(ct, proc, set_parms_from_columns: false, mapper: async (IGetFieldValue fv, string[] headers, CancellationToken ct) =>
                {
                    ApplicationOption result = new()
                    {
                        ApplicationID = await fv.GetFieldValueAsync<string>(nameof(result.ApplicationID), ct),
                        ApplicationName = await fv.GetFieldValueAsync<string>(nameof(result.ApplicationName), ct),
                        SQLServer = await fv.GetFieldValueAsync<string>(nameof(result.SQLServer), ct),
                        SQLDB = await fv.GetFieldValueAsync<string>(nameof(result.SQLDB), ct),
                        SQLUser = await fv.GetFieldValueAsync<string>(nameof(result.SQLUser), ct),
                        SQLPassword = await fv.GetFieldValueAsync<string>(nameof(result.SQLPassword), ct),
                        JWTAudience = await fv.GetFieldValueAsync<string?>(nameof(result.JWTAudience), ct),
                        JWTIssuer = await fv.GetFieldValueAsync<string>(nameof(result.JWTIssuer), ct),
                        JWTKey = await fv.GetFieldValueAsync<string>(nameof(result.JWTKey), ct),
                        JWTRefreshExpirationHours = await fv.GetFieldValueAsync<int>(nameof(result.JWTRefreshExpirationHours), ct),
                        JWTTokenExpirationMinutes = await fv.GetFieldValueAsync<int>(nameof(result.JWTTokenExpirationMinutes), ct),
                        AccountLockoutMinutes = await fv.GetFieldValueAsync<int>(nameof(result.AccountLockoutMinutes), ct),
                        MaxBadLogonAttempts = await fv.GetFieldValueAsync<int>(nameof(result.MaxBadLogonAttempts), ct),
                        MaxRefreshTokenAttempts = await fv.GetFieldValueAsync<int>(nameof(result.MaxRefreshTokenAttempts), ct),
                    };

                    if (encryptor != null) result.SQLPassword = encryptor.Decrypt(result.SQLPassword);

                    return result;
                });

            }
            finally
            {
                await ec.Disconnect();
            }
            return result;
        }

    }

}
