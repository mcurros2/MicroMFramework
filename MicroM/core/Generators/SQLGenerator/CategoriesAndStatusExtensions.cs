using MicroM.Core;
using MicroM.Data;
using MicroM.DataDictionary;
using MicroM.DataDictionary.Configuration;
using MicroM.Extensions;
using MicroM.Generators.Extensions;
using System.Globalization;
using System.Text;
using static MicroM.Generators.Constants;

namespace MicroM.Generators.SQLGenerator
{
    /// <summary>
    /// Provides SQL generation helpers for handling <c>_cat</c> and <c>_status</c> auxiliary tables.
    /// These extensions create the lookup tables and the necessary join clauses for entities
    /// that expose category or status relationships.
    /// </summary>
    internal static class CategoriesAndStatusExtensions
    {
        // MMC: this is too hard coded, it will be better to always define the entity for a _cat or _status table
        /// <summary>
        /// Builds the SQL statements required to create either a category or a status table
        /// associated with the supplied entity. The generated script includes primary key columns,
        /// foreign key constraints to the global lookup table and the creation of an index for
        /// faster joins.
        /// </summary>
        /// <param name="entity">Entity whose auxiliary table is being generated.</param>
        /// <param name="is_status">When <see langword="true"/> a status table is produced; otherwise a category table.</param>
        /// <returns>A list containing the table creation script and its related index statement.</returns>
        internal static List<string> CreateCategoryOrStatusTable(this EntityBase entity, bool is_status)
        {
            List<string> result = [];
            if (entity.Def.Fake) return result;

            string fk_index;
            StringBuilder ret = new();
            var sb_primary_key = new StringBuilder();

            string table_suffix = is_status ? TableSuffix.STATUS_TABLE_SUFFIX : TableSuffix.CATEGORY_TABLE_SUFFIX;

            ret.AppendFormat(CultureInfo.InvariantCulture, "create table [{0}_{1}]\n(\n", entity.Def.TableName, table_suffix);

            foreach (var col in entity.Def.Columns.GetWithFlags(ColumnFlags.PK))
            {
                ret.AppendFormat(CultureInfo.InvariantCulture, "{0} {1},\n", col.Name, col.AsSQLTypeString());
                sb_primary_key.AppendFormat(CultureInfo.InvariantCulture, "{0},", col.Name);
            }

            if (is_status)
            {
                var status = new StatusValues();
                ret.AppendFormat(CultureInfo.InvariantCulture, "{0} {1},\n", nameof(status.Def.c_status_id), status.Def.c_status_id.AsSQLTypeString());
                sb_primary_key.AppendFormat(CultureInfo.InvariantCulture, "{0},", nameof(status.Def.c_status_id));
                ret.AppendFormat(CultureInfo.InvariantCulture, "{0} {1},\n", nameof(status.Def.c_statusvalue_id), status.Def.c_statusvalue_id.AsSQLTypeString());
                sb_primary_key.AppendFormat(CultureInfo.InvariantCulture, "{0},", nameof(status.Def.c_statusvalue_id));

                ret.AppendFormat(CultureInfo.InvariantCulture, "CONSTRAINT FK{0}_{1} FOREIGN KEY ({2},{3}) REFERENCES {4},\n", entity.Def.Mneo, $"{entity.Def.Mneo}s", nameof(status.Def.c_status_id), nameof(status.Def.c_statusvalue_id), status.Def.TableName);

                fk_index = $"create index FKIDX{entity.Def.Mneo}_stv on {entity.Def.TableName} ({nameof(StatusValuesDef.c_status_id)}, {nameof(StatusValuesDef.c_statusvalue_id)})";
            }
            else
            {
                var cat = new CategoriesValues();
                ret.AppendFormat(CultureInfo.InvariantCulture, "{0} {1},\n", nameof(cat.Def.c_category_id), cat.Def.c_category_id.AsSQLTypeString());
                sb_primary_key.AppendFormat(CultureInfo.InvariantCulture, "{0},", nameof(cat.Def.c_category_id));
                ret.AppendFormat(CultureInfo.InvariantCulture, "{0} {1},\n", nameof(cat.Def.c_categoryvalue_id), cat.Def.c_categoryvalue_id.AsSQLTypeString());
                sb_primary_key.AppendFormat(CultureInfo.InvariantCulture, "{0},", nameof(cat.Def.c_categoryvalue_id));

                ret.AppendFormat(CultureInfo.InvariantCulture, "CONSTRAINT FK{0}_{1} FOREIGN KEY ({2},{3}) REFERENCES {4},\n", entity.Def.Mneo, $"{entity.Def.Mneo}s", nameof(cat.Def.c_category_id), nameof(cat.Def.c_categoryvalue_id), cat.Def.TableName);

                fk_index = $"create index FKIDX{entity.Def.Mneo}_cav on {entity.Def.TableName} ({nameof(CategoriesValuesDef.c_category_id)}, {nameof(CategoriesValuesDef.c_categoryvalue_id)})";
            }

            if (sb_primary_key.Length > 0)
            {
                sb_primary_key.Remove(sb_primary_key.Length - 1, 1);
                string primary_key = sb_primary_key.ToString();
                ret.AppendFormat(CultureInfo.InvariantCulture, "CONSTRAINT PK{0} PRIMARY KEY ({1})\n", entity.Def.Mneo, primary_key);
            }

            ret.Append(")\n");

            result.Add(ret.ToString().RemoveEmptyLines());
            result.Add(fk_index);
            return result;
        }

