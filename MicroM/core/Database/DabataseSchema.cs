using MicroM.Core;
using MicroM.Data;
using MicroM.Extensions;
using MicroM.Generators.SQLGenerator;
using System.Reflection;
using static MicroM.Database.DatabaseManagement;
using static MicroM.Database.DatabaseSchemaProcedures;
using static MicroM.Database.DatabaseSchemaTables;

namespace MicroM.Database;

/// <summary>
/// Utilities to create schemas, tables, and procedures for entity models.
/// </summary>
public static class DatabaseSchema
{

    /// <summary>
    /// Creates the schema, tables and procedures for an entity.
    /// </summary>
    /// <typeparam name="T">Entity type.</typeparam>
    /// <param name="ec">Entity client.</param>
    /// <param name="create_or_alter">Whether to create or alter existing objects.</param>
    /// <param name="create_if_not_exists">Only create if the table does not exist.</param>
    /// <param name="create_custom_procs">Create custom procedures.</param>
    /// <param name="drop_and_recreate_indexes">Drop and recreate indexes.</param>
    /// <param name="create_procs">Create generated procedures.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The initialized entity instance.</returns>
    public static async Task<T> CreateSchema<T>(
        IEntityClient ec,
        bool create_or_alter, bool create_if_not_exists, bool create_custom_procs, bool drop_and_recreate_indexes,
        bool create_procs,
        CancellationToken ct
        ) where T : EntityBase, new()
    {

        bool should_close = !(ec.ConnectionState == System.Data.ConnectionState.Open);
        T ent = new();

        try
        {
            ent.Init(ec);

            if (ent.Def.Fake == false)
            {
                bool create = true;
                bool table_exists = await TableExists(ec, ent.Def.TableName, "dbo", ct);

                if (create_if_not_exists)
                {
                    create = !table_exists;
                }

                if (create)
                {
                    // If drop_and_recreate_indexes is true, we should only get the script with the primiary key and no indexes
                    // as we will drop and recreate them
                    await ec.ExecuteSQLNonQuery(ent.AsCreateTable(table_and_primary_key_only: drop_and_recreate_indexes), ct);
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
                        await ec.ExecuteSQLNonQuery(ent.AsAlterForeignKeys(with_drop: false) ?? "", ct);
                        await ec.ExecuteSQLNonQuery(ent.AsCreateIndexes() ?? "", ct);
                    }

                    if (create_procs)
                    {
                        await CreateProcs(ent, ec, ct, create_or_alter, create_custom_procs);
                    }
                }

            }
            else
            {
                if (create_procs && create_custom_procs) await CreateCustomProcs<T>(ent, ec, ct);
            }

        }
        finally
        {
            if (should_close) await ec.Disconnect();
        }

        return ent;
    }

    /// <summary>
    /// Creates stored procedures for all provided entities and custom scripts.
    /// </summary>
    /// <param name="ec">Entity client.</param>
    /// <param name="entities">Entities for which to create procedures.</param>
    /// <param name="classified_custom_scripts">Classified custom SQL scripts.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <param name="create_or_alter">Create or alter existing procedures.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public async static Task CreateAllEntitiesProcs(IEntityClient ec, CustomOrderedDictionary<DatabaseSchemaCreationOptions<EntityBase>> entities, CustomOrderedDictionary<CustomScript>? classified_custom_scripts, CancellationToken ct, bool create_or_alter = true)
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

            // Standard generated procs are created if no custom proc replacing it exists
            foreach (var options in entities.Values)
            {
                await CreateGeneratedProcs(options.EntityInstance, ec, ct, classified_custom_scripts, create_or_alter);
            }

            // the rest of the custom procs, in order
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

    /// <summary>
    /// Creates tables, constraints and procedures for entities and updates the data dictionary.
    /// </summary>
    /// <param name="ec">Entity client.</param>
    /// <param name="entities">Entities to process.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <param name="create_or_alter">Create or alter existing objects.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public async static Task CreateEntitiesDatabaseSchemaAndDictionary(IEntityClient ec, CustomOrderedDictionary<DatabaseSchemaCreationOptions<EntityBase>> entities, CancellationToken ct, bool create_or_alter = false)
    {
        bool should_close = !(ec.ConnectionState == System.Data.ConnectionState.Open);

        CustomOrderedDictionary<CustomScript>? custom_procs = null;
        CustomOrderedDictionary<DatabaseSchemaCreationOptions<EntityBase>>? created_tables = null;
        try
        {
            await ec.Connect(ct);

            if (entities.Count == 0)
            {
                throw new InvalidOperationException("No entities to create.");
            }

            Assembly asm = entities[0]!.EntityType.Assembly;
            custom_procs = await asm.GetAllClassifiedCustomProcs(ct);

            // Create types and sequences
            if (custom_procs?.Count > 0) await CreateAllCustomSQLTypes(ec, custom_procs, ct);

            // Tables and constraints
            created_tables = await CreateEntitiesInexistentTables(ec, entities, ct);
            await CreateEntitiesConstraintsAndIndexes(ec, created_tables, ct);

            // create custom tables is any
            if (custom_procs?.Count > 0)
            {
                await CreateAllCustomTables(ec, custom_procs, ct);
                await CreateAllCustomViews(ec, custom_procs, ct);
            }

            await CreateAllEntitiesProcs(ec, entities, custom_procs, ct, create_or_alter);

            await asm.CreateAllCategories(ec, ct);
            await asm.CreateAllStatus(ec, ct);


            // MMC: add to data dictionary
            await DataDictionarySchema.AddEntitiesToDataDictionary(ec, entities, ct);

        }
        finally
        {
            if (custom_procs?.Count > 0) custom_procs.Clear();
            if (created_tables?.Count > 0) created_tables.Clear();
            if (should_close) await ec.Disconnect();
        }
    }


}
