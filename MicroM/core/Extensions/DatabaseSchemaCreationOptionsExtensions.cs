using MicroM.Configuration;
using MicroM.Core;
using MicroM.Data;
using MicroM.Database;
using MicroM.Generators.SQLGenerator;
using System.Text;
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
    /// method is safe to call multiple times; it will only create or alter objects as needed.</remarks>
    public async static Task CreateSchemaAndProcs(this CustomOrderedDictionary<DatabaseSchemaCreationOptions<EntityBase>> entities, IEntityClient ec, AppDBSchemaConfiguration schema_config, CancellationToken ct, bool create_or_alter = false, CustomOrderedDictionary<CustomScript>? custom_procs = null)
    {
        bool should_close = !(ec.ConnectionState == System.Data.ConnectionState.Open);

        if (entities == null || entities.Count == 0) return;

        string? schema_name = entities[0]?.EntityInstance.Def.SchemaName;

        CustomOrderedDictionary<DatabaseSchemaCreationOptions<EntityBase>>? created_tables = null;
        try
        {
            await ec.Connect(ct);

            if (custom_procs == null)
            {
                var custom_procs_assembly = (entities[0]?.EntityType.Assembly) ?? throw new InvalidOperationException("Unable to determine the assembly for custom procedures.");
                custom_procs = await custom_procs_assembly.GetAllClassifiedCustomSQLScripts(ct, schema_name: schema_name);
            }

            // Create schemas if not exist
            await CreateAllInexistingSchemas(ec, entities, ct);

            // Create types and sequences
            if (custom_procs?.Count > 0) await CreateAllCustomSQLTypes(ec, custom_procs, ct);

            created_tables = await CreateEntitiesInexistentTables(ec, entities, schema_config, ct);
            await created_tables.CreateEntitiesConstraintsAndIndexes(schema_config, ec, ct);

            // create custom tables if any
            if (custom_procs?.Count > 0)
            {
                await CreateAllCustomTables(ec, custom_procs, ct);
                await CreateAllCustomViews(ec, custom_procs, ct);
            }

            await CreateAllEntitiesProcs(ec, entities, custom_procs, schema_config.DDSchema, ct, create_or_alter);

        }
        finally
        {
            if (custom_procs?.Count > 0) custom_procs.Clear();
            if (created_tables?.Count > 0) created_tables.Clear();
            if (should_close) await ec.Disconnect();
        }

    }

    public static async Task CreateEntitiesConstraintsAndIndexes(this CustomOrderedDictionary<DatabaseSchemaCreationOptions<EntityBase>> entities, AppDBSchemaConfiguration schema_config, IEntityClient ec, CancellationToken ct)
    {
        bool should_close = !(ec.ConnectionState == System.Data.ConnectionState.Open);
        try
        {
            await ec.Connect(ct);

            StringBuilder sb_create_PKs = new();
            StringBuilder sb_create_UNs = new();
            StringBuilder sb_create_FKs = new();
            StringBuilder sb_create_IDXs = new();
            foreach (var options in entities.Values)
            {
                sb_create_PKs.Append(options.EntityInstance.AsAlterPrimaryKey());
                sb_create_UNs.Append(options.EntityInstance.AsAlterUniqueConstraints());
                sb_create_FKs.Append(options.EntityInstance.AsAlterForeignKeys(schema_config));
                sb_create_IDXs.Append(options.EntityInstance.AsCreateIndexes());
            }
            // create constraints and indexes
            await ec.ExecuteSQLNonQuery(sb_create_PKs.ToString(), ct);
            await ec.ExecuteSQLNonQuery(sb_create_UNs.ToString(), ct);
            await ec.ExecuteSQLNonQuery(sb_create_FKs.ToString(), ct);
            await ec.ExecuteSQLNonQuery(sb_create_IDXs.ToString(), ct);
        }
        finally
        {
            if (should_close) await ec.Disconnect();
        }
    }

    public static async Task AddEntitiesToDataDictionary(this CustomOrderedDictionary<DatabaseSchemaCreationOptions<EntityBase>> entities, IEntityClient ec, CancellationToken ct, string? dd_schema_name = null)
    {
        bool should_close = !(ec.ConnectionState == System.Data.ConnectionState.Open);
        try
        {
            await ec.Connect(ct);
            foreach (var option in entities.Values)
            {
                await option.EntityInstance.AddInstanceToDataDictionary(ct, dd_schema_name);
            }
        }
        finally
        {
            if (should_close) await ec.Disconnect();
        }
    }


}
