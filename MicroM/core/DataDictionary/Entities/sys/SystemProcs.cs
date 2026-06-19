using MicroM.Core;
using MicroM.Data;
using MicroM.Web.Services;

namespace MicroM.DataDictionary.Entities;

public class SystemProcsDef : EntityDefinition
{
    public SystemProcsDef() : base("sys", nameof(Status), add_default_columns: false) { this.Fake = true; }

    public readonly Column<string> c_system_id = Column<string>.PK(fake: true);

    public readonly ProcedureDefinition sys_GetTimeZoneOffset = new(readonly_locks: true);
}

public class SystemProcs : Entity<SystemProcsDef>
{
    public SystemProcs() : base() { }
    public SystemProcs(string? schema_name) : base(schema_name) { }
    public SystemProcs(IEntityClient ec, IMicroMEncryption? encryptor = null, string? schema_name = null) : base(ec, encryptor, schema_name) { }

}
