using MicroM.Configuration;
using MicroM.Core;
using MicroM.Data;
using MicroM.Database;
using MicroM.DataDictionary.Configuration;
using MicroM.Extensions;
using MicroM.Generators.Extensions;
using System.Globalization;
using System.Text;

namespace MicroM.Generators.SQLGenerator;

/// <summary>
/// Extensions for generating database schema
/// </summary>
public static class TableExtensions
{

    public static string AsEntityCategoryTable<T>(this T entity) where T : EntityBase
    {
        var def = entity.Def;
        return $"{(!def.QualifiedSchemaName.IsNullOrEmpty() ? $"{def.QualifiedSchemaName}." : "")}[{def.TableName}{TableSuffix.CATEGORY_TABLE_SUFFIX}]";
    }

    public static string AsEntityStatusTable<T>(this T entity) where T : EntityBase
    {
        var def = entity.Def;
        return $"{(!def.QualifiedSchemaName.IsNullOrEmpty() ? $"{def.QualifiedSchemaName}." : "")}[{def.TableName}{TableSuffix.STATUS_TABLE_SUFFIX}]";
    }

    /// <summary>
    /// Checks for existence of the table that represents an <see cref="Entity{TDefinition}"/>
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="entity"></param>
    /// <param name="dbc"></param>
    /// <param name="ct"></param>
    /// <returns></returns>
    public async static Task<bool> IsTableCreated<T>(this T entity, DatabaseClient dbc, CancellationToken ct) where T : EntityBase
    {
        var result = await dbc.ExecuteSQL($"select object_id('{entity.Def.FullTableName}')", ct);
        if (result.HasData() && result[0].records[0][0]!.GetType() != typeof(DBNull)) return true;
        return false;
    }

