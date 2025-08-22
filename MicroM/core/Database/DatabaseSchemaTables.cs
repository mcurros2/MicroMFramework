using MicroM.Core;
using MicroM.Data;
using MicroM.Generators.SQLGenerator;
using System.Text;
using static MicroM.Database.DatabaseManagement;

namespace MicroM.Database
{
    /// <summary>
    /// Helpers for creating tables, types and constraints for entity schemas.
    /// </summary>
    public static class DatabaseSchemaTables
    {
        /// <summary>
        /// Returns the names of tables that do not exist for the given entities.
        /// </summary>
        /// <param name="ec">Entity client.</param>
        /// <param name="entities">Entities to check.</param>
        /// <param name="ct">Cancellation token.</param>
        /// <returns>Set of table names that are missing.</returns>
        public static async Task<HashSet<string>> GetEntitiesInexistingTables(IEntityClient ec, CustomOrderedDictionary<DatabaseSchemaCreationOptions<EntityBase>> entities, CancellationToken ct)
        {
            bool should_close = !(ec.ConnectionState == System.Data.ConnectionState.Open);
            HashSet<string> inexisting_tables = new(StringComparer.OrdinalIgnoreCase);
            try
            {
                await ec.Connect(ct);
                foreach (var options in entities.Values)
                {
                    bool table_exists = await TableExists(ec, options.EntityInstance.Def.TableName, "dbo", ct);
                    if (!table_exists) inexisting_tables.Add(options.EntityInstance.Def.TableName);
                }
            }
            finally
            {
                if (should_close) await ec.Disconnect();
            }
            return inexisting_tables;
        }

        /// <summary>
        /// Creates tables for entities that are missing in the database.
        /// </summary>
        /// <param name="ec">Entity client.</param>
        /// <param name="entities">Entities to process.</param>
        /// <param name="ct">Cancellation token.</param>
        /// <returns>Dictionary of entities whose tables were created.</returns>
        public static async Task<CustomOrderedDictionary<DatabaseSchemaCreationOptions<EntityBase>>> CreateEntitiesInexistentTables(IEntityClient ec, CustomOrderedDictionary<DatabaseSchemaCreationOptions<EntityBase>> entities, CancellationToken ct)
        {
            bool should_close = !(ec.ConnectionState == System.Data.ConnectionState.Open);
            CustomOrderedDictionary<DatabaseSchemaCreationOptions<EntityBase>> created_tables = new();
            HashSet<string>? inexisting_tables = null;
            try
            {
                await ec.Connect(ct);

                inexisting_tables = await GetEntitiesInexistingTables(ec, entities, ct);

                StringBuilder sb_create_tables = new();
                foreach (var options in entities.Values)
                {
                    // if the table does not exist, create it
                    if (inexisting_tables.Contains(options.EntityInstance.Def.TableName))
                    {
                        var scripts = options.EntityInstance.AsCreateTable(table_and_primary_key_only: true);
                        if (scripts?.Count > 0)
                        {
                            sb_create_tables.Append(scripts[0]);
                            created_tables.Add(options.EntityType.Name, options);
                        }
                    }
                }
                // create tables
                if (sb_create_tables.Length > 0)
                {
                    await ec.ExecuteSQLNonQuery(sb_create_tables.ToString(), ct);
                }

                return created_tables;
            }
            finally
            {
                inexisting_tables?.Clear();
                if (should_close) await ec.Disconnect();
            }
        }

