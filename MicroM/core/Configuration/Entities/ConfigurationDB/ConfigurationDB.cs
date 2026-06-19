using MicroM.Core;
using MicroM.Data;
using MicroM.Web.Services;
using static MicroM.Configuration.Entities.ConfigurationDBHandlers;
using static System.ArgumentNullException;

namespace MicroM.Configuration.Entities;

public class ConfigurationDBDef : EntityDefinition
{
    public ConfigurationDBDef() : base("dfg", nameof(ConfigurationDB)) { Fake = true; }

    public readonly Column<string> c_confgidb_id = Column<string>.PK(value: "1");

    public readonly Column<string> vc_configsqlserver = Column<string>.Text();
    public readonly Column<string> vc_configsqluser = Column<string>.Text();
    public readonly Column<string?> vc_configsqlpassword = Column<string?>.Text(size: 2048, nullable: true);
    public readonly Column<string> vc_configdatabase = Column<string>.Text();

    public readonly Column<string> vc_certificatethumbprint = Column<string>.Text();
    public readonly Column<string> vc_certificatepassword = Column<string>.Text(size: 2048);
    public readonly Column<string> vc_certificatename = Column<string>.Text(size: 2048);

    public readonly Column<bool> b_adminuserhasrights = new();
    public readonly Column<bool> b_configdbexists = new();
    public readonly Column<bool> b_configuserexists = new();
    public readonly Column<bool> b_secretsconfigured = new();
    public readonly Column<bool> b_defaultcertificate = new();
    public readonly Column<bool> b_thumbprintconfigured = new();
    public readonly Column<bool> b_thumbprintfound = new();
    public readonly Column<bool> b_certificatefound = new();
    public readonly Column<bool> b_secretsfilevalid = new();

    public readonly Column<bool> b_recreatedatabase = new();

    public readonly ViewDefinition dfg_brwStandard = new(nameof(c_confgidb_id));

}

public class ConfigurationDB : Entity<ConfigurationDBDef>
{
    public ConfigurationDB() : base() { }
    public ConfigurationDB(string? schema_name) : base(schema_name) { }
    public ConfigurationDB(IEntityClient ec, IMicroMEncryption? encryptor = null, string? schema_name = null) : base(ec, encryptor, schema_name) { }


    public override async Task<bool> GetData(CancellationToken ct, MicroMOptions? options = null, Dictionary<string, object>? server_claims = null, IWebAPIServices? api = null, string? app_id = null)
    {
        ThrowIfNull(server_claims);
        ThrowIfNull(options);
        return await HandleGetData(this, options, server_claims, ct);
    }

    public override async Task<DBStatusResult> UpdateData(CancellationToken ct, bool throw_dbstat_exception = false, MicroMOptions? options = null, Dictionary<string, object>? server_claims = null, IWebAPIServices? api = null, string? app_id = null)
    {
        ThrowIfNull(server_claims);
        ThrowIfNull(options);
        return await HandleUpdateData(this, throw_dbstat_exception, options, server_claims, api, ct);
    }
}
