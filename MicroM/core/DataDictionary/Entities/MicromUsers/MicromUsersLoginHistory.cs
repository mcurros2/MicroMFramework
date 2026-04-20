using MicroM.Core;
using MicroM.Data;
using MicroM.Web.Services;

namespace MicroM.DataDictionary.Entities;

public class MicromUsersLoginHistoryDef : EntityDefinition
{
    public MicromUsersLoginHistoryDef() : base("ulh", nameof(MicromUsersLoginHistory)) { SQLCreationOptions = SQLCreationOptionsMetadata.WithIUpdate; }

    public readonly Column<string> c_user_history_id = Column<string>.PK(autonum: true);
    public readonly Column<string> c_user_id = Column<string>.PK();

    public readonly Column<string?> vc_useragent = Column<string?>.Text(size: 4096, nullable: true);
    public readonly Column<string?> vc_ipaddress = Column<string?>.Text(size: 40, nullable: true);
    public readonly Column<bool> bt_success = new();
    public readonly Column<DateTime> dt_login_attempt = new();

    public readonly ViewDefinition ulh_brwStandard = new(nameof(c_user_id));

}

public class MicromUsersLoginHistory : Entity<MicromUsersLoginHistoryDef>
{
    public MicromUsersLoginHistory() : base() { }
    public MicromUsersLoginHistory(string? schema_name) : base(schema_name) { }
    public MicromUsersLoginHistory(IEntityClient ec, IMicroMEncryption? encryptor = null, string? schema_name = null) : base(ec, encryptor, schema_name) { }

}
