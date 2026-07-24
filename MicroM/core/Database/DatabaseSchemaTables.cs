using MicroM.Configuration;
using MicroM.Core;
using MicroM.Data;
using MicroM.Extensions;
using MicroM.Generators.SQLGenerator;
using System.Text;
using static MicroM.Database.DatabaseManagement;

namespace MicroM.Database
{
    public static class DatabaseSchemaTables
    {
        public static async Task<HashSet<string>> GetEntitiesInexistingTables(IEntityClient ec, CustomOrderedDictionary<DatabaseSchemaCreationOptions<EntityBase>> entities, CancellationToken ct)
        {
            bool should_close = !(ec.ConnectionState == System.Data.ConnectionState.Open);
            HashSet<string> inexisting_tables = new(StringComparer.OrdinalIgnoreCase);
            try
            {
                await ec.Connect(ct);
                foreach (var options in entities.Values)
                {
                    bool table_exists = await TableExists(ec, options.EntityInstance.Def.TableName, options.EntityInstance.Def.SchemaName ?? "dbo", ct);
                    if (!table_exists) inexisting_tables.Add(options.EntityInstance.Def.FullTableName);
                }
            }
            finally
            {
                if (should_close) await ec.Disconnect();
            }
            return inexisting_tables;
        }

        public static async Task CreateAllInexistingSchemas(IEntityClient ec, CustomOrderedDictionary<DatabaseSchemaCreationOptions<EntityBase>> entities, CancellationToken ct)
        {
            bool should_close = !(ec.ConnectionState == System.Data.ConnectionState.Open);
            HashSet<string> processed_schemas = new(StringComparer.OrdinalIgnoreCase);

            try
            {
                await ec.Connect(ct);

                StringBuilder sb_create_schemas = new();
                foreach (var options in entities.Values)
                {
                    var entity = options.EntityInstance;
                    if (!string.IsNullOrEmpty(entity.Def.SchemaName))
                    {
                        if (!processed_schemas.Contains(entity.Def.SchemaName))
                        {
                            bool schema_exists = await SchemaExists(ec, entity.Def.SchemaName, ct);
                            if (!schema_exists)
                            {
                                sb_create_schemas.Append($"create schema {entity.Def.QualifiedSchemaName};");
                            }
                            processed_schemas.Add(entity.Def.SchemaName);
                        }
                    }
                }

                if (sb_create_schemas.Length > 0)
                {
                    await ec.ExecuteSQLNonQuery(sb_create_schemas.ToString(), ct);
                }

            }
            finally
            {
                processed_schemas.Clear();
                if (should_close) await ec.Disconnect();
            }
        }

