using MicroM.Core;
using MicroM.Data;
using MicroM.Web.Services;

namespace MicroM.DataDictionary
{
    /// <summary>
    /// Schema definition for application assembly types.
    /// </summary>
    public class ApplicationAssemblyTypesDef : EntityDefinition
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ApplicationAssemblyTypesDef"/> class.
        /// </summary>
        public ApplicationAssemblyTypesDef() : base("apt", nameof(ApplicationAssemblyTypes)) { Fake = true; }

        /// <summary>
        /// Application identifier.
        /// </summary>
        public readonly Column<string> c_application_id = Column<string>.PK();

        /// <summary>
        /// Assembly identifier.
        /// </summary>
        public readonly Column<string> c_assembly_id = Column<string>.PK();

        /// <summary>
        /// Display order of the assembly type.
        /// </summary>
        public readonly Column<int> i_order = new(column_flags: ColumnFlags.PK);

        /// <summary>
        /// Assembly type identifier.
        /// </summary>
        public readonly Column<string> c_assemblytype_id = Column<string>.PK();

        /// <summary>
        /// Default view for browsing application assembly types.
        /// </summary>
        public ViewDefinition apt_brwStandard { get; private set; } = new(nameof(c_application_id), nameof(c_assembly_id), nameof(i_order), nameof(c_assemblytype_id));

        /// <summary>
        /// Procedure helper for retrieving assembly type code.
        /// </summary>
        public APTGetCode APTGetCode { get; private set; } = new();
    }

    /// <summary>
    /// Entity for managing application assembly types.
    /// </summary>
    public class ApplicationAssemblyTypes : Entity<ApplicationAssemblyTypesDef>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ApplicationAssemblyTypes"/> class.
        /// </summary>
        public ApplicationAssemblyTypes() : base() { }

        /// <summary>
        /// Initializes a new instance of the <see cref="ApplicationAssemblyTypes"/> class with the specified client and encryption provider.
        /// </summary>
        /// <param name="ec">Entity client used for data access.</param>
        /// <param name="encryptor">Optional encryption provider.</param>
        public ApplicationAssemblyTypes(IEntityClient ec, IMicroMEncryption? encryptor = null) : base(ec, encryptor) { }


    }
}
