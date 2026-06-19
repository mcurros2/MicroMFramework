using MicroM.Configuration;
using MicroM.Core;
using MicroM.Data;
using MicroM.Extensions;
using MicroM.Generators.SQLGenerator;
using System.Reflection;
using static MicroM.Database.DatabaseManagement;
using static MicroM.Database.DatabaseSchemaProcedures;

namespace MicroM.Database;

public static class DatabaseSchema
{
    /// <summary>
    /// Creates the database schema for the specified entity type asynchronously in the provided <see cref="AppDBSchemaConfiguration.APPSchema"/> 
    /// </summary>
    public static async Task<T> CreateDBSchema<T>(
        IEntityClient ec,
        bool create_or_alter, bool create_if_not_exists, bool create_custom_procs, bool drop_and_recreate_indexes,
        bool create_procs,
        AppDBSchemaConfiguration schema_config,
        CancellationToken ct
        ) where T : EntityBase, new()
    {

        bool should_close = !(ec.ConnectionState == System.Data.ConnectionState.Open);
        T ent = new();
        ent.Init(null, null, schema_config.APPSchema);

        try
        {
            ent.Init(ec, schema_name: schema_config.APPSchema);

            if (ent.Def.Fake == false)
            {
                bool create = true;
                bool table_exists = await TableExists(ec, ent.Def.TableName, ent.Def.SchemaName ?? "dbo", ct);

                if (create_if_not_exists)
                {
                    create = !table_exists;
                }

                if (create)
                {
                    // If drop_and_recreate_indexes is true, we should only get the script with the primiary key and no indexes
                    // as we will drop and recreate them
                    await ec.ExecuteSQLNonQuery(ent.AsCreateTable(schema_config, table_and_primary_key_only: drop_and_recreate_indexes), ct);
                }

                if (table_exists || create)
                {
                    // Drop and recreate foreign keys, uniques, indexes
                    if (table_exists && drop_and_recreate_indexes)
                    {
                        await ec.ExecuteSQLNonQuery(ent.AsDropIndexes() ?? "", ct);
                        await ec.ExecuteSQLNonQuery(ent.AsDropForeignKeys() ?? "", ct);
                        await ec.ExecuteSQLNonQuery(ent.AsDropUniqueConstraints() ?? "", ct);

                        await ec.ExecuteSQLNonQuery(ent.AsAlterPrimaryKey() ?? "", ct);
                        await ec.ExecuteSQLNonQuery(ent.AsAlterUniqueConstraints() ?? "", ct);
                        await ec.ExecuteSQLNonQuery(ent.AsAlterForeignKeys(schema_config, with_drop: false) ?? "", ct);
                        await ec.ExecuteSQLNonQuery(ent.AsCreateIndexes() ?? "", ct);
                    }

                    if (create_procs)
                    {
                        await CreateProcs(ent, ec, create_or_alter, schema_config.DDSchema, ct, create_custom_procs);
                    }
                }

            }
            else
            {
                if (create_procs && create_custom_procs) await CreateCustomProcs<T>(ent, ec, ct, schema_config.APPSchema);
            }

        }
        finally
        {
            if (should_close) await ec.Disconnect();
        }

        return ent;
    }

