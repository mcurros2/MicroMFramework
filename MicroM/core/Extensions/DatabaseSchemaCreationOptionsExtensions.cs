using MicroM.Configuration;
using MicroM.Core;
using MicroM.Data;
using MicroM.Database;
using MicroM.DataDictionary.Entities;
using static MicroM.Database.DatabaseSchema;
using static MicroM.Database.DatabaseSchemaTables;


namespace MicroM.Extensions;

public static class DatabaseSchemaCreationOptionsExtensions
{

    public static void TryAddEntityType<T>(this CustomOrderedDictionary<DatabaseSchemaCreationOptions<EntityBase>> dict, IEntityClient ec, bool create_or_alter = true, string? schema_name = null) where T : EntityBase, new()
    {
        var ent = new T();
        ent.Init(ec, schema_name: schema_name);
        var type = typeof(T);
        dict.TryAdd(
            type.Name,
            new DatabaseSchemaCreationOptions<EntityBase>(
                ent,
                create_or_alter
            )
        );
    }

    public static void TryAddEntities(this CustomOrderedDictionary<DatabaseSchemaCreationOptions<EntityBase>> dict, bool create_or_alter = true, params EntityBase[] entities)
    {
        foreach (var entity in entities)
        {
            dict.TryAdd(
                entity.GetType().Name,
                new DatabaseSchemaCreationOptions<EntityBase>(
                    entity,
                    create_or_alter
                )
            );
        }
    }

    /// <summary>
    /// Creates or updates the database schema, tables, constraints, indexes, and associated stored procedures for the
    /// specified entities using the provided entity client.
    /// </summary>
    /// <remarks>This method ensures that all required database objects for the specified entities exist,
    /// including schemas, tables, types, sequences, constraints, indexes, and custom stored procedures. It will connect
    /// to the database if not already connected and disconnect upon completion if it established the connection. The
    /// method is safe to call multiple times; it will only create or alter objects as needed.
    /// Custom types and sequences, custom tables and custom views are created in one batch per step.</remarks>
    public async static Task CreateSchemaAndProcs(this CustomOrderedDictionary<DatabaseSchemaCreationOptions<EntityBase>> entities, IEntityClient ec, AppDBSchemaConfiguration schema_config,
        CancellationToken ct, bool create_or_alter = false, CustomOrderedDictionary<CustomScript>? custom_procs = null, bool generate_standard_procs = true,
        bool create_only_inexisting_constraints_and_indexes = true, Func<string, string?>? custom_sql_transform = null)
    {
        bool should_close = !(ec.ConnectionState == System.Data.ConnectionState.Open);

        if (entities == null || entities.Count == 0) return;

        string? schema_name = entities[0]?.EntityInstance.Def.SchemaName;

        CustomOrderedDictionary<DatabaseSchemaCreationOptions<EntityBase>>? created_tables = null;
        try
        {
            await ec.Connect(ct);

            // Entities without schema use the application configured schema
            var dd_types = DataDictionarySchema.GetCoreEntitiesTypes();
            foreach (var options in entities.Values)
            {
                var def = options.EntityInstance.Def;
                if (def.SchemaName == null) def.SchemaName = dd_types.ContainsKey(options.EntityType.Name) ? schema_config.DDSchema : schema_config.APPSchema;
            }

            if (custom_procs == null)
            {
                var custom_procs_assembly = (entities[0]?.EntityType.Assembly) ?? throw new InvalidOperationException("Unable to determine the assembly for custom procedures.");
                custom_procs = await custom_procs_assembly.GetAllClassifiedCustomSQLScripts(ct, schema_name: schema_name, custom_sql_transform: custom_sql_transform);
            }

            // Create schemas if not exist
            await CreateAllInexistingSchemas(ec, entities, ct);

            // Create types and sequences, one batch
            if (custom_procs?.Count > 0)
            {
                var types_and_sequences = custom_procs.Values.Where(x => x.ProcType == SQLScriptType.Type)
                    .Concat(custom_procs.Values.Where(x => x.ProcType == SQLScriptType.Sequence)).ToList();
                await ExecuteProcedureScripts(ec, types_and_sequences, ct);
            }

            created_tables = await CreateEntitiesInexistentTables(ec, entities, schema_config, ct);
            await created_tables.CreateEntitiesConstraintsAndIndexes(schema_config, ec, ct, create_only_inexisting_constraints_and_indexes);

            // create custom tables and views if any, one batch each
            if (custom_procs?.Count > 0)
            {
                await ExecuteProcedureScripts(ec, [.. custom_procs.Values.Where(x => x.ProcType == SQLScriptType.Table)], ct);
                await ExecuteProcedureScripts(ec, [.. custom_procs.Values.Where(x => x.ProcType == SQLScriptType.View)], ct);
            }

            await CreateAllEntitiesProcs(ec, entities, custom_procs, schema_config.DDSchema, generate_standard_procs, ct, create_or_alter);

        }
        finally
        {
            if (custom_procs?.Count > 0) custom_procs.Clear();
            if (created_tables?.Count > 0) created_tables.Clear();
            if (should_close) await ec.Disconnect();
        }

    }

