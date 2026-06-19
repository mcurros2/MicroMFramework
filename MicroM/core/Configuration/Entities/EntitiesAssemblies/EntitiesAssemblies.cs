using MicroM.Core;
using MicroM.Data;
using MicroM.Web.Services;

namespace MicroM.Configuration.Entities;

public class EntitiesAssembliesDef : EntityDefinition
{
    public EntitiesAssembliesDef() : base("eas", nameof(EntitiesAssemblies)) { SQLCreationOptions = SQLCreationOptionsMetadata.WithIUpdateAndIDrop; }

    public readonly Column<string> c_assembly_id = Column<string>.PK(autonum: true);
    public readonly Column<string> vc_assemblypath = Column<string>.Text(size: 2048);

    public readonly ViewDefinition eas_brwStandard = new(nameof(c_assembly_id));

    public readonly EntityUniqueConstraint UNAssemblies = new(keys: nameof(vc_assemblypath));

    public readonly ProcedureDefinition eas_dropUnusedAssemblies = new();

}

public class EntitiesAssemblies : Entity<EntitiesAssembliesDef>
{
    public EntitiesAssemblies() : base() { }

    public EntitiesAssemblies(string? schema_name) : base(schema_name) { }

    public EntitiesAssemblies(IEntityClient ec, IMicroMEncryption? encryptor = null, string? schema_name = null) : base(ec, encryptor, schema_name) { }

}
