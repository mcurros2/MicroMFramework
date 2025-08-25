using MicroM.Core;
using MicroM.Data;
using MicroM.DataDictionary;
using MicroM.DataDictionary.Configuration;
using MicroM.Extensions;
using MicroM.Generators.Extensions;
using System.Text;
using static MicroM.Generators.Constants;

namespace MicroM.Generators.SQLGenerator
{
    /// <summary>
    /// Provides extension methods for generating SQL scripts related to entity status values.
    /// </summary>
    internal static class StatusExtensions
    {
        /// <summary>
        /// Returns two SQL scripts with the DDL to create a child status table and a foreign key index for an <see cref="Entity{TDefinition}"/>.
        /// The status table name for the specified entity will be <![CDATA[<entity table name>_status]]> and will include the primary keys of the entity and <see cref="Status"/> along with <see cref="DefaultColumns"/>.
        /// </summary>
        /// <typeparam name="T">The type of entity definition.</typeparam>
        /// <param name="entity">The entity for which to create the status table.</param>
        /// <returns>A list containing the table creation and index scripts.</returns>
        internal static List<string> CreateStatusTable<T>(this T entity) where T : EntityBase
        {
            return entity.CreateCategoryOrStatusTable(true);
        }

        /// <summary>
        /// Builds an INSERT statement for the status table for the specified entity.
        /// </summary>
        /// <typeparam name="T">The type of entity definition.</typeparam>
        /// <param name="entity">The entity whose status values will be inserted.</param>
        /// <param name="separator">The separator used between SQL values.</param>
        /// <param name="status_alias">The alias used for the status table.</param>
        /// <returns>A SQL INSERT statement or an empty string when no status values exist.</returns>
        internal static string AsStatusInsertValues<T>(this T entity, string separator = $"\n{TAB}{TAB}{TAB}{TAB}, ", string status_alias = "a") where T : EntityBase
        {
            if (entity.Def.RelatedStatus.Count == 0) return "";

            var sb = new StringBuilder();
            var PKs = entity.Def.Columns.GetWithFlags(ColumnFlags.PK);

            string entity_PK = PKs.AsProcParms(separator);

            var parms = new TemplateValues()
            {
                STATUS_TABLE = $"[{entity.Def.TableName}{TableSuffix.STATUS_TABLE_SUFFIX}]",
                INSERT_VALUES = $"{entity_PK}{separator}{status_alias}.{nameof(StatusValuesDef.c_status_id)}{separator}{status_alias}.{nameof(StatusValuesDef.c_statusvalue_id)}",
                MNEO = $"'{entity.Def.Mneo.SQLEscape()}'"
            };
            sb.Append(Templates.INSERT_STATUS_TEMPLATE.ReplaceTemplate(parms));

            return sb.ToString();
        }

        /// <summary>
        /// Builds a DELETE statement to remove status values for the specified entity.
        /// </summary>
        /// <typeparam name="T">The type of entity definition.</typeparam>
        /// <param name="entity">The entity whose status values will be deleted.</param>
        /// <param name="separator">The separator used between predicate clauses.</param>
        /// <returns>A SQL DELETE statement or an empty string when no status values exist.</returns>
        internal static string AsStatusDelete<T>(this T entity, string separator = $"\n{TAB}{TAB}{TAB}and ") where T : EntityBase
        {
            if (entity.Def.RelatedStatus.Count == 0) return "";

            var sb = new StringBuilder();
            var PKs = entity.Def.Columns.GetWithFlags(ColumnFlags.PK);

            var parms = new TemplateValues()
            {
                STATUS_TABLE = $"{entity.Def.TableName}{TableSuffix.STATUS_TABLE_SUFFIX}",
                WHERE_CLAUSE = PKs.AsColumnValuePairs(union_string: $"\n{TAB}{TAB}{TAB}and ")
            };
            sb.Append(Templates.DELETE_STATUS_TEMPLATE.ReplaceTemplate(parms));

            return sb.ToString();
        }

        /// <summary>
        /// Builds UPDATE statements for status values of the specified entity using template values.
        /// </summary>
        /// <typeparam name="T">The type of entity definition.</typeparam>
        /// <param name="entity">The entity whose status values will be updated.</param>
        /// <param name="union_string">The string used to join WHERE clause conditions.</param>
        /// <param name="separator">The separator used between update values.</param>
        /// <returns>A SQL UPDATE statement or an empty string when no status values exist.</returns>
        internal static string AsStatusUpdateTemplateValues<T>(this T entity, string union_string = $"\n{TAB}{TAB}{TAB}and ", string separator = $"\n{TAB}{TAB}{TAB}, ") where T : EntityBase
        {
            if (entity.Def.RelatedStatus.Count == 0) return "";

            var sb = new StringBuilder();

            var status = new StatusValues();
            var PKs = entity.Def.Columns.GetWithFlags(ColumnFlags.PK);
            var Fakes = entity.Def.Columns.GetWithFlags(ColumnFlags.Fake, ColumnFlags.None);

            foreach (var stat_col in Fakes)
            {
                if (entity.Def.RelatedStatus.Contains(stat_col.RelatedStatusID!) && stat_col.SQLMetadata.IsArray == false)
                {
                    var parms = new TemplateValues()
                    {
                        STATUS_PARM = stat_col.AsProcParm(false),
                        STATUS_TABLE = $"[{entity.Def.TableName}{TableSuffix.STATUS_TABLE_SUFFIX}]",
                        UPDATE_VALUES = $"{status.Def.c_statusvalue_id.Name} = {stat_col.AsProcParm(false)}",
                        WHERE_CLAUSE = $"{PKs.AsColumnValuePairs(union_string: union_string)}{union_string}{status.Def.c_status_id.Name} = '{stat_col.RelatedStatusID!.SQLEscape()}'"
                    };
                    sb.Append(Templates.UPDATE_STATUS_TEMPLATE.ReplaceTemplate(parms));
                }
            }
            return sb.ToString();
        }


    }
}
