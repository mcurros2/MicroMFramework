using MicroM.Core;
using MicroM.Data;
using MicroM.Web.Services;
using System.Data;

namespace MicroM.DataDictionary
{
    public class EntitiesAssembliesDef : EntityDefinition
    {
        public EntitiesAssembliesDef() : base("eas", nameof(EntitiesAssemblies)) { SQLCreationOptions = SQLCreationOptionsMetadata.WithIUpdateAndIDrop; }

        public readonly Column<string> c_assembly_id = Column<string>.PK(autonum: true);
        public readonly Column<string> vc_assemblypath = new(sql_type: SqlDbType.VarChar, size: 2048);

        public ViewDefinition eas_brwStandard { get; private set; } = new(nameof(c_assembly_id));

        public readonly EntityUniqueConstraint UNAssemblies = new(keys: nameof(vc_assemblypath));

        public ProcedureDefinition eas_dropUnusedAssemblies { get; private set; } = new();

    }

    public class EntitiesAssemblies : Entity<EntitiesAssembliesDef>
    {
        public EntitiesAssemblies() : base() { }

        public EntitiesAssemblies(IEntityClient ec, IMicroMEncryption? encryptor = null) : base(ec, encryptor) { }

    }
}