    /// <summary>
    /// Returns a SQL script with the DDL to create the table and another script with indexes for each <see cref="EntityForeignKey{TParent, TChild}"/> (if they exist), for the <see cref="Entity{TDefinition}"/>
    /// </summary>
    public static List<string> AsCreateTable<T>(this T entity,
           AppDBSchemaConfiguration schema_config, bool force = false, bool table_and_primary_key_only = false
        ) where T : EntityBase
    {
        List<string> result = [];
        if (entity.Def.Fake && force == false) return result;

        StringBuilder sb_indexes = new();
        StringBuilder ret = new();
        StringBuilder sb_primary_key = new();
        StringBuilder sb_unique_constraints = new();

        ret.AppendFormat(CultureInfo.InvariantCulture, "create table {0}\n(\n", entity.Def.FullTableName);

        string? primary_key_columns = null;
        HashSet<string> unique_constraints_keys = [];
        HashSet<string> fk_indexes_keys = [];
        HashSet<string> indexes_keys = [];

        // PK and columns
        foreach (var col in entity.Def.Columns.Values)
        {
            if (!col.ColumnMetadata.HasFlag(ColumnFlags.Fake))
                ret.AppendFormat(CultureInfo.InvariantCulture, "{0} {1},\n", col.Name, col.AsSQLTypeString());

            if (col.ColumnMetadata.HasFlag(ColumnFlags.PK) && !col.ColumnMetadata.HasFlag(ColumnFlags.Fake))
                sb_primary_key.AppendFormat(CultureInfo.InvariantCulture, "{0},", col.Name);
        }

        if (sb_primary_key.Length > 0)
        {
            sb_primary_key.Remove(sb_primary_key.Length - 1, 1);
            primary_key_columns = sb_primary_key.ToString();
        }

        // unique constraints
        if (!table_and_primary_key_only && entity.Def.UniqueConstraints.Count > 0)
        {
            foreach (var unique in entity.Def.UniqueConstraints.Values)
            {
                var keys = string.Join<string>(",", unique.Keys);
                if (keys == primary_key_columns || unique_constraints_keys.Contains(keys))
                {
                    sb_unique_constraints.AppendFormat(CultureInfo.InvariantCulture, "-- Duplicate index: CONSTRAINT {0} UNIQUE ({1}),\n", unique.Name, keys);
                }
                else
                {
                    unique_constraints_keys.Add(keys);
                    sb_unique_constraints.AppendFormat(CultureInfo.InvariantCulture, "CONSTRAINT {0} UNIQUE ({1}),\n", unique.Name, keys);
                }
            }
        }

        var dd_types = DataDictionarySchema.GetCoreEntitiesTypes();

        foreach (var foreign_key in entity.Def.ForeignKeys.Values)
        {
            if (!foreign_key.Fake && !table_and_primary_key_only)
            {
                EntityBase? parent_entity = (EntityBase?)Activator.CreateInstance(foreign_key.ParentEntityType) ?? throw new ArgumentException($"Cannot create foreign key {foreign_key.Name}. You may need to map columns.");
                string parent_schema = dd_types.ContainsKey(foreign_key.ParentEntityType.Name) ? schema_config.DDSchema : schema_config.APPSchema;
                parent_entity.Init(null, null, parent_schema);

                if (foreign_key.KeyMappings.Count > 0)
                {
                    var sb_local_keys = new StringBuilder();
                    var sb_references_keys = new StringBuilder();
                    using (var mappings = foreign_key.KeyMappings.GetEnumerator())
                    {
                        mappings.MoveNext();

                        sb_references_keys.AppendFormat(CultureInfo.InvariantCulture, "{0}", mappings.Current.ParentColName);
                        sb_local_keys.AppendFormat(CultureInfo.InvariantCulture, "{0}", mappings.Current.ChildColName);

                        while (mappings.MoveNext())
                        {
                            sb_references_keys.AppendFormat(CultureInfo.InvariantCulture, ",{0}", mappings.Current.ParentColName);
                            sb_local_keys.AppendFormat(CultureInfo.InvariantCulture, ",{0}", mappings.Current.ChildColName);
                        }

                    }

                    string sb_local_keys_str = sb_local_keys.ToString();

                    ret.AppendFormat(CultureInfo.InvariantCulture, "CONSTRAINT {3} FOREIGN KEY ({0}) REFERENCES {1} ({2}),\n", sb_local_keys_str, parent_entity.Def.FullTableName, sb_references_keys.ToString(), foreign_key.Name);

                    // detect duplicate foreign key indexes
                    if (!foreign_key.DoNotCreateIndex)
                    {
                        if (sb_local_keys_str == primary_key_columns || unique_constraints_keys.Contains(sb_local_keys_str) || fk_indexes_keys.Contains(sb_local_keys_str))
                        {
                            sb_indexes.AppendFormat(CultureInfo.InvariantCulture, "-- duplicate index: create index IDX{0} on {1} ({2})\n", foreign_key.Name, entity.Def.FullTableName, sb_local_keys_str);
                        }
                        else
                        {
                            fk_indexes_keys.Add(sb_local_keys_str);
                            sb_indexes.AppendFormat(CultureInfo.InvariantCulture, "create index IDX{0} on {1} ({2})\n", foreign_key.Name, entity.Def.FullTableName, sb_local_keys_str);
                        }
                    }

                }
                else
                {
                    bool fk_created = false;

                    // MMC: the PK can be joined by name
                    IReadonlyOrderedDictionary<ColumnBase> child_pks = entity.Def.Columns.GetWithFlags(ColumnFlags.PK | ColumnFlags.FK);
                    if (child_pks.ContainsAllKeys(parent_entity.Def.Columns.GetWithFlags(ColumnFlags.PK)))
                    {
                        using var parent_pk_cols = ((IReadonlyOrderedDictionary<ColumnBase>)parent_entity.Def.Columns.GetWithFlags(ColumnFlags.PK)).GetEnumerator();
                        if (parent_pk_cols.MoveNext())
                        {
                            var sb_keys = new StringBuilder();
                            sb_keys.Append(parent_pk_cols.Current.Name);
                            while (parent_pk_cols.MoveNext())
                            {
                                sb_keys.AppendFormat(CultureInfo.InvariantCulture, ",{0}", parent_pk_cols.Current.Name);
                            }

                            string keys = sb_keys.ToString();
                            ret.AppendFormat(CultureInfo.InvariantCulture, "CONSTRAINT {3} FOREIGN KEY ({0}) REFERENCES {1} ({2}),\n", keys, parent_entity.Def.FullTableName, keys, foreign_key.Name);

                            // detect duplicate foreign key indexes
                            if (!foreign_key.DoNotCreateIndex)
                            {
                                if (keys == primary_key_columns || unique_constraints_keys.Contains(keys) || fk_indexes_keys.Contains(keys))
                                {
                                    sb_indexes.AppendFormat(CultureInfo.InvariantCulture, "-- duplicate index: create index IDX{0} on {1} ({2})\n", foreign_key.Name, entity.Def.FullTableName, keys);
                                }
                                else
                                {
                                    fk_indexes_keys.Add(keys);
                                    sb_indexes.AppendFormat(CultureInfo.InvariantCulture, "create index IDX{0} on {1} ({2})\n", foreign_key.Name, entity.Def.FullTableName, keys);
                                }
                            }

                        }
                        fk_created = true;
                    }

                    if (!fk_created)
                    {
                        // MMC: check in unique constraints
                        foreach (var un in parent_entity.Def.UniqueConstraints.Values)
                        {
                            if (child_pks.ContainsAllKeys(un.Keys))
                            {
                                var pk_cols = un.Keys.GetEnumerator();
                                if (pk_cols.MoveNext())
                                {
                                    var sb_keys = new StringBuilder();
                                    sb_keys.Append(pk_cols.Current);
                                    while (pk_cols.MoveNext())
                                    {
                                        sb_keys.AppendFormat(CultureInfo.InvariantCulture, ",{0}", pk_cols.Current);
                                    }

                                    string keys = sb_keys.ToString();
                                    ret.AppendFormat(CultureInfo.InvariantCulture, "CONSTRAINT {3} ({0}) REFERENCES {1} ({2}),\n", keys, parent_entity.Def.FullTableName, keys, foreign_key.Name);

                                    // detect duplicate foreign key indexes
                                    if (!foreign_key.DoNotCreateIndex)
                                    {
                                        if (keys == primary_key_columns || unique_constraints_keys.Contains(keys) || fk_indexes_keys.Contains(keys))
                                        {
                                            sb_indexes.AppendFormat(CultureInfo.InvariantCulture, "-- duplicate index: create index IDX{0} on {1} ({2})\n", foreign_key.Name, entity.Def.FullTableName, keys);
                                        }
                                        else
                                        {
                                            fk_indexes_keys.Add(keys);
                                            sb_indexes.AppendFormat(CultureInfo.InvariantCulture, "create index IDX{0} on {1} ({2})\n", foreign_key.Name, entity.Def.FullTableName, keys);
                                        }
                                    }

                                }
                                fk_created = true;
                                break;
                            }
                        }

                    }

                    if (!fk_created) throw new ArgumentException($"Cannot create foreign key {foreign_key.Name}. You may need to map columns.");

                }

            }

        }

        if (!table_and_primary_key_only && entity.Def.UniqueConstraints.Count > 0)
        {
            ret.Append(sb_unique_constraints);
        }

        if (!string.IsNullOrEmpty(primary_key_columns))
        {
            ret.AppendFormat(CultureInfo.InvariantCulture, "CONSTRAINT PK{0} PRIMARY KEY ({1})\n", entity.Def.Mneo, primary_key_columns);
        }

        ret.Append(")\n");

        if (!table_and_primary_key_only && entity.Def.Indexes.Count > 0)
        {
            foreach (var index in entity.Def.Indexes.Values)
            {
                var keys = string.Join<string>(",", index.Keys);
                if (keys == primary_key_columns || unique_constraints_keys.Contains(keys) || fk_indexes_keys.Contains(keys) || indexes_keys.Contains(keys))
                {
                    sb_indexes.AppendFormat(CultureInfo.InvariantCulture, "-- Duplicate index: create index {0} on {1} ({2})\n", index.Name, entity.Def.FullTableName, keys);
                }
                else
                {
                    indexes_keys.Add(keys);
                    sb_indexes.AppendFormat(CultureInfo.InvariantCulture, "create index {0} on {1} ({2})\n", index.Name, entity.Def.FullTableName, keys);
                }
            }
        }

        // add table
        result.Add(ret.ToString().RemoveEmptyLines());

        // add indexes
        string indexes = sb_indexes.ToString();
        if (!string.IsNullOrEmpty(indexes)) result.Add(sb_indexes.ToString().RemoveEmptyLines());

        return result;
    }

