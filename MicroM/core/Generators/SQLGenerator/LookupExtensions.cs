using MicroM.Core;
using MicroM.Data;
using MicroM.Extensions;
using MicroM.Generators.Extensions;
using static MicroM.Generators.Constants;

namespace MicroM.Generators.SQLGenerator
{
    internal static class LookupExtensions
    {
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
