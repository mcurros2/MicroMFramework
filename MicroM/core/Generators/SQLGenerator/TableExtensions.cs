using MicroM.Core;
using MicroM.Data;
using MicroM.Extensions;
using MicroM.Generators.Extensions;
using System.Globalization;
using System.Text;

namespace MicroM.Generators.SQLGenerator
{
    /// <summary>
    /// Extensions for generating database schema
    /// </summary>
    public static class TableExtensions
    {

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
            var result = await dbc.ExecuteSQL($"select object_id('{entity.Def.TableName}')", ct);
            if (result.HasData() && result[0].records[0][0]!.GetType() != typeof(DBNull)) return true;
            return false;
        }

        /// <summary>
        /// Returns a SQL script with the DDL to create the table and another script with indexes for each <see cref="EntityForeignKey{TParent, TChild}"/> (if they exist), for the <see cref="Entity{TDefinition}"/>
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="entity"></param>
        /// <param name="force">forces generating code for Fake entities</param>
        /// <returns></returns>
        public static List<string> AsCreateTable<T>(this T entity, bool force = false) where T : EntityBase
        {
            List<string> result = [];
            if (entity.Def.Fake && force == false) return result;

            StringBuilder sb_indexes = new();
            StringBuilder ret = new();
            StringBuilder sb_primary_key = new();

            ret.AppendFormat(CultureInfo.InvariantCulture, "create table [{0}]\n(\n", entity.Def.TableName);

            foreach (var col in entity.Def.Columns.Values)
            {
                if (!col.ColumnMetadata.HasFlag(ColumnFlags.Fake)) ret.AppendFormat(CultureInfo.InvariantCulture, "{0} {1},\n", col.Name, col.AsSQLTypeString());
                if (col.ColumnMetadata.HasFlag(ColumnFlags.PK) && !col.ColumnMetadata.HasFlag(ColumnFlags.Fake)) sb_primary_key.AppendFormat(CultureInfo.InvariantCulture, "{0},", col.Name);
            }

            foreach (var foreign_key in entity.Def.ForeignKeys.Values)
            {

                if (!foreign_key.Fake)
                {
                    EntityBase? parent_entity = (EntityBase?)Activator.CreateInstance(foreign_key.ParentEntityType) ?? throw new ArgumentException($"Cannot create foreign key {foreign_key.Name}. You may need to map columns.");

                    sb_indexes.AppendFormat(CultureInfo.InvariantCulture, "create index IDX{0} on [{1}] (", foreign_key.Name, entity.Def.TableName);

                    ret.AppendFormat(CultureInfo.InvariantCulture, "CONSTRAINT {0} FOREIGN KEY ", foreign_key.Name);
                    if (foreign_key.KeyMappings.Count > 0)
                    {
                        var sb_local = new StringBuilder();
                        var sb_references = new StringBuilder();
                        using (var mappings = foreign_key.KeyMappings.GetEnumerator())
                        {
                            mappings.MoveNext();

                            sb_references.AppendFormat(CultureInfo.InvariantCulture, "{0}", mappings.Current.ParentColName);
                            sb_local.AppendFormat(CultureInfo.InvariantCulture, "{0}", mappings.Current.ChildColName);
                            sb_indexes.AppendFormat(CultureInfo.InvariantCulture, "{0}", mappings.Current.ChildColName);

                            while (mappings.MoveNext())
                            {
                                sb_references.AppendFormat(CultureInfo.InvariantCulture, ",{0}", mappings.Current.ParentColName);
                                sb_local.AppendFormat(CultureInfo.InvariantCulture, ",{0}", mappings.Current.ChildColName);
                                sb_indexes.AppendFormat(CultureInfo.InvariantCulture, ",{0}", mappings.Current.ChildColName);
                            }

                        }
                        ret.AppendFormat(CultureInfo.InvariantCulture, "({0}) REFERENCES {1} ({2}),\n", sb_local.ToString(), parent_entity.Def.TableName, sb_references.ToString());
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
                                string keys = sb_keys.ToString();
                                ret.AppendFormat(CultureInfo.InvariantCulture, "({0}) REFERENCES {1} ({2}),\n", keys, parent_entity.Def.TableName, keys);
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
                                        ret.AppendFormat(CultureInfo.InvariantCulture, "({0}) REFERENCES {1} ({2}),\n", keys, parent_entity.Def.TableName, keys);
                                    }
                                    fk_created = true;
                                    break;
                                }

                            }
                        }

                        if (!fk_created) throw new ArgumentException($"Cannot create foreign key {foreign_key.Name}. You may need to map columns.");

                    }

                    sb_indexes.Append(")\n");
                }

            }

            if (entity.Def.UniqueConstraints.Count > 0)
            {
                foreach (var unique in entity.Def.UniqueConstraints.Values)
                {
                    ret.AppendFormat(CultureInfo.InvariantCulture, "CONSTRAINT {0} UNIQUE (", unique.Name);
                    ret.Append(string.Join<string>(", ", unique.Keys));
                    ret.Append("),\n");
                }
            }

            if (entity.Def.Indexes.Count > 0)
            {
                foreach (var index in entity.Def.Indexes.Values)
                {
                    sb_indexes.AppendFormat(CultureInfo.InvariantCulture, "create index IDX{0} on [{1}] (", index.Name, entity.Def.TableName);
                    sb_indexes.Append(string.Join<string>(", ", index.Keys));
                    sb_indexes.Append(")\n");
                }
            }

            if (sb_primary_key.Length > 0)
            {
                sb_primary_key.Remove(sb_primary_key.Length - 1, 1);
                string primary_key = sb_primary_key.ToString();
                ret.AppendFormat(CultureInfo.InvariantCulture, "CONSTRAINT PK{0} PRIMARY KEY ({1})\n", entity.Def.Mneo, primary_key);
            }

            ret.Append(")\n");

            result.Add(ret.ToString().RemoveEmptyLines());
            string indexes = sb_indexes.ToString();
            if (!string.IsNullOrEmpty(indexes)) result.Add(sb_indexes.ToString().RemoveEmptyLines());
            return result;
        }


    }
}
