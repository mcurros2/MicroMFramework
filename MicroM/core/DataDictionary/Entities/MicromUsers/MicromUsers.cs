
using MicroM.Configuration;
using MicroM.Core;
using MicroM.Data;
using MicroM.DataDictionary.CategoriesDefinitions;
using MicroM.DataDictionary.Entities.MicromUsers;
using MicroM.Extensions;
using MicroM.Web.Authentication;
using MicroM.Web.Services;
using System.Data;

namespace MicroM.DataDictionary
{

    public class MicromUsersDef : EntityDefinition
    {
        public MicromUsersDef() : base("usr", nameof(MicromUsers)) { }

        public readonly Column<string> c_user_id = Column<string>.PK(autonum: true);
        public readonly Column<string> vc_username = Column<string>.Text();
        public readonly Column<string?> vc_email = Column<string?>.Text(nullable: true);
        public readonly Column<string> vc_pwhash = Column<string>.Text(size: 2048);
        public readonly Column<string?> vb_sid = Column<string?>.Text(size: 85, nullable: true);
        public readonly Column<int> i_badlogonattempts = new(value: 0);
        public readonly Column<bool> bt_disabled = new();
        public readonly Column<DateTime?> dt_locked = new();
        public readonly Column<DateTime?> dt_last_login = new();
        public readonly Column<DateTime?> dt_last_refresh = new();

        public readonly Column<string?> vc_recovery_code = Column<string?>.Text(nullable: true);
        public readonly Column<DateTime?> dt_last_recovery = new(nullable: true);

        public readonly Column<string> c_usertype_id = Column<string>.EmbedCategory(nameof(UserTypes));

        public readonly Column<string[]?> vc_user_groups = Column<string[]?>.Text(size: 0, nullable: true, isArray: true, fake: true);

        public readonly Column<bool> bt_islocked = new(column_flags: ColumnFlags.None, fake: true);
        public readonly Column<int> i_locked_minutes_remaining = new(fake: true, column_flags: ColumnFlags.None);

        public readonly Column<string> vc_password = Column<string>.Text(column_flags: ColumnFlags.None, fake: true);

        public readonly ViewDefinition usr_brwStandard = new(nameof(c_user_id));

        public ProcedureDefinition usr_getUserData { get; private set; } = null!;

        public ProcedureDefinition usr_updateLoginAttempt { get; private set; } = null!;

        public readonly ProcedureDefinition usr_logoff = new(nameof(vc_username));
        public readonly ProcedureDefinition usr_setPassword = new(nameof(vc_username), nameof(vc_pwhash));
        public readonly ProcedureDefinition usr_resetPassword = new(nameof(vc_username));

        public readonly ProcedureDefinition usr_GetClientClaims = new(readonly_locks: true, nameof(vc_username));
        public readonly ProcedureDefinition usr_GetServerClaims = new(readonly_locks: true, nameof(vc_username));
        public readonly ProcedureDefinition usr_GetEnabledMenus = new(readonly_locks: true, nameof(vc_username));

        public readonly ProcedureDefinition usr_GetRecoveryCode = new(nameof(vc_username));
        public readonly ProcedureDefinition usr_GetRecoveryEmails = new(nameof(vc_username));
        public readonly ProcedureDefinition usr_RecoverPassword = new(nameof(vc_username), nameof(vc_recovery_code), nameof(vc_pwhash));

        protected override void DefineProcs()
        {
            string success, account_lockout_mins, refresh_expiration_hours, max_bad_logon_attempts, new_refresh_token, device_id, user_agent, ipaddress;

            usr_updateLoginAttempt = new ProcedureDefinition();
            usr_updateLoginAttempt.AddParmFromCol<string>(c_user_id);
            usr_updateLoginAttempt.AddParm<string?>(nameof(new_refresh_token), sql_type: SqlDbType.VarChar, size: 255);
            usr_updateLoginAttempt.AddParm<bool>(nameof(success), sql_type: SqlDbType.Bit);
            usr_updateLoginAttempt.AddParm<int>(nameof(account_lockout_mins), sql_type: SqlDbType.Int);
            usr_updateLoginAttempt.AddParm<int>(nameof(refresh_expiration_hours), sql_type: SqlDbType.Int);
            usr_updateLoginAttempt.AddParm<int>(nameof(max_bad_logon_attempts), sql_type: SqlDbType.Int);
            usr_updateLoginAttempt.AddParm<string>(nameof(device_id), sql_type: SqlDbType.VarChar, size: 255);
            usr_updateLoginAttempt.AddParm<string>(nameof(user_agent), sql_type: SqlDbType.VarChar, size: 4096);
            usr_updateLoginAttempt.AddParm<string>(nameof(ipaddress), sql_type: SqlDbType.VarChar, size: 40);

            usr_getUserData = new ProcedureDefinition(readonly_locks: true);
            usr_getUserData.AddParmFromCol<string>(vc_username);
            usr_getUserData.AddParmFromCol<string>(c_user_id);
            usr_getUserData.AddParm<string>(nameof(device_id), sql_type: SqlDbType.VarChar, size: 255);


        }