    public static string? AsDropIndexes<T>(this T entity) where T : EntityBase
    {
        if (entity.Def.Fake) return null;

        StringBuilder sb_indexes = new();

        if (entity.Def.Indexes.Count > 0)
        {
            foreach (var index in entity.Def.Indexes.Values)
            {
                sb_indexes.AppendFormat(CultureInfo.InvariantCulture, "drop index if exists {0}.[{1}]\n", entity.Def.FullTableName, index.Name);
            }
        }

        return sb_indexes.ToString();
    }

    public static string? AsCreateIndexes<T>(this T entity) where T : EntityBase
    {
        if (entity.Def.Fake) return null;

        StringBuilder sb_indexes = new();

        if (entity.Def.Indexes.Count > 0)
        {
            foreach (var index in entity.Def.Indexes.Values)
            {
                string qualidifed_schema = !entity.Def.QualifiedSchemaName.IsNullOrEmpty() ? $"{entity.Def.QualifiedSchemaName}." : "";
                sb_indexes.AppendFormat(CultureInfo.InvariantCulture, "if not exists(select 1 from sys.indexes a where a.object_id=object_id('{1}') and a.name='{0}') create index {0} on {1} (", index.Name, entity.Def.FullTableName);
                sb_indexes.Append(string.Join<string>(", ", index.Keys));
                sb_indexes.Append(")\n");
            }
        }

        return sb_indexes.ToString();
    }

