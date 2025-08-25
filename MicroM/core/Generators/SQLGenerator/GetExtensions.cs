using MicroM.Core;
using MicroM.Data;
using MicroM.Extensions;
using MicroM.Generators.Extensions;
using static MicroM.Generators.Constants;

namespace MicroM.Generators.SQLGenerator
{
    /// <summary>
    /// Extensions for generating <c>_get</c> stored procedures that retrieve
    /// complete entity records.
    /// </summary>
    internal static class GetExtensions
    {
        /// <summary>
        /// Builds the SQL script for the <c>_get</c> procedure of an entity.
        /// </summary>
        /// <typeparam name="T">Entity type.</typeparam>
        /// <param name="entity">Entity definition.</param>
        /// <param name="create_or_alter">True to emit <c>create or alter</c> header.</param>
        /// <param name="force">Generate script even if entity is marked fake.</param>
        /// <returns>SQL script or empty string.</returns>
        public static string AsCreateGetProc<T>(this T entity, bool create_or_alter = false, bool force = false) where T : EntityBase
        {
            if (entity.Def.Fake && force == false) return "";

            string categories_join = entity.AsCategoriesAndStatusJoin();
            string json_categories_get = entity.AsJSONCategoriesGet();
            string json_parms_declaration = entity.AsJSONCategoriesGetParmsDeclaration();

            var parms = new TemplateValues(create_or_alter)
            {
                MNEO = entity.Def.Mneo,
                TABLE_NAME = $"[{entity.Def.TableName}] a",
                PARMS_DECLARATION = entity.Def.Columns.GetWithFlags(ColumnFlags.Get, exclude_flags: ColumnFlags.None).AsProcParmsDeclaration(separator: $"\n{TAB}{TAB}, "),
                CATEGORIES_JOIN = categories_join,
                JSON_PARMS_DECLARATION = json_parms_declaration,
                JSON_CATEGORIES_GET = json_categories_get,
                GET_VALUES = entity.Def.Columns.GetWithFlags(ColumnFlags.All, ColumnFlags.None, [nameof(DefaultColumns.webusr)]).AsProcColumns(separator: $"\n{TAB}{TAB}, ", alias: "a", cat_alias: "b"),
                WHERE_CLAUSE = entity.Def.Columns.GetWithFlags(ColumnFlags.PK, ColumnFlags.Fake).AsColumnValuePairs(alias: "a", union_string: $"\n{TAB}{TAB}and "),
            };
            return Templates.GET_TEMPLATE.ReplaceTemplate(parms).RemoveEmptyLines();
        }

    }
}