        public static async Task<CustomOrderedDictionary<DatabaseSchemaCreationOptions<EntityBase>>> CreateEntitiesInexistentTables(IEntityClient ec, CustomOrderedDictionary<DatabaseSchemaCreationOptions<EntityBase>> entities, AppDBSchemaConfiguration schema_config, CancellationToken ct)
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
                    if (inexisting_tables.Contains(options.EntityInstance.Def.FullTableName))
                    {
                        var scripts = options.EntityInstance.AsCreateTable(schema_config, table_and_primary_key_only: true);
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

        public static async Task DropEntitiesIndexes(IEntityClient ec, CustomOrderedDictionary<DatabaseSchemaCreationOptions<EntityBase>> entities, CancellationToken ct)
        {
            bool should_close = !(ec.ConnectionState == System.Data.ConnectionState.Open);
            try
            {
                await ec.Connect(ct);
                StringBuilder sb_drop_IDXs = new();
                foreach (var entity_option in entities.Values)
                {
                    if (entity_option != null)
                    {
                        var ent = entity_option.EntityInstance;
                        if (ent != null)
                        {
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

        public static async Task CreateEntitiesIndexes(IEntityClient ec, CustomOrderedDictionary<DatabaseSchemaCreationOptions<EntityBase>> entities, CancellationToken ct)
        {
            bool should_close = !(ec.ConnectionState == System.Data.ConnectionState.Open);
            try
            {
                await ec.Connect(ct);
                StringBuilder sb_create_IDXs = new();
                foreach (var entity_option in entities.Values)
                {
                    if (entity_option != null)
                    {
                        var ent = entity_option.EntityInstance;
                        if (ent != null)
                        {
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

        public static async Task DropEntitiesConstraintsAndIndexes(IEntityClient ec, CustomOrderedDictionary<DatabaseSchemaCreationOptions<EntityBase>> entities, CancellationToken ct, bool drop_primary_keys = false)
        {
            bool should_close = !(ec.ConnectionState == System.Data.ConnectionState.Open);
            try
            {
                await ec.Connect(ct);
                StringBuilder sb_drop_FKs = new();
                StringBuilder sb_drop_PKs = new();
                StringBuilder sb_drop_UNs = new();
                StringBuilder sb_drop_IDXs = new();
                foreach (var entity_option in entities.Values)
                {
                    if (entity_option != null)
                    {
                        var ent = entity_option.EntityInstance;
                        if (ent != null)
                        {
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

        public static async Task CreateEntitiesConstraintsAndIndexes(IEntityClient ec, CustomOrderedDictionary<DatabaseSchemaCreationOptions<EntityBase>> entities, AppDBSchemaConfiguration schema_config, CancellationToken ct, bool create_only_inexisting_constraints_and_indexes = true)
        {
            bool should_close = !(ec.ConnectionState == System.Data.ConnectionState.Open);
            try
            {
                await ec.Connect(ct);

                StringBuilder sb_create_PKs = new();
                StringBuilder sb_create_UNs = new();
                StringBuilder sb_create_FKs = new();
                StringBuilder sb_create_IDXs = new();

                if (create_only_inexisting_constraints_and_indexes)
                {
                    // check for existing constraints/indexes by columns before generating DDL
                    foreach (var options in entities.Values)
                    {
                        var entity = options.EntityInstance;
                        string full_table_name = entity.Def.FullTableName;

                        var existing_pk_columns = await GetExistingPrimaryKeyColumns(ec, full_table_name, ct);

                        // Create PK only if not exists
                        if (existing_pk_columns == null)
                        {
                            // Primary Key: check if PK exists with same ordered columns
                            var pk_columns = entity.Def.Columns.GetWithFlags(ColumnFlags.PK);
                            if (pk_columns != null && pk_columns.Count > 0)
                            {
                                var pk_column_names = pk_columns.Values.Select(c => c.Name).ToList();

                                sb_create_PKs.Append(entity.AsAlterPrimaryKey());
                            }
                        }

                        // Unique Constraints: check each unique constraint
                        if (entity.Def.UniqueConstraints.Count > 0)
                        {
                            var existing_uniques = await GetExistingUniqueConstraints(ec, full_table_name, ct);

                            foreach (var unique in entity.Def.UniqueConstraints.Values)
                            {
                                if (!ConstraintExistsWithTheSameColumns(existing_uniques, unique.Keys))
                                {
                                    string qualified_unique = $"{(!entity.Def.QualifiedSchemaName.IsNullOrEmpty() ? $"{entity.Def.QualifiedSchemaName}." : "")}{unique.Name}";
                                    sb_create_UNs.AppendFormat(System.Globalization.CultureInfo.InvariantCulture,
                                        "if object_id('{0}') is not null and object_id('{2}') is null ALTER TABLE {0} ADD CONSTRAINT {1} UNIQUE (",
                                        entity.Def.FullTableName, unique.Name, qualified_unique);
                                    sb_create_UNs.Append(string.Join<string>(", ", unique.Keys));
                                    sb_create_UNs.Append(")\n");
                                }
                            }
                        }

                        // Foreign Keys: check each foreign key by child columns
                        if (entity.Def.ForeignKeys.Count > 0)
                        {
                            var existing_fks = await GetExistingForeignKeys(ec, full_table_name, ct);
                            var dd_types = Database.DataDictionarySchema.GetCoreEntitiesTypes();

                            foreach (var foreign_key in entity.Def.ForeignKeys.Values)
                            {
                                if (!foreign_key.Fake)
                                {
                                    // Extract child columns from the foreign key
                                    List<string> fk_child_columns = [];

                                    if (foreign_key.KeyMappings.Count > 0)
                                    {
                                        fk_child_columns = [.. foreign_key.KeyMappings.Select(m => m.ChildColName)];
                                    }
                                    else
                                    {
                                        // Fallback to PK inference by column names if no explicit key mappings are provided
                                        EntityBase? parent_entity = (EntityBase?)Activator.CreateInstance(foreign_key.ParentEntityType);
                                        if (parent_entity != null)
                                        {
                                            string parent_schema = dd_types.ContainsKey(foreign_key.ParentEntityType.Name) ? schema_config.DDSchema : schema_config.APPSchema;
                                            parent_entity.Init(null, null, parent_schema);

                                            var child_pks = entity.Def.Columns.GetWithFlags(ColumnFlags.PK | ColumnFlags.FK);
                                            if (child_pks.ContainsAllKeys(parent_entity.Def.Columns.GetWithFlags(ColumnFlags.PK)))
                                            {
                                                fk_child_columns = [.. parent_entity.Def.Columns.GetWithFlags(ColumnFlags.PK).Values.Select(c => c.Name)];
                                            }
                                            else
                                            {
                                                // Check unique constraints if we can't find a PK match
                                                foreach (var un in parent_entity.Def.UniqueConstraints.Values)
                                                {
                                                    if (child_pks.ContainsAllKeys(un.Keys))
                                                    {
                                                        fk_child_columns = [.. un.Keys];
                                                        break;
                                                    }
                                                }
                                            }
                                        }
                                    }

                                    if (fk_child_columns.Count > 0 && !ConstraintExistsWithTheSameColumns(existing_fks, fk_child_columns))
                                    {
                                        string fk_name = $"{(!entity.Def.QualifiedSchemaName.IsNullOrEmpty() ? $"{entity.Def.QualifiedSchemaName}." : "")}{foreign_key.Name}";
                                        string? fk_script = entity.AsAlterForeignKeys(schema_config, with_drop: false);
                                        if (!string.IsNullOrEmpty(fk_script))
                                        {
                                            sb_create_FKs.Append(fk_script);
                                        }
                                        break; // Only append once per entity to avoid duplicates
                                    }
                                }
                            }
                        }

                        // Indexes: check each index
                        if (entity.Def.Indexes.Count > 0)
                        {
                            var existing_indexes = await GetExistingIndexes(ec, full_table_name, ct);

                            foreach (var index in entity.Def.Indexes.Values)
                            {
                                if (!IndexExistsWithTheSameColumns(existing_indexes, index.Keys))
                                {
                                    string? index_script = entity.AsCreateIndex(index.Name);
                                    if (!string.IsNullOrEmpty(index_script))
                                    {
                                        sb_create_IDXs.Append(index_script);
                                    }
                                }
                            }
                        }
                    }
                }
                else
                {
                    // Legacy behavior: unconditionally generate all DDL using existing methods
                    foreach (var options in entities.Values)
                    {
                        sb_create_PKs.Append(options.EntityInstance.AsAlterPrimaryKey());
                        sb_create_UNs.Append(options.EntityInstance.AsAlterUniqueConstraints());
                        sb_create_FKs.Append(options.EntityInstance.AsAlterForeignKeys(schema_config));
                        sb_create_IDXs.Append(options.EntityInstance.AsCreateIndexes());
                    }
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
