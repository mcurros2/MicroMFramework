using MicroM.Core;
using MicroM.Data;
using MicroM.Web.Services;

namespace MicroM.DataDictionary.Entities;

public class ProcsDef : EntityDefinition
{
    public ProcsDef() : base("prc", nameof(Procs)) { }

    public readonly Column<string> c_object_id = Column<string>.PK();
    public readonly Column<int> c_proc_id = Column<int>.PK(autonum: true);
    public readonly Column<string> vc_procname = Column<string>.Text();

    public readonly ViewDefinition prc_brwStandard = new(nameof(c_object_id), nameof(c_proc_id));

    public readonly EntityForeignKey<Objects, Procs> FKObjects = new();

    public readonly EntityUniqueConstraint UNProcName = new(keys: nameof(vc_procname));

}

public class Procs : Entity<ProcsDef>
{
    public Procs() : base() { }

    public Procs(IEntityClient ec, IMicroMEncryption? encryptor = null) : base(ec, encryptor) { }

}
