using MicroM.Core;
using MicroM.Data;
using MicroM.Database;
using MicroM.Web.Services;
using System.Text.Json;

namespace MicroM.DataDictionary.Entities;

public class MicromEntitiesTypesDef : EntityDefinition
{
    public MicromEntitiesTypesDef() : base("mty", nameof(MicromEntitiesTypes)) { }

    public readonly Column<string> vc_entity_name = Column<string>.Text(column_flags: ColumnFlags.PK | ColumnFlags.Insert | ColumnFlags.Update | ColumnFlags.Delete | ColumnFlags.Get);
    public readonly Column<string> vc_entity_type = Column<string>.Text();
    public readonly Column<string> vc_json_entities = Column<string>.Text(size: 0, fake: true, column_flags: ColumnFlags.None);

    public readonly ViewDefinition mty_brwStandard = new(nameof(vc_entity_name));

    public readonly ProcedureDefinition mty_deleteAll = new();
    public readonly ProcedureDefinition mty_GetEntitiesTypes = new();
    public readonly ProcedureDefinition mty_InsertChunk = new(nameof(vc_json_entities), nameof(webusr));

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

            var current = await etyp.ExecuteProc(etyp.Def.mty_GetEntitiesTypes, ct);
            var expected = entities.Values.Select(e => (e.EntityType.Name, e.EntityType.FullName ?? "")).ToHashSet();
            var actual = current.SelectMany(r => r.records).Select(r => (r[0]?.ToString() ?? "", r[1]?.ToString() ?? "")).ToHashSet();
            if (!expected.SetEquals(actual))
            {
                await etyp.ExecuteProcessDBStatus(etyp.Def.mty_deleteAll, ct, throw_dbstat_exception: true);
                etyp.Def.webusr.Value = ec.WebUser ?? "";
                foreach (var chunk in expected.Chunk(200))
                {
                    ct.ThrowIfCancellationRequested();
                    etyp.Def.vc_json_entities.Value = JsonSerializer.Serialize(chunk.Select(e => new
                    {
                        vc_entity_name = e.Name,
                        vc_entity_type = e.Item2
                    }));
                    await etyp.ExecuteProcessDBStatus(etyp.Def.mty_InsertChunk, ct, throw_dbstat_exception: true);
                }
            }
        }
        finally
        {
            await ec.Disconnect();
        }
    }
}
