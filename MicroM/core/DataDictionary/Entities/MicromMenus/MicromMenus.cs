
using MicroM.Configuration;
using MicroM.Core;
using MicroM.Data;
using MicroM.Web.Services;

namespace MicroM.DataDictionary.Entities;

public class MicromMenusDef : EntityDefinition
{
    public MicromMenusDef() : base("mme", nameof(MicromMenus), schemaName: DataDefaults.DataDictionarySchema) { }

    public readonly Column<string> c_menu_id = Column<string>.PK(size: 50);
    public readonly Column<string> vc_menu_name = Column<string>.Text();

    public readonly Column<DateTime>? dt_last_route_updated = new(nullable: true);

    public readonly ViewDefinition mme_brwStandard = new(nameof(c_menu_id));

}

public class MicromMenus : Entity<MicromMenusDef>
{
    public MicromMenus() : base() { }
    public MicromMenus(IEntityClient ec, IMicroMEncryption? encryptor = null) : base(ec, encryptor) { }

}