        /// <summary>
        /// Generates the SQL <c>JOIN</c> clauses required to link the entity table with its
        /// related category or status tables. Only the fake columns that reference a configured
        /// category or status are considered.
        /// </summary>
        /// <typeparam name="T">Type of the entity.</typeparam>
        /// <param name="entity">Entity for which the join statements are produced.</param>
        /// <param name="separator">Separator inserted between join fragments, usually new lines and tabs.</param>
        /// <param name="parent_alias">Alias used for the entity table in the join.</param>
        /// <param name="alias">Starting alias for the joined lookup tables; each join increments it.</param>
        /// <returns>SQL fragment representing the joins or an empty string when no joins are needed.</returns>
        internal static string AsCategoriesAndStatusJoin<T>(this T entity, string separator = $"\n{TAB}{TAB}", string parent_alias = "a", string alias = "b") where T : EntityBase
        {
            if (entity.Def.RelatedCategories.Count == 0 && entity.Def.RelatedStatus.Count == 0) return "";
            var sb = new StringBuilder();

            var cat = new CategoriesValues();
            var stat = new StatusValues();
            var PKs = entity.Def.Columns.GetWithFlags(ColumnFlags.PK);
            var Fakes = entity.Def.Columns.GetWithFlags(ColumnFlags.Fake, ColumnFlags.None);

            foreach (var cav_col in Fakes)
            {
                if (cav_col.SQLMetadata.IsArray == false)
                {
                    if (entity.Def.RelatedCategories.Contains(cav_col.RelatedCategoryID!))
                    {

                        var key_mappings = PKs.AsSQLJoinColumnsByName(parent_alias: parent_alias, child_alias: alias);

                        sb.Append(CultureInfo.InvariantCulture, $"{separator}{(cav_col.SQLMetadata.Nullable ? "left " : "")}join [{entity.Def.TableName}{TableSuffix.CATEGORY_TABLE_SUFFIX}] {alias}{separator}on({key_mappings}{separator}and {alias}.{cat.Def.c_category_id.Name} = '{cav_col.RelatedCategoryID!.SQLEscape()}')");

                        alias = ((char)(alias[0] + 1)).ToString();
                    }
                    else
                    {
                        if (entity.Def.RelatedStatus.Contains(cav_col.RelatedStatusID!))
                        {
                            var key_mappings = PKs.AsSQLJoinColumnsByName(parent_alias: parent_alias, child_alias: alias);

                            sb.Append(CultureInfo.InvariantCulture, $"{separator}{(cav_col.SQLMetadata.Nullable ? "left " : "")}join [{entity.Def.TableName}{TableSuffix.STATUS_TABLE_SUFFIX}] {alias}{separator}on({key_mappings}{separator}and {alias}.{stat.Def.c_status_id.Name} = '{cav_col.RelatedStatusID!.SQLEscape()}')");

                            alias = ((char)(alias[0] + 1)).ToString();
                        }
                    }

                }
            }

            return sb.ToString();
        }


    }
}
