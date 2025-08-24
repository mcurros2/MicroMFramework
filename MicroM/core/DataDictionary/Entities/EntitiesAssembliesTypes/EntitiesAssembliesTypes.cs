using MicroM.Core;
using MicroM.Data;
using MicroM.Web.Services;
using System.Data;

namespace MicroM.DataDictionary
{
    /// <summary>
    /// Entity definition for assembly type mappings.
    /// </summary>
    public class EntitiesAssembliesTypesDef : EntityDefinition
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="EntitiesAssembliesTypesDef"/> class.
        /// </summary>
        public EntitiesAssembliesTypesDef() : base("eat", nameof(EntitiesAssembliesTypes)) { SQLCreationOptions = SQLCreationOptionsMetadata.WithIUpdateAndIDrop; }

        /// <summary>
        /// Identifier of the assembly.
        /// </summary>
        public readonly Column<string> c_assembly_id = Column<string>.PK();
        /// <summary>
        /// Identifier of the assembly type.
        /// </summary>
        public readonly Column<string> c_assemblytype_id = Column<string>.PK(autonum: true);
        /// <summary>
        /// Name of the assembly type.
        /// </summary>
        public readonly Column<string> vc_assemblytypename = new(sql_type: SqlDbType.VarChar, size: 2048);

        /// <summary>
        /// Standard browse view for assembly types.
        /// </summary>
        public ViewDefinition eat_brwStandard { get; private set; } = new(nameof(c_assembly_id), nameof(c_assemblytype_id));

        /// <summary>
        /// Procedure that removes all types for an assembly.
        /// </summary>
        public ProcedureDefinition eat_deleteAllTypes { get; private set; } = new(nameof(c_assembly_id));

        /// <summary>
        /// Unique constraint for type names within an assembly.
        /// </summary>
        public readonly EntityUniqueConstraint UNTypes = new(keys: new[] { nameof(c_assembly_id), nameof(vc_assemblytypename) });

        /// <summary>
        /// Foreign key relationship to assemblies.
        /// </summary>
        public readonly EntityForeignKey<EntitiesAssemblies, EntitiesAssembliesTypes> FKApplications = new();

    }

    /// <summary>
    /// Represents the mapping between assemblies and their types.
    /// </summary>
    public class EntitiesAssembliesTypes : Entity<EntitiesAssembliesTypesDef>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="EntitiesAssembliesTypes"/> class.
        /// </summary>
        public EntitiesAssembliesTypes() : base() { }
        /// <summary>
        /// Initializes a new instance using the specified entity client and optional encryptor.
        /// </summary>
        public EntitiesAssembliesTypes(IEntityClient ec, IMicroMEncryption? encryptor = null) : base(ec, encryptor) { }

    }
}
