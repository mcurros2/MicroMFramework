using MicroM.Configuration;
using MicroM.Core;
using MicroM.Data;
using MicroM.Web.Services;

namespace MicroM.DataDictionary.Entities;

public class ApplicationAssemblyTypesDef : EntityDefinition
{
    public ApplicationAssemblyTypesDef() : base("apt", nameof(ApplicationAssemblyTypes), schemaName: DataDefaults.DataDictionarySchema) { Fake = true; }

    public readonly Column<string> c_application_id = Column<string>.PK();
    public readonly Column<string> c_assembly_id = Column<string>.PK();
    public readonly Column<int> i_order = new(column_flags: ColumnFlags.PK);
    public readonly Column<string> c_assemblytype_id = Column<string>.PK();

    public readonly ViewDefinition apt_brwStandard = new(nameof(c_application_id), nameof(c_assembly_id), nameof(i_order), nameof(c_assemblytype_id));

    public readonly APTGetCode APTGetCode = new();
}

public class ApplicationAssemblyTypes : Entity<ApplicationAssemblyTypesDef>
{
    public ApplicationAssemblyTypes() : base() { }
    public ApplicationAssemblyTypes(IEntityClient ec, IMicroMEncryption? encryptor = null) : base(ec, encryptor) { }


}
