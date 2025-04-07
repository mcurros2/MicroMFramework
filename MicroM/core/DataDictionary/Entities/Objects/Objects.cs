using MicroM.Core;
using MicroM.Data;
using MicroM.Web.Services;
using System.Data;

namespace MicroM.DataDictionary
{
    /*
     * MMC: The idea here is to persist only entities that need to
     * we should have a way to specify an entity as "dynamic" which means to gather
     * it's definition from information schema views and procedures by their mnemonic
     * 
     * or
     * 
     * generate the entites as "reverse engineering" with a specific method
     * 
     * fake entities can´t be reverse engineered and would need to be persisted if needed at the backend
     * fake entities can live within the client space.
     */

    public class ObjectsDef : EntityDefinition
    {
        public ObjectsDef() : base("obj", nameof(Objects)) { }

        public readonly Column<string> c_object_id = Column<string>.PK();
        public readonly Column<string> c_mneo_id = Column<string>.FK();
        public readonly Column<string> vc_tablename = new(sql_type: SqlDbType.VarChar, size: 255);

        public ViewDefinition obj_brwStandard { get; private set; } = new(nameof(c_object_id));

        public readonly EntityUniqueConstraint UNMnemonic = new(keys: nameof(c_mneo_id));

    }

    public class Objects : Entity<ObjectsDef>
    {
        public Objects() : base() { }

        public Objects(IEntityClient ec, IMicroMEncryption? encryptor = null) : base(ec, encryptor) { }

    }
}