        /// <summary>
        /// Creates custom SQL types and sequences contained in the provided scripts.
        /// </summary>
        /// <param name="ec">Entity client.</param>
        /// <param name="classified_custom_procs">Classified custom scripts.</param>
        /// <param name="ct">Cancellation token.</param>
        /// <param name="create_or_alter">Indicates if existing objects should be altered.</param>
        public async static Task CreateAllCustomSQLTypes(IEntityClient ec, CustomOrderedDictionary<CustomScript>? classified_custom_procs, CancellationToken ct, bool create_or_alter = true)
        {
            bool should_close = !(ec.ConnectionState == System.Data.ConnectionState.Open);
            try
            {
                await ec.Connect(ct);

                if (classified_custom_procs?.Count > 0)
                {
                    var types = classified_custom_procs.Values.Where(x => x.ProcType == SQLScriptType.Type).ToList();
                    foreach (var script in types)
                    {
                        await ec.ExecuteSQLNonQuery(script.SQLText, ct);
                    }

                    var sequences = classified_custom_procs.Values.Where(x => x.ProcType == SQLScriptType.Sequence).ToList();
                    foreach (var script in sequences)
                    {
                        await ec.ExecuteSQLNonQuery(script.SQLText, ct);
                    }
                }

            }
            finally
            {
                if (should_close) await ec.Disconnect();
            }
        }

        /// <summary>
        /// Creates custom tables defined in the classified scripts.
        /// </summary>
        /// <param name="ec">Entity client.</param>
        /// <param name="classified_custom_procs">Classified custom scripts.</param>
        /// <param name="ct">Cancellation token.</param>
        /// <param name="create_or_alter">Indicates if existing objects should be altered.</param>
        public async static Task CreateAllCustomTables(IEntityClient ec, CustomOrderedDictionary<CustomScript>? classified_custom_procs, CancellationToken ct, bool create_or_alter = true)
        {
            bool should_close = !(ec.ConnectionState == System.Data.ConnectionState.Open);
            try
            {
                await ec.Connect(ct);

                if (classified_custom_procs?.Count > 0)
                {
                    var types = classified_custom_procs.Values.Where(x => x.ProcType == SQLScriptType.Table).ToList();
                    foreach (var script in types)
                    {
                        await ec.ExecuteSQLNonQuery(script.SQLText, ct);
                    }
                }
            }
            finally
            {
                if (should_close) await ec.Disconnect();
            }
        }

        /// <summary>
        /// Creates custom views defined in the classified scripts.
        /// </summary>
        /// <param name="ec">Entity client.</param>
        /// <param name="classified_custom_procs">Classified custom scripts.</param>
        /// <param name="ct">Cancellation token.</param>
        /// <param name="create_or_alter">Indicates if existing objects should be altered.</param>
        public async static Task CreateAllCustomViews(IEntityClient ec, CustomOrderedDictionary<CustomScript>? classified_custom_procs, CancellationToken ct, bool create_or_alter = true)
        {
            bool should_close = !(ec.ConnectionState == System.Data.ConnectionState.Open);
            try
            {
                await ec.Connect(ct);

                if (classified_custom_procs?.Count > 0)
                {
                    var types = classified_custom_procs.Values.Where(x => x.ProcType == SQLScriptType.View).ToList();
                    foreach (var script in types)
                    {
                        await ec.ExecuteSQLNonQuery(script.SQLText, ct);
                    }
                }
            }
            finally
            {
                if (should_close) await ec.Disconnect();
            }
        }

        /// <summary>
        /// Drops indexes for the specified entities.
        /// </summary>
        /// <param name="ec">Entity client.</param>
        /// <param name="entities">Entity types.</param>
        /// <param name="ct">Cancellation token.</param>
        public static async Task DropEntitiesIndexes(IEntityClient ec, Dictionary<string, Type> entities, CancellationToken ct)
        {
            bool should_close = !(ec.ConnectionState == System.Data.ConnectionState.Open);
            try
            {
                await ec.Connect(ct);
                StringBuilder sb_drop_IDXs = new();
                foreach (var entity_type in entities.Values)
                {
                    if (entity_type != null)
                    {
                        var ent = (EntityBase?)Activator.CreateInstance(entity_type) ?? throw new InvalidOperationException($"Can't create entity instance. {entity_type.Name}");
                        if (ent != null)
                        {
                            ent.Init(ec);
                            sb_drop_IDXs.Append(ent.AsDropIndexes());
                        }
                    }
                }
                // drop indexes
                await ec.ExecuteSQLNonQuery(sb_drop_IDXs.ToString(), ct);
            }
            finally
            {
                if (should_close) await ec.Disconnect();
            }
        }