    /// <summary>
    /// Sequential version of <see cref="CreateSchemaAndProcs"/>: custom types and sequences, custom tables and custom views are executed one script at a time.
    /// </summary>
    public async static Task CreateSchemaAndProcsSequential(this CustomOrderedDictionary<DatabaseSchemaCreationOptions<EntityBase>> entities, IEntityClient ec, AppDBSchemaConfiguration schema_config,
        CancellationToken ct, bool create_or_alter = false, CustomOrderedDictionary<CustomScript>? custom_procs = null, bool generate_standard_procs = true,
        bool create_only_inexisting_constraints_and_indexes = true, Func<string, string?>? custom_sql_transform = null)
    {
        bool should_close = !(ec.ConnectionState == System.Data.ConnectionState.Open);

        if (entities == null || entities.Count == 0) return;

        string? schema_name = entities[0]?.EntityInstance.Def.SchemaName;

        CustomOrderedDictionary<DatabaseSchemaCreationOptions<EntityBase>>? created_tables = null;
        try
        {
            await ec.Connect(ct);

            // Entities without schema use the application configured schema
            var dd_types = DataDictionarySchema.GetCoreEntitiesTypes();
            foreach (var options in entities.Values)
            {
                var def = options.EntityInstance.Def;
                if (def.SchemaName == null) def.SchemaName = dd_types.ContainsKey(options.EntityType.Name) ? schema_config.DDSchema : schema_config.APPSchema;
            }

            if (custom_procs == null)
            {
                var custom_procs_assembly = (entities[0]?.EntityType.Assembly) ?? throw new InvalidOperationException("Unable to determine the assembly for custom procedures.");
                custom_procs = await custom_procs_assembly.GetAllClassifiedCustomSQLScripts(ct, schema_name: schema_name, custom_sql_transform: custom_sql_transform);
            }

            // Create schemas if not exist
            await CreateAllInexistingSchemas(ec, entities, ct);

            // Create types and sequences
            if (custom_procs?.Count > 0) await CreateAllCustomSQLTypes(ec, custom_procs, ct);

            created_tables = await CreateEntitiesInexistentTables(ec, entities, schema_config, ct);
            await created_tables.CreateEntitiesConstraintsAndIndexes(schema_config, ec, ct, create_only_inexisting_constraints_and_indexes);

            // create custom tables if any
            if (custom_procs?.Count > 0)
            {
                await CreateAllCustomTables(ec, custom_procs, ct);
                await CreateAllCustomViews(ec, custom_procs, ct);
            }

            await CreateAllEntitiesProcs(ec, entities, custom_procs, schema_config.DDSchema, generate_standard_procs, ct, create_or_alter);

        }
        finally
        {
            if (custom_procs?.Count > 0) custom_procs.Clear();
            if (created_tables?.Count > 0) created_tables.Clear();
            if (should_close) await ec.Disconnect();
        }

    }

    public static async Task CreateEntitiesConstraintsAndIndexes(this CustomOrderedDictionary<DatabaseSchemaCreationOptions<EntityBase>> entities, AppDBSchemaConfiguration schema_config, IEntityClient ec, CancellationToken ct, bool create_only_inexisting_constraints_and_indexes = true)
    {
        await DatabaseSchemaTables.CreateEntitiesConstraintsAndIndexes(ec, entities, schema_config, ct, create_only_inexisting_constraints_and_indexes);
    }

    public static async Task AddEntitiesToDataDictionary(this CustomOrderedDictionary<DatabaseSchemaCreationOptions<EntityBase>> entities, IEntityClient ec, CancellationToken ct, string? dd_schema_name = null)
    {
        var unique_entities = entities.Values.Select(option => option.EntityInstance).DistinctBy(ent => ent.Def.Mneo).ToList();
        if (unique_entities.Count == 0) return;

        bool should_close = !(ec.ConnectionState == System.Data.ConnectionState.Open);
        try
        {
            await ec.Connect(ct);

            // One obj_InitializeObjects call per chunk creates objects, numbering, objects categories and objects status
            var obj = new Objects(ec, schema_name: dd_schema_name);
            obj.Def.webusr.Value = ec.WebUser ?? "";
            foreach (var chunk in unique_entities.Chunk(100))
            {
                ct.ThrowIfCancellationRequested();
                obj.Def.vc_json_objects.Value = System.Text.Json.JsonSerializer.Serialize(chunk.Select(ent => new
                {
                    object_id = ent.Def.Mneo,
                    table_name = ent.Def.FullTableName,
                    autonum = ent.Def.AutonumColumn != null,
                    categories = ent.Def.RelatedCategories,
                    status = ent.Def.RelatedStatus
                }));
                await obj.ExecuteProcessDBStatus(obj.Def.obj_InitializeObjects, ct, throw_dbstat_exception: true);
            }
        }
        finally
        {
            if (should_close) await ec.Disconnect();
        }
    }


}
