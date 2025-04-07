using MicroM.Core;
using MicroM.Data;
using MicroM.Web.Services;

namespace MicroM.DataDictionary
{
    public class ObjectsStatusDef : EntityDefinition
    {
        public ObjectsStatusDef() : base("ost", nameof(ObjectsStatus)) { }

        public readonly Column<string> c_object_id = Column<string>.PK();
        public readonly Column<string> c_status_id = Column<string>.PK();

        public ViewDefinition ost_brwStandard { get; private set; } = new(nameof(c_object_id), nameof(c_status_id));

        public readonly EntityForeignKey<Objects, ObjectsStatus> FKObjects = new();
        public readonly EntityForeignKey<Status, ObjectsStatus> FKStates = new();

    }

    public class ObjectsStatus : Entity<ObjectsStatusDef>
    {
        public ObjectsStatus() : base() { }
        public ObjectsStatus(IEntityClient ec, IMicroMEncryption? encryptor = null) : base(ec, encryptor) { }

    }

}
