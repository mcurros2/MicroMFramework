using MicroM.Core;
using MicroM.Data;
using MicroM.Web.Services;

namespace MicroM.DataDictionary;

public class StatusDef : EntityDefinition
{
    public StatusDef() : base("sta", nameof(Status)) { }

    public readonly Column<string> c_status_id = Column<string>.PK();
    public readonly Column<string> vc_description = Column<string>.Text();

    public readonly ViewDefinition sta_brwStandard = new(nameof(c_status_id));

}

public class Status : Entity<StatusDef>
{
    public Status() : base() { }

    public Status(IEntityClient ec, IMicroMEncryption? encryptor = null) : base(ec, encryptor) { }

}