    public static string? AsDropUniqueConstraints<T>(this T entity) where T : EntityBase
    {
        if (entity.Def.Fake) return null;

        StringBuilder sb_uniqueConstraints = new();

        if (entity.Def.UniqueConstraints.Count > 0)
        {
            foreach (var unique in entity.Def.UniqueConstraints.Values)
            {
                sb_uniqueConstraints.AppendFormat(CultureInfo.InvariantCulture, "if object_id('{0}') is not null ALTER TABLE {0} DROP CONSTRAINT IF EXISTS {1}\n", entity.Def.FullTableName, unique.Name);
            }
        }

        return sb_uniqueConstraints.ToString();
    }

    public static string? AsAlterUniqueConstraints<T>(this T entity) where T : EntityBase
    {
        if (entity.Def.Fake) return null;

        StringBuilder sb_uniqueConstraints = new();

        if (entity.Def.UniqueConstraints.Count > 0)
        {
            foreach (var unique in entity.Def.UniqueConstraints.Values)
            {
                string qualified_unique = $"{(!entity.Def.QualifiedSchemaName.IsNullOrEmpty() ? $"{entity.Def.QualifiedSchemaName}." : "")}{unique.Name}";
                sb_uniqueConstraints.AppendFormat(CultureInfo.InvariantCulture, "if object_id('{0}') is not null and object_id('{2}') is null ALTER TABLE {0} ADD CONSTRAINT {1} UNIQUE (", entity.Def.FullTableName, unique.Name, qualified_unique);
                sb_uniqueConstraints.Append(string.Join<string>(", ", unique.Keys));
                sb_uniqueConstraints.Append(")\n");
            }
        }

        return sb_uniqueConstraints.ToString();
    }

    public static string? AsDropPrimaryKey<T>(this T entity) where T : EntityBase
    {
        if (entity.Def.Fake) return null;

        return $"if object_id('{entity.Def.FullTableName}') is not null ALTER TABLE {entity.Def.FullTableName} DROP CONSTRAINT IF EXISTS {(!entity.Def.QualifiedSchemaName.IsNullOrEmpty() ? $"{entity.Def.QualifiedSchemaName}." : "")}PK{entity.Def.Mneo}\n";
    }

