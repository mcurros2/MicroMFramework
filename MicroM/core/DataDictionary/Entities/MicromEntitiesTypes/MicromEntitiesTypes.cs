using MicroM.Core;
using MicroM.Data;
using MicroM.Database;
using MicroM.Web.Services;

namespace MicroM.DataDictionary.Entities;

public class MicromEntitiesTypesDef : EntityDefinition
{
    public MicromEntitiesTypesDef() : base("mty", nameof(MicromEntitiesTypes)) { }

    public readonly Column<string> vc_entity_name = Column<string>.Text(column_flags: ColumnFlags.PK | ColumnFlags.Insert | ColumnFlags.Update | ColumnFlags.Delete | ColumnFlags.Get);
    public readonly Column<string> vc_entity_type = Column<string>.Text();

    public readonly ViewDefinition mty_brwStandard = new(nameof(vc_entity_name));

}

public class MicromEntitiesTypes : Entity<MicromEntitiesTypesDef>
{
    public MicromEntitiesTypes() : base() { }
    public MicromEntitiesTypes(string? schema_name) : base(schema_name) { }
    public MicromEntitiesTypes(IEntityClient ec, IMicroMEncryption? encryptor = null, string? schema_name = null) : base(ec, encryptor, schema_name) { }
    public static async Task FillEntitiesTypes(IEntityClient ec, string app_schema, CustomOrderedDictionary<DatabaseSchemaCreationOptions<EntityBase>> entities, CancellationToken ct)
    {

        try
        {
            await ec.Connect(ct);

            var etyp = new MicromEntitiesTypes(ec, null, app_schema);

            await ec.ExecuteSQL($"DELETE {etyp.Def.FullTableName}", ct);

            foreach (var entry in entities)
            {
                etyp.Def.vc_entity_name.Value = entry.EntityType.Name;
                etyp.Def.vc_entity_type.Value = entry.EntityType.FullName ?? "";

                await etyp.InsertData(ct, throw_dbstat_exception: true);
            }
        }
        finally
        {
            await ec.Disconnect();
        }
    }
}
