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
        /// Generates the SQL script to create or alter a lookup stored procedure for the specified entity.
        /// </summary>
        /// <typeparam name="T">The type of the entity used to build the lookup stored procedure.</typeparam>
        /// <param name="entity">The entity instance used for generating the lookup stored procedure.</param>
        /// <param name="create_or_alter">If <c>true</c>, the script creates or alters the procedure; otherwise it only creates it.</param>
        /// <param name="force">If <c>true</c>, generates the procedure even if the entity is marked as fake.</param>
        /// <returns>The SQL script for the lookup stored procedure.</returns>
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
