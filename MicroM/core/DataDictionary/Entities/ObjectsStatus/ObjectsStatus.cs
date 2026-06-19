using MicroM.Core;
using MicroM.Data;
using MicroM.Web.Services;

namespace MicroM.DataDictionary.Entities;

public class ObjectsStatusDef : EntityDefinition
{
    public ObjectsStatusDef() : base("ost", nameof(ObjectsStatus)) { }

    public readonly Column<string> c_object_id = Column<string>.PK();
    public readonly Column<string> c_status_id = Column<string>.PK();

    public readonly ViewDefinition ost_brwStandard = new(nameof(c_object_id), nameof(c_status_id));

    public readonly EntityForeignKey<Objects, ObjectsStatus> FKObjects = new();
    public readonly EntityForeignKey<Status, ObjectsStatus> FKStates = new();

}

public class ObjectsStatus : Entity<ObjectsStatusDef>
{
    public ObjectsStatus() : base() { }
    public ObjectsStatus(string? schema_name) : base(schema_name) { }
    public ObjectsStatus(IEntityClient ec, IMicroMEncryption? encryptor = null, string? schema_name = null) : base(ec, encryptor, schema_name) { }

}
