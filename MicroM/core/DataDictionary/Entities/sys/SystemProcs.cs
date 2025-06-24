using MicroM.Core;
using MicroM.Data;
using MicroM.Web.Services;

namespace MicroM.DataDictionary;

public class SystemProcsDef : EntityDefinition
{
    public SystemProcsDef() : base("sys", nameof(Status), false) { this.Fake = true; }

    public readonly Column<string> c_system_id = Column<string>.PK(fake: true);

    public readonly ProcedureDefinition sys_GetTimeZoneOffset = new(readonly_locks: true);
}

public class SystemProcs : Entity<SystemProcsDef>
{
    public SystemProcs() : base() { }

    public SystemProcs(IEntityClient ec, IMicroMEncryption? encryptor = null) : base(ec, encryptor) { }

}
