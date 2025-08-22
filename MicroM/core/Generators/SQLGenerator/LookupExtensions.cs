using MicroM.Core;
using MicroM.Data;
using MicroM.Extensions;
using MicroM.Generators.Extensions;
using static MicroM.Generators.Constants;

namespace MicroM.Generators.SQLGenerator
{
    /// <summary>
    /// Extension methods for generating lookup stored procedures.
    /// </summary>
    internal static class LookupExtensions
    {
        /// <summary>
        /// Builds the SQL script for the <c>_lookup</c> stored procedure used to
        /// return a description column for a record.
        /// </summary>
        /// <typeparam name="T">Entity type.</typeparam>
        /// <param name="entity">Entity definition.</param>
        /// <param name="create_or_alter">True to emit <c>create or alter</c>.</param>
        /// <param name="force">Generate script even if entity is marked fake.</param>
        /// <returns>SQL script for the lookup procedure or an empty string.</returns>
        public static string AsCreateLookupProc<T>(this T entity, bool create_or_alter = false, bool force = false) where T : EntityBase
        {
            if (entity.Def.Fake && force == false) return "";
            var parms = new TemplateValues(create_or_alter)
            {
                MNEO = entity.Def.Mneo,
                TABLE_NAME = $"[{entity.Def.TableName}] a",
                PARMS_DECLARATION = entity.Def.Columns.GetWithFlags(ColumnFlags.Get).AsProcParmsDeclaration(separator: $"\n{TAB}{TAB}, "),
                WHERE_CLAUSE = entity.Def.Columns.GetWithFlags(ColumnFlags.PK, ColumnFlags.Fake).AsColumnValuePairs(alias: "a", union_string: $"\n{TAB}{TAB}and "),
                DESC_COLUMN = entity.Def.Columns.GetDescriptionColumn(alias: "a")
            };
            return Templates.LOOKUP_TEMPLATE.ReplaceTemplate(parms).RemoveEmptyLines();
        }


    }
}