    public static string? AsAlterPrimaryKey<T>(this T entity) where T : EntityBase
    {
        if (entity.Def.Fake) return null;
        string? result = null;

        var pks = entity.Def.Columns.GetWithFlags(ColumnFlags.PK);
        if (pks != null)
        {
            string col_names = string.Join(", ", pks.Values.Select(x => x.Name));
            string pk_name = $"{(!entity.Def.QualifiedSchemaName.IsNullOrEmpty() ? $"{entity.Def.QualifiedSchemaName}." : "")}PK{entity.Def.Mneo}";
            result = $"if object_id('{entity.Def.FullTableName}') is not null and object_id('{pk_name}') is null ALTER TABLE {entity.Def.FullTableName} ADD CONSTRAINT PK{entity.Def.Mneo} PRIMARY KEY ({col_names})\n";
        }

        return result;
    }

    public static string? AsDropForeignKeys<T>(this T entity) where T : EntityBase
    {
        if (entity.Def.Fake) return null;

        StringBuilder sb_foreign_keys = new();

        foreach (var foreign_key in entity.Def.ForeignKeys.Values)
        {
            if (!foreign_key.Fake)
            {
                sb_foreign_keys.AppendFormat(CultureInfo.InvariantCulture, "if object_id('{0}') is not null ALTER TABLE {0} DROP CONSTRAINT IF EXISTS {1}\n",
                    entity.Def.FullTableName, $"{(!entity.Def.QualifiedSchemaName.IsNullOrEmpty() ? $"{entity.Def.QualifiedSchemaName}." : "")}{foreign_key.Name}");
            }

        }

        return sb_foreign_keys.Append(sb_foreign_keys).ToString();
    }

    public static string? AsCreateForeignKeysIndexes<T>(this T entity) where T : EntityBase
    {
        if (entity.Def.Fake) return null;

        StringBuilder sb_indexes = new();

        foreach (var foreign_key in entity.Def.ForeignKeys.Values)
        {
            if (!foreign_key.Fake && !foreign_key.DoNotCreateIndex)
            {
                EntityBase? parent_entity = (EntityBase?)Activator.CreateInstance(foreign_key.ParentEntityType) ?? throw new ArgumentException($"Cannot create index for foreign key {foreign_key.Name}. You may need to map columns.");

                string fk_name = $"{(!entity.Def.QualifiedSchemaName.IsNullOrEmpty() ? $"{entity.Def.QualifiedSchemaName}." : "")}{foreign_key.Name}";

                sb_indexes.AppendFormat(CultureInfo.InvariantCulture, "create index IDX{0} on {1} (", fk_name, entity.Def.FullTableName);

                if (foreign_key.KeyMappings.Count > 0)
                {
                    var sb_local = new StringBuilder();
                    var sb_references = new StringBuilder();
                    using var mappings = foreign_key.KeyMappings.GetEnumerator();
                    mappings.MoveNext();

                    sb_indexes.AppendFormat(CultureInfo.InvariantCulture, "{0}", mappings.Current.ChildColName);

                    while (mappings.MoveNext())
                    {
                        sb_indexes.AppendFormat(CultureInfo.InvariantCulture, ",{0}", mappings.Current.ChildColName);
                    }
                }
                else
                {
                    bool fk_created = false;
                    // MMC: the PK can be joined by name
                    IReadonlyOrderedDictionary<ColumnBase> child_pks = entity.Def.Columns.GetWithFlags(ColumnFlags.PK | ColumnFlags.FK);
                    if (child_pks.ContainsAllKeys(parent_entity.Def.Columns.GetWithFlags(ColumnFlags.PK)))
                    {
                        using var pk_cols = ((IReadonlyOrderedDictionary<ColumnBase>)parent_entity.Def.Columns.GetWithFlags(ColumnFlags.PK)).GetEnumerator();
                        if (pk_cols.MoveNext())
                        {
                            var sb_keys = new StringBuilder();
                            sb_keys.Append(pk_cols.Current.Name);
                            sb_indexes.Append(pk_cols.Current.Name);
                            while (pk_cols.MoveNext())
                            {
                                sb_indexes.AppendFormat(CultureInfo.InvariantCulture, ",{0}", pk_cols.Current.Name);
                                sb_keys.AppendFormat(CultureInfo.InvariantCulture, ",{0}", pk_cols.Current.Name);
                            }
                        }
                        fk_created = true;
                    }

                    if (!fk_created)
                    {
                        // MMC: check in unique constraints
                        foreach (var un in parent_entity.Def.UniqueConstraints.Values)
                        {
                            if (child_pks.ContainsAllKeys(un.Keys))
                            {
                                var pk_cols = un.Keys.GetEnumerator();
                                if (pk_cols.MoveNext())
                                {
                                    var sb_keys = new StringBuilder();
                                    sb_keys.Append(pk_cols.Current);
                                    sb_indexes.Append(pk_cols.Current);
                                    while (pk_cols.MoveNext())
                                    {
                                        sb_indexes.AppendFormat(CultureInfo.InvariantCulture, ",{0}", pk_cols.Current);
                                        sb_keys.AppendFormat(CultureInfo.InvariantCulture, ",{0}", pk_cols.Current);
                                    }
                                    string keys = sb_keys.ToString();
                                }
                                fk_created = true;
                                break;
                            }

                        }
                    }

                    if (!fk_created) throw new ArgumentException($"Cannot create index for foreign key {fk_name}. You may need to map columns.");

                }
                sb_indexes.Append(")\n");
            }

        }

        return sb_indexes.ToString();
    }

