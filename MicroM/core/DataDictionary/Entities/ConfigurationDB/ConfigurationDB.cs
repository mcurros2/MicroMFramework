using MicroM.Configuration;
using MicroM.Core;
using MicroM.Data;
using MicroM.Web.Services;
using System.Data;
using static MicroM.DataDictionary.ConfigurationDBHandlers;
using static System.ArgumentNullException;

namespace MicroM.DataDictionary
{
    public class ConfigurationDBDef : EntityDefinition
    {
        public ConfigurationDBDef() : base("dfg", nameof(ConfigurationDB)) { Fake = true; }

        public readonly Column<string> c_confgidb_id = Column<string>.PK(value: "1");

        public readonly Column<string> vc_configsqlserver = new(sql_type: SqlDbType.VarChar, size: 255);
        public readonly Column<string> vc_configsqluser = new(sql_type: SqlDbType.VarChar, size: 255);
        public readonly Column<string?> vc_configsqlpassword = new(sql_type: SqlDbType.VarChar, size: 2048, nullable: true);
        public readonly Column<string> vc_configdatabase = new(sql_type: SqlDbType.VarChar, size: 255);

        public readonly Column<string> vc_certificatethumbprint = new(sql_type: SqlDbType.VarChar, size: 255);
        public readonly Column<string> vc_certificatepassword = new(sql_type: SqlDbType.VarChar, size: 2048);
        public readonly Column<string> vc_certificatename = new(sql_type: SqlDbType.VarChar, size: 2048);

        public readonly Column<bool> b_adminuserhasrights = new(sql_type: SqlDbType.Bit);
        public readonly Column<bool> b_configdbexists = new(sql_type: SqlDbType.Bit);
        public readonly Column<bool> b_configuserexists = new(sql_type: SqlDbType.Bit);
        public readonly Column<bool> b_secretsconfigured = new(sql_type: SqlDbType.Bit);
        public readonly Column<bool> b_defaultcertificate = new(sql_type: SqlDbType.Bit);
        public readonly Column<bool> b_thumbprintconfigured = new(sql_type: SqlDbType.Bit);
        public readonly Column<bool> b_thumbprintfound = new(sql_type: SqlDbType.Bit);
        public readonly Column<bool> b_certificatefound = new(sql_type: SqlDbType.Bit);
        public readonly Column<bool> b_secretsfilevalid = new(sql_type: SqlDbType.Bit);

        public readonly Column<bool> b_recreatedatabase = new(sql_type: SqlDbType.Bit);

        public ViewDefinition dfg_brwStandard { get; private set; } = new(nameof(c_confgidb_id));

    }

    public class ConfigurationDB : Entity<ConfigurationDBDef>
    {
        public ConfigurationDB() : base() { }
        public ConfigurationDB(IEntityClient ec, IMicroMEncryption? encryptor = null) : base(ec, encryptor) { }


        public override async Task<bool> GetData(CancellationToken ct, MicroMOptions? options = null, Dictionary<string, object>? server_claims = null, IMicroMWebAPI? api = null, string? app_id = null)
        {
            ThrowIfNull(server_claims);
            ThrowIfNull(options);
            return await HandleGetData(this, options, server_claims, ct);
        }

        public override async Task<DBStatusResult> UpdateData(CancellationToken ct, bool throw_dbstat_exception = false, MicroMOptions? options = null, Dictionary<string, object>? server_claims = null, IMicroMWebAPI? api = null, string? app_id = null)
        {
            ThrowIfNull(server_claims);
            ThrowIfNull(options);
            return await HandleUpdateData(this, throw_dbstat_exception, options, server_claims, api, ct);
        }
    }
}
