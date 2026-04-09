using MicroM.Configuration;
using MicroM.Core;
using MicroM.Data;
using MicroM.Web.Services;

namespace MicroM.DataDictionary.Entities;

public class EntitiesAssembliesTypesDef : EntityDefinition
{
    public EntitiesAssembliesTypesDef() : base("eat", nameof(EntitiesAssembliesTypes), schemaName: DataDefaults.DataDictionarySchema) { SQLCreationOptions = SQLCreationOptionsMetadata.WithIUpdateAndIDrop; }

    public readonly Column<string> c_assembly_id = Column<string>.PK();
    public readonly Column<string> c_assemblytype_id = Column<string>.PK(autonum: true);
    public readonly Column<string> vc_assemblytypename = Column<string>.Text(size: 2048);

    public readonly ViewDefinition eat_brwStandard = new(nameof(c_assembly_id), nameof(c_assemblytype_id));

    public readonly ProcedureDefinition eat_deleteAllTypes = new(nameof(c_assembly_id));

    public readonly EntityUniqueConstraint UNTypes = new(keys: [nameof(c_assembly_id), nameof(vc_assemblytypename)]);

    public readonly EntityForeignKey<EntitiesAssemblies, EntitiesAssembliesTypes> FKApplications = new();

}

public class EntitiesAssembliesTypes : Entity<EntitiesAssembliesTypesDef>
{
    public EntitiesAssembliesTypes() : base() { }
    public EntitiesAssembliesTypes(IEntityClient ec, IMicroMEncryption? encryptor = null) : base(ec, encryptor) { }

}
