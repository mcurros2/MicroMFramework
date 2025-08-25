using MicroM.Core;
using MicroM.Data;
using MicroM.Extensions;
using MicroM.Generators.Extensions;
using static MicroM.Generators.Constants;

namespace MicroM.Generators.SQLGenerator
{
    /// <summary>
    /// Provides extension methods for generating SQL GET stored procedures.
    /// </summary>
    internal static class GetExtensions
    {
        /// <summary>
        /// Generates a SQL script for creating a GET stored procedure for the given entity.
        /// </summary>
        /// <typeparam name="T">Type of the entity.</typeparam>
        /// <param name="entity">Entity definition used to create the stored procedure.</param>
        /// <param name="create_or_alter">Indicates whether to CREATE or ALTER the procedure.</param>
        /// <param name="force">Force generation even if the entity is marked as fake.</param>
        /// <returns>The SQL script for the GET stored procedure, or an empty string if not generated.</returns>
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
