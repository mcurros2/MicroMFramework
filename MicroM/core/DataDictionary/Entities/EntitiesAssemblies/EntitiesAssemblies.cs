using MicroM.Core;
using MicroM.Data;
using MicroM.Web.Services;
using System.Data;

namespace MicroM.DataDictionary
{
    /// <summary>
    /// Entity definition for assembly records.
    /// </summary>
    public class EntitiesAssembliesDef : EntityDefinition
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="EntitiesAssembliesDef"/> class.
        /// </summary>
        public EntitiesAssembliesDef() : base("eas", nameof(EntitiesAssemblies)) { SQLCreationOptions = SQLCreationOptionsMetadata.WithIUpdateAndIDrop; }

        /// <summary>
        /// Identifier of the assembly.
        /// </summary>
        public readonly Column<string> c_assembly_id = Column<string>.PK(autonum: true);
        /// <summary>
        /// Path to the assembly file.
        /// </summary>
        public readonly Column<string> vc_assemblypath = new(sql_type: SqlDbType.VarChar, size: 2048);

        /// <summary>
        /// Standard browse view for assemblies.
        /// </summary>
        public ViewDefinition eas_brwStandard { get; private set; } = new(nameof(c_assembly_id));

        /// <summary>
        /// Unique constraint enforcing unique assembly paths.
        /// </summary>
        public readonly EntityUniqueConstraint UNAssemblies = new(keys: nameof(vc_assemblypath));

        /// <summary>
        /// Procedure that removes assemblies no longer in use.
        /// </summary>
        public ProcedureDefinition eas_dropUnusedAssemblies { get; private set; } = new();

    }

    /// <summary>
    /// Runtime entity representing an assembly record.
    /// </summary>
    public class EntitiesAssemblies : Entity<EntitiesAssembliesDef>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="EntitiesAssemblies"/> class.
        /// </summary>
        public EntitiesAssemblies() : base() { }

        /// <summary>
        /// Initializes a new instance using the specified entity client and optional encryptor.
        /// </summary>
        /// <param name="ec">Entity client used for data access.</param>
        /// <param name="encryptor">Optional encryptor for sensitive fields.</param>
        public EntitiesAssemblies(IEntityClient ec, IMicroMEncryption? encryptor = null) : base(ec, encryptor) { }

    }
}
