using MicroM.Core;
using MicroM.Data;
using MicroM.DataDictionary.Entities;
using MicroM.Web.Services;

namespace MicroM.Configuration.Entities;

public class ApplicationsAssembliesDef : EntityDefinition
{
    public ApplicationsAssembliesDef() : base("apa", nameof(ApplicationsAssemblies), schemaName: DataDefaults.DataDictionarySchema) { SQLCreationOptions = SQLCreationOptionsMetadata.WithIUpdateAndIDrop; }

    public readonly Column<string> c_application_id = Column<string>.PK();
    public readonly Column<string> c_assembly_id = Column<string>.PK();
    public readonly Column<int> i_order = new();

    public readonly ViewDefinition apa_brwStandard = new(nameof(c_application_id), nameof(c_assembly_id));

    public readonly ProcedureDefinition apa_GetAssemblies = new();

    public readonly EntityUniqueConstraint UNAssembliesOrder = new(keys: [nameof(c_application_id), nameof(i_order)]);

    public readonly EntityForeignKey<Applications, ApplicationsAssemblies> FKApplications = new();
    public readonly EntityForeignKey<EntitiesAssemblies, ApplicationsAssemblies> FKApplicationsAssemblies = new();
}

public class ApplicationsAssemblies : Entity<ApplicationsAssembliesDef>
{
    public ApplicationsAssemblies() : base() { }
    public ApplicationsAssemblies(IEntityClient ec, IMicroMEncryption? encryptor = null) : base(ec, encryptor) { }


}