    public static string? AsAlterForeignKeys<T>(this T entity, AppDBSchemaConfiguration schema_config, bool with_drop = false) where T : EntityBase
    {
        if (entity.Def.Fake) return null;

        StringBuilder sb_foreign_keys = new();

        var dd_types = DataDictionarySchema.GetCoreEntitiesTypes();

        foreach (var foreign_key in entity.Def.ForeignKeys.Values)
        {
            if (!foreign_key.Fake)
            {

                string fk_name = $"{(!entity.Def.QualifiedSchemaName.IsNullOrEmpty() ? $"{entity.Def.QualifiedSchemaName}." : "")}{foreign_key.Name}";

                EntityBase? parent_entity = (EntityBase?)Activator.CreateInstance(foreign_key.ParentEntityType) ?? throw new ArgumentException($"Cannot create foreign key {fk_name}. You may need to map columns.");
                string parent_schema = dd_types.ContainsKey(foreign_key.ParentEntityType.Name) ? schema_config.DDSchema : schema_config.APPSchema;
                parent_entity.Init(null, null, parent_schema);

                if (with_drop) sb_foreign_keys.AppendFormat(CultureInfo.InvariantCulture, "if object_id('{0}') is not null ALTER TABLE {0} DROP CONSTRAINT {1}\n", entity.Def.FullTableName, fk_name);

                sb_foreign_keys.AppendFormat(CultureInfo.InvariantCulture, "if object_id('{0}') is not null and object_id('{2}') is null ALTER TABLE {0} ADD CONSTRAINT {1} FOREIGN KEY ", entity.Def.FullTableName, foreign_key.Name, fk_name);

                if (foreign_key.KeyMappings.Count > 0)
                {
                    var sb_local = new StringBuilder();
                    var sb_references = new StringBuilder();
                    using (var mappings = foreign_key.KeyMappings.GetEnumerator())
                    {
                        mappings.MoveNext();

                        sb_references.AppendFormat(CultureInfo.InvariantCulture, "{0}", mappings.Current.ParentColName);
                        sb_local.AppendFormat(CultureInfo.InvariantCulture, "{0}", mappings.Current.ChildColName);

                        while (mappings.MoveNext())
                        {
                            sb_references.AppendFormat(CultureInfo.InvariantCulture, ",{0}", mappings.Current.ParentColName);
                            sb_local.AppendFormat(CultureInfo.InvariantCulture, ",{0}", mappings.Current.ChildColName);
                        }

                    }
                    sb_foreign_keys.AppendFormat(CultureInfo.InvariantCulture, "({0}) REFERENCES {1} ({2})\n", sb_local.ToString(), parent_entity.Def.FullTableName, sb_references.ToString());
                }
                else
                {
                    bool fk_created = false;
                    // MMC: the PK can be joined by name
                    IReadonlyOrderedDictionary<ColumnBase> child_pks = entity.Def.Columns.GetWithFlags(ColumnFlags.PK | ColumnFlags.FK);
                    if (child_pks.ContainsAllKeys(parent_entity.Def.Columns.GetWithFlags(ColumnFlags.PK)))
                    {
                        using var pk_cols = ((IReadonlyOrderedDictionary<ColumnBase>)parent_entity.Def.Columns.GetWithFlags(ColumnFlags.PK)).GetEnumerator();
                        if (pk_cols.MoveNext())
                        {
                            var sb_keys = new StringBuilder();
                            sb_keys.Append(pk_cols.Current.Name);
                            while (pk_cols.MoveNext())
                            {
                                sb_keys.AppendFormat(CultureInfo.InvariantCulture, ",{0}", pk_cols.Current.Name);
                            }
                            string keys = sb_keys.ToString();
                            sb_foreign_keys.AppendFormat(CultureInfo.InvariantCulture, "({0}) REFERENCES {1} ({2})\n", keys, parent_entity.Def.FullTableName, keys);
                        }
                        fk_created = true;
                    }

                    if (!fk_created)
                    {
                        // MMC: check in unique constraints
                        foreach (var un in parent_entity.Def.UniqueConstraints.Values)
                        {
                            if (child_pks.ContainsAllKeys(un.Keys))
                            {
                                var pk_cols = un.Keys.GetEnumerator();
                                if (pk_cols.MoveNext())
                                {
                                    var sb_keys = new StringBuilder();
                                    sb_keys.Append(pk_cols.Current);
                                    while (pk_cols.MoveNext())
                                    {
                                        sb_keys.AppendFormat(CultureInfo.InvariantCulture, ",{0}", pk_cols.Current);
                                    }
                                    string keys = sb_keys.ToString();
                                    sb_foreign_keys.AppendFormat(CultureInfo.InvariantCulture, "({0}) REFERENCES {1} ({2})\n", keys, parent_entity.Def.FullTableName, keys);
                                }
                                fk_created = true;
                                break;
                            }

                        }
                    }

                    if (!fk_created) throw new ArgumentException($"Cannot create foreign key {fk_name}. You may need to map columns.");

                }

            }

        }

        return sb_foreign_keys.ToString();
    }

