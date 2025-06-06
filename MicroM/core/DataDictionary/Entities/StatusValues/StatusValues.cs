using MicroM.Core;
using MicroM.Data;
using MicroM.Web.Services;

namespace MicroM.DataDictionary;

public class StatusValuesDef : EntityDefinition
{
    public StatusValuesDef() : base("stv", nameof(StatusValues)) { }

    public readonly Column<string> c_status_id = Column<string>.PK();
    public readonly Column<string> c_statusvalue_id = Column<string>.PK();
    public readonly Column<string> vc_description = Column<string>.Text();
    public readonly Column<bool> bt_initial_value = new();

    public readonly ViewDefinition stv_brwStandard = new(nameof(c_status_id), nameof(c_statusvalue_id));

    public readonly EntityForeignKey<Status, StatusValues> FKStates = new();
    public readonly EntityUniqueConstraint UNDescription = new(keys: [nameof(c_status_id), nameof(vc_description)]);

}

public class StatusValues : Entity<StatusValuesDef>
{
    public StatusValues() : base() { }
    public StatusValues(IEntityClient ec, IMicroMEncryption? encryptor = null) : base(ec, encryptor) { }

}