    public async static Task CreateAllEntitiesProcs(IEntityClient ec, CustomOrderedDictionary<DatabaseSchemaCreationOptions<EntityBase>> entities, CustomOrderedDictionary<CustomScript>? classified_custom_scripts, string dd_schema, bool generate_standard_procs, CancellationToken ct, bool create_or_alter = true)
    {
        bool should_close = !(ec.ConnectionState == System.Data.ConnectionState.Open);
        try
        {
            await ec.Connect(ct);

            // Functions are created first if custom scripts exist
            if (classified_custom_scripts?.Count > 0)
            {
                var functions = classified_custom_scripts.Values.Where(x => x.ProcType == SQLScriptType.Function).ToList();
                HashSet<string> created_functions = new(StringComparer.OrdinalIgnoreCase);

                // in sql server functions will fail if they depend on inexistent functions
                int retries = 0;
                bool retry;
                Exception? last_exception = null;
                do
                {
                    retry = false;
                    int pass_created_functions = 0;
                    foreach (var func in functions)
                    {
                        if (func.ProcName != null && created_functions.Contains(func.ProcName))
                        {
                            continue;
                        }

                        try
                        {
                            await ec.ExecuteSQLNonQuery(func.SQLText, ct);
                            if (func.ProcName != null) created_functions.Add(func.ProcName);
                            pass_created_functions++;
                        }
                        catch (Exception ex)
                        {
                            last_exception = ex;
                            retry = true;
                        }
                    }

                    if (retry)
                    {
                        if (last_exception != null && (pass_created_functions == 0 || retries > 10))
                        {
                            throw last_exception;
                        }
                        retries++;
                    }

                } while (retry);
            }

            if (generate_standard_procs)
            {
                // Standard generated procs are created if no custom proc replacing it exists
                foreach (var options in entities.Values)
                {
                    await CreateGeneratedProcs(options.EntityInstance, ec, classified_custom_scripts, create_or_alter, dd_schema, ct);
                }
            }

            // the rest of the custom procs, in order, that correspond to the entities defined mnemonic code
            if (classified_custom_scripts?.Count > 0)
            {

                var idrop_scripts = classified_custom_scripts.Values.Where(x => x.ProcType == SQLScriptType.Procedure && x.StandardType == SQLProcStandardType.IDrop).ToList();
                foreach (var func in idrop_scripts)
                {
                    await ec.ExecuteSQLNonQuery(func.SQLText, ct);
                }

                var iupdate_scripts = classified_custom_scripts.Values.Where(x => x.ProcType == SQLScriptType.Procedure && x.StandardType == SQLProcStandardType.IUpdate).ToList();
                foreach (var func in iupdate_scripts)
                {
                    await ec.ExecuteSQLNonQuery(func.SQLText, ct);
                }

                var standard_replacements = classified_custom_scripts.Values.Where(x => x.ProcType == SQLScriptType.Procedure &&
                x.StandardType.IsIn(SQLProcStandardType.Update, SQLProcStandardType.Drop, SQLProcStandardType.Get, SQLProcStandardType.Lookup, SQLProcStandardType.StandardView)).ToList();
                foreach (var func in standard_replacements)
                {
                    await ec.ExecuteSQLNonQuery(func.SQLText, ct);
                }

                var custom_scripts = classified_custom_scripts.Values.Where(x => x.ProcType == SQLScriptType.Procedure && x.StandardType == SQLProcStandardType.Unknown).ToList();
                foreach (var func in custom_scripts)
                {
                    await ec.ExecuteSQLNonQuery(func.SQLText, ct);
                }

            }
        }
        finally
        {
            if (should_close) await ec.Disconnect();
        }
    }

    public async static Task CreateEntitiesDatabaseSchemaAndDictionary(IEntityClient ec, CustomOrderedDictionary<DatabaseSchemaCreationOptions<EntityBase>> entities, AppDBSchemaConfiguration schema_config, CancellationToken ct, bool create_or_alter = false)
    {
        bool should_close = !(ec.ConnectionState == System.Data.ConnectionState.Open);

        CustomOrderedDictionary<CustomScript>? custom_procs = null;
        try
        {
            await ec.Connect(ct);

            if (entities.Count == 0)
            {
                throw new InvalidOperationException("No entities to create.");
            }

            await entities.CreateSchemaAndProcs(ec, schema_config, ct, create_or_alter);

            Assembly asm = entities[0]!.EntityType.Assembly;

            await asm.CreateAllCategories(ec, ct, schema_config.DDSchema);
            await asm.CreateAllStatus(ec, ct, schema_config.DDSchema);

            // MMC: add to data dictionary
            await entities.AddEntitiesToDataDictionary(ec, ct, schema_config.DDSchema);

        }
        finally
        {
            custom_procs?.Clear();
            if (should_close) await ec.Disconnect();
        }
    }


}