    public static string AsGrantExecutionToEntityProcsScript<T>(this T entity, string login_or_group_name) where T : EntityBase
    {

        StringBuilder sb = new();

        string qualified_schema = !entity.Def.QualifiedSchemaName.IsNullOrEmpty() ? $"{entity.Def.QualifiedSchemaName}." : "";

        sb.AppendFormat(CultureInfo.InvariantCulture, "if(select object_id('{2}[{0}_update]')) is not null grant exec on {2}[{0}_update] to [{1}]\n", entity.Def.Mneo, login_or_group_name, qualified_schema);
        sb.AppendFormat(CultureInfo.InvariantCulture, "if(select object_id('{2}[{0}_get]')) is not null grant exec on {2}[{0}_get] to [{1}]\n", entity.Def.Mneo, login_or_group_name, qualified_schema);
        sb.AppendFormat(CultureInfo.InvariantCulture, "if(select object_id('{2}[{0}_drop]')) is not null grant exec on {2}[{0}_drop] to [{1}]\n", entity.Def.Mneo, login_or_group_name, qualified_schema);
        sb.AppendFormat(CultureInfo.InvariantCulture, "if(select object_id('{2}[{0}_lookup]')) is not null grant exec on {2}[{0}_lookup] to [{1}]\n", entity.Def.Mneo, login_or_group_name, qualified_schema);

        foreach (var proc in entity.Def.Procs.Values)
        {
            sb.AppendFormat(CultureInfo.InvariantCulture, "if(select object_id('{0}')) is not null grant exec on {0} to [{1}]\n", proc.QualifiedName, login_or_group_name);
        }

        foreach (var view in entity.Def.Views.Values)
        {
            sb.AppendFormat(CultureInfo.InvariantCulture, "if(select object_id('{0}')) is not null grant exec on {0} to [{1}]\n", view.Proc.QualifiedName, login_or_group_name);
        }

        return sb.ToString();
    }



}
