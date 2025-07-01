using MicroM.Core;
using MicroM.Data;
using MicroM.Web.Services;
using System.Data;

namespace MicroM.DataDictionary
{
    public class EntitiesAssembliesTypesDef : EntityDefinition
    {
        public EntitiesAssembliesTypesDef() : base("eat", nameof(EntitiesAssembliesTypes)) { SQLCreationOptions = SQLCreationOptionsMetadata.WithIUpdateAndIDrop; }

        public readonly Column<string> c_assembly_id = Column<string>.PK();
        public readonly Column<string> c_assemblytype_id = Column<string>.PK(autonum: true);
        public readonly Column<string> vc_assemblytypename = new(sql_type: SqlDbType.VarChar, size: 2048);

        public ViewDefinition eat_brwStandard { get; private set; } = new(nameof(c_assembly_id), nameof(c_assemblytype_id));

        public ProcedureDefinition eat_deleteAllTypes { get; private set; } = new(nameof(c_assembly_id));

        public readonly EntityUniqueConstraint UNTypes = new(keys: new[] { nameof(c_assembly_id), nameof(vc_assemblytypename) });

        public readonly EntityForeignKey<EntitiesAssemblies, EntitiesAssembliesTypes> FKApplications = new();

    }

    public class EntitiesAssembliesTypes : Entity<EntitiesAssembliesTypesDef>
    {
        public EntitiesAssembliesTypes() : base() { }
        public EntitiesAssembliesTypes(IEntityClient ec, IMicroMEncryption? encryptor = null) : base(ec, encryptor) { }

    }
}