        /// <summary>
        /// Creates indexes for the specified entities.
        /// </summary>
        /// <param name="ec">Entity client.</param>
        /// <param name="entities">Entity types.</param>
        /// <param name="ct">Cancellation token.</param>
        public static async Task CreateEntitiesIndexes(IEntityClient ec, Dictionary<string, Type> entities, CancellationToken ct)
        {
            bool should_close = !(ec.ConnectionState == System.Data.ConnectionState.Open);
            try
            {
                await ec.Connect(ct);
                StringBuilder sb_create_IDXs = new();
                foreach (var entity_type in entities.Values)
                {
                    if (entity_type != null)
                    {
                        var ent = (EntityBase?)Activator.CreateInstance(entity_type) ?? throw new InvalidOperationException($"Can't create entity instance. {entity_type.Name}");
                        if (ent != null)
                        {
                            ent.Init(ec);
                            sb_create_IDXs.Append(ent.AsCreateIndexes());
                        }
                    }
                }
                // create indexes
                await ec.ExecuteSQLNonQuery(sb_create_IDXs.ToString(), ct);
            }
            finally
            {
                if (should_close) await ec.Disconnect();
            }
        }

        /// <summary>
        /// Drops constraints and indexes for the specified entities.
        /// </summary>
        /// <param name="ec">Entity client.</param>
        /// <param name="entities">Entity types.</param>
        /// <param name="ct">Cancellation token.</param>
        /// <param name="drop_primary_keys">Whether to drop primary keys as well.</param>
        public static async Task DropEntitiesConstraintsAndIndexes(IEntityClient ec, Dictionary<string, Type> entities, CancellationToken ct, bool drop_primary_keys = false)
        {
            bool should_close = !(ec.ConnectionState == System.Data.ConnectionState.Open);
            try
            {
                await ec.Connect(ct);
                StringBuilder sb_drop_FKs = new();
                StringBuilder sb_drop_PKs = new();
                StringBuilder sb_drop_UNs = new();
                StringBuilder sb_drop_IDXs = new();
                foreach (var entity_type in entities.Values)
                {
                    if (entity_type != null)
                    {
                        var ent = (EntityBase?)Activator.CreateInstance(entity_type) ?? throw new InvalidOperationException($"Can't create entity instance. {entity_type.Name}");
                        if (ent != null)
                        {
                            ent.Init(ec);
                            if (drop_primary_keys) sb_drop_PKs.Append(ent.AsDropPrimaryKey());
                            sb_drop_FKs.Append(ent.AsDropForeignKeys());
                            sb_drop_UNs.Append(ent.AsDropUniqueConstraints());
                            sb_drop_IDXs.Append(ent.AsDropIndexes());
                        }
                    }
                }
                // drop constraints and indexes
                await ec.ExecuteSQLNonQuery(sb_drop_IDXs.ToString(), ct);
                await ec.ExecuteSQLNonQuery(sb_drop_FKs.ToString(), ct);
                await ec.ExecuteSQLNonQuery(sb_drop_UNs.ToString(), ct);
                if (drop_primary_keys) await ec.ExecuteSQLNonQuery(sb_drop_PKs.ToString(), ct);
            }
            finally
            {
                if (should_close) await ec.Disconnect();
            }
        }

        /// <summary>
        /// Creates constraints and indexes for the specified entities.
        /// </summary>
        /// <param name="ec">Entity client.</param>
        /// <param name="entities">Entities to process.</param>
        /// <param name="ct">Cancellation token.</param>
        public static async Task CreateEntitiesConstraintsAndIndexes(IEntityClient ec, CustomOrderedDictionary<DatabaseSchemaCreationOptions<EntityBase>> entities, CancellationToken ct)
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
                    sb_create_FKs.Append(options.EntityInstance.AsAlterForeignKeys());
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



    }
}