        public readonly EntityUniqueConstraint UNUsername = new(keys: nameof(vc_username));

        public readonly EntityForeignKey<MicromUsersGroups, MicromUsers> FKGroups = new(fake: true);
    }

    public class MicromUsers : Entity<MicromUsersDef>
    {
        public MicromUsers() : base() { }
        public MicromUsers(IEntityClient ec, IMicroMEncryption? encryptor = null) : base(ec, encryptor) { }

        public override Task<DBStatusResult> InsertData(CancellationToken ct, bool throw_dbstat_exception = false, MicroMOptions? options = null, Dictionary<string, object>? server_claims = null, IMicroMWebAPI? api = null, string? app_id = null)
        {
            var user_login = new UserLogin
            {
                Username = this.Def.vc_username.Value,
                Password = this.Def.vc_password.Value,
            };

            this.Def.vc_pwhash.Value = UserPasswordHasher.HashPassword(user_login, user_login.Password);

            return base.InsertData(ct, throw_dbstat_exception, options, server_claims, api);
        }

        public async static Task<DBStatusResult> Logoff(string username, IEntityClient ec, CancellationToken ct)
        {
            MicromUsers user = new(ec);

            var proc = user.Def.usr_logoff;
            proc[nameof(MicromUsersDef.vc_username)].ValueObject = username;
            return await user.Data.ExecuteProcDBStatus(ct, proc);
        }

        public async static Task<LoginData?> GetUserData(string? username, string? user_id, string device_id, IEntityClient ec, CancellationToken ct)
        {
            MicromUsers user = new(ec);

            var proc = user.Def.usr_getUserData;
            if (username != null) proc[nameof(MicromUsersDef.vc_username)].ValueObject = username;
            if (user_id != null) proc[nameof(MicromUsersDef.c_user_id)].ValueObject = user_id;
            if (device_id != null) proc[nameof(device_id)].ValueObject = device_id;
            var result = await user.Data.ExecuteProcSingleRow<LoginData>(ct, proc, set_parms_from_columns: false, mode: IEntityClient.AutoMapperMode.ByNameLaxNotThrow);

            return result;
        }

        internal async Task<Dictionary<string, string>> GetClientClaims(CancellationToken ct)
        {
            var proc = Def.usr_GetClientClaims;

            var result = await Data.ExecuteProc(ct, proc, set_parms_from_columns: true);

            if (result.HasData())
            {
                return result[0].ToDictionaryOfStringRecord(0, StringComparer.OrdinalIgnoreCase);
            }

            return [];
        }

        internal async Task<Dictionary<string, object>> GetServerClaims(CancellationToken ct)
        {
            var proc = Def.usr_GetServerClaims;

            var result = await Data.ExecuteProc(ct, proc, set_parms_from_columns: true);

            if (result.HasData())
            {
                return result[0].ToDictionary(0);
            }

            return [];
        }

        public async static Task<(Dictionary<string, object> server_claims, Dictionary<string, string> client_claims)> GetClaims(string username, IEntityClient ec, CancellationToken ct)
        {
            Dictionary<string, object> server_claims = [];
            Dictionary<string, string> client_claims = [];

            try
            {
                MicromUsers user = new(ec);
                await ec.Connect(ct);

                user.Def.vc_username.Value = username;

                client_claims = await user.GetClientClaims(ct);
                server_claims = await user.GetServerClaims(ct);

            }
            finally
            {
                await ec.Disconnect();
            }

            return (server_claims, client_claims);
        }


        public async static Task<LoginAttemptResult> UpdateLoginAttempt(string user_id, string device_id, string? new_refresh_token, bool success, int account_lockout_mins, int refresh_expiration_hours, int max_bad_logon_attempts, string ipaddress, string user_agent, IEntityClient ec, CancellationToken ct)
        {
            MicromUsers user = new(ec);
            LoginAttemptResult result = new();


            var proc = user.Def.usr_updateLoginAttempt;
            proc[nameof(MicromUsersDef.c_user_id)].ValueObject = user_id;
            proc[nameof(device_id)].ValueObject = device_id;
            proc[nameof(new_refresh_token)].ValueObject = new_refresh_token!;
            proc[nameof(success)].ValueObject = success;
            proc[nameof(account_lockout_mins)].ValueObject = account_lockout_mins;
            proc[nameof(refresh_expiration_hours)].ValueObject = refresh_expiration_hours;
            proc[nameof(max_bad_logon_attempts)].ValueObject = max_bad_logon_attempts;
            proc[nameof(ipaddress)].ValueObject = ipaddress;
            proc[nameof(user_agent)].ValueObject = user_agent;
            var dbstat = await user.Data.ExecuteProcDBStatus(ct, proc, false, false);

            if (dbstat != null && dbstat.Results != null && dbstat.Results.Count > 0)
            {
                result.Status = (LoginAttemptStatus)dbstat.Results[0].Status;
                result.RefreshToken = dbstat.Results[0].Message;
                result.Message = dbstat.Results[0].Message;
            }

            return result;
        }

        public async static Task<RefreshTokenResult?> RefreshToken(string user_id, string device_id, string refreshtoken, string new_refresh_token, int refresh_expiration_hours, int max_refresh_count, IEntityClient ec, CancellationToken ct)
        {
            MicromUsersDevices user = new(ec);

            var proc = user.Def.usd_refreshToken;
            proc[nameof(MicromUsersDef.c_user_id)].ValueObject = user_id;
            proc[nameof(MicromUsersDevicesDef.c_device_id)].ValueObject = device_id;
            proc[nameof(MicromUsersDevicesDef.vc_refreshtoken)].ValueObject = refreshtoken;
            proc[nameof(new_refresh_token)].ValueObject = new_refresh_token;
            proc[nameof(refresh_expiration_hours)].ValueObject = refresh_expiration_hours;
            proc[nameof(max_refresh_count)].ValueObject = max_refresh_count;
            var result = await user.ExecuteProcSingleRow<RefreshTokenResult>(ct, proc, set_parms_from_columns: false, mode: IEntityClient.AutoMapperMode.ByNameLaxNotThrow);

            return result;
        }

        public async static Task<(string? recovery_code, string? error)> GetRecoveryCode(string username, IEntityClient ec, CancellationToken ct)
        {
            MicromUsers user = new(ec);

            user.Def.vc_username.Value = username;

            var result = await user.Data.ExecuteProcDBStatus(ct, user.Def.usr_GetRecoveryCode);

            if (result.Failed)
            {
                return (null, result?.Results?[0].Message);
            }

            string? recovery_code = result.Results?[0].Message;

            return (recovery_code, null);
        }

        public async static Task<List<string>> GetRecoveryEmails(string username, IEntityClient ec, CancellationToken ct)
        {
            MicromUsers user = new(ec);

            user.Def.vc_username.Value = username;

            var result = await user.Data.ExecuteProc(ct, user.Def.usr_GetRecoveryEmails);

            if (result == null) return [];

            var header_index = result[0].GetHeaderIndex(nameof(MicromUsersDef.vc_email));
            if (header_index == null) return [];

            return result[0].ToListOfStringColumn(header_index.Value);
        }

        public async static Task<DBStatusResult> RecoverPassword(string username, string recovery_code, string new_password, IEntityClient ec, CancellationToken ct)
        {
            MicromUsers user = new(ec);

            var proc = user.Def.usr_RecoverPassword;
            proc[nameof(MicromUsersDef.vc_username)].ValueObject = username;
            proc[nameof(MicromUsersDef.vc_recovery_code)].ValueObject = recovery_code;
            proc[nameof(MicromUsersDef.vc_pwhash)].ValueObject = UserPasswordHasher.HashPassword(new UserLogin { Username = username, Password = new_password }, new_password);

            return await user.Data.ExecuteProcDBStatus(ct, proc, set_parms_from_columns: false);
        }


        public async static Task<DBStatusResult> usr_setPassword(string username, string pwhash, IEntityClient ec, CancellationToken ct)
        {

            MicromUsers user = new(ec);

            var proc = user.Def.usr_setPassword;
            proc[nameof(MicromUsersDef.vc_username)].ValueObject = username;
            proc[nameof(MicromUsersDef.vc_pwhash)].ValueObject = pwhash;

            return await user.Data.ExecuteProcDBStatus(ct, proc);
        }

        public async static Task<DBStatusResult> usr_resetPassword(string username, IEntityClient ec, CancellationToken ct)
        {

            MicromUsers user = new(ec);

            var proc = user.Def.usr_resetPassword;
            proc[nameof(MicromUsersDef.vc_username)].ValueObject = username;

            return await user.Data.ExecuteProcDBStatus(ct, proc);
        }

    }


}
