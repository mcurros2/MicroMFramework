using MicroM.Core;
using MicroM.Data;
using MicroM.Extensions;
using MicroM.Generators.Extensions;
using static MicroM.Generators.Constants;

namespace MicroM.Generators.SQLGenerator
{
    internal static class DropExtensions
    {
        internal static List<string> AsCreateIDropProc<T>(this T entity, bool create_or_alter = false, bool force_fake = false) where T : EntityBase
        {
            List<string> scripts = [];
            if (entity.Def.Fake && force_fake == false) return scripts;
            var parms = new TemplateValues(create_or_alter)
            {
                MNEO = entity.Def.Mneo,
                TABLE_NAME = $"[{entity.Def.TableName}]",
                PARMS_DECLARATION = entity.Def.Columns.GetWithFlags(ColumnFlags.Delete).AsProcParmsDeclaration(separator: $"\n{TAB}{TAB}, "),
                PARMS = entity.Def.Columns.GetWithFlags(ColumnFlags.Delete).AsProcParms(separator: $"\n{TAB}{TAB}{TAB}, "),
                WHERE_CLAUSE = entity.Def.Columns.GetWithFlags(ColumnFlags.PK, ColumnFlags.Fake).AsColumnValuePairs(union_string: $"\n{TAB}{TAB}{TAB}and "),
                CATEGORIES_DELETE = entity.AsCategoriesDelete(),
                STATUS_DELETE = entity.AsStatusDelete()
            };

            scripts.Add(Templates.IDROP_TEMPLATE.ReplaceTemplate(parms).RemoveEmptyLines());
            scripts.Add(Templates.DROP_CALLS_IDROP_TEMPLATE.ReplaceTemplate(parms).RemoveEmptyLines());

            return scripts;
        }

        internal static List<string> AsCreateNormalDropProc<T>(this T entity, bool create_or_alter = false, bool force_fake = false) where T : EntityBase
        {
            List<string> scripts = [];
            if (entity.Def.Fake && force_fake == false) return scripts;
            var parms = new TemplateValues(create_or_alter)
            {
                MNEO = entity.Def.Mneo,
                TABLE_NAME = $"[{entity.Def.TableName}]",
                PARMS_DECLARATION = entity.Def.Columns.GetWithFlags(ColumnFlags.Delete).AsProcParmsDeclaration(separator: $"\n{TAB}{TAB}, "),
                PARMS = entity.Def.Columns.GetWithFlags(ColumnFlags.Delete).AsProcParms(separator: $"\n{TAB}{TAB}{TAB}, "),
                WHERE_CLAUSE = entity.Def.Columns.GetWithFlags(ColumnFlags.PK, ColumnFlags.Fake).AsColumnValuePairs(union_string: $"\n{TAB}{TAB}{TAB}and "),
                CATEGORIES_DELETE = entity.AsCategoriesDelete(),
                STATUS_DELETE = entity.AsStatusDelete()
            };


            scripts.Add(Templates.DROP_TEMPLATE.ReplaceTemplate(parms).RemoveEmptyLines());

            return scripts;
        }

        public static List<string> AsCreateDropProc<T>(this T entity, bool create_or_alter = false, bool with_idrop = false, bool force_fake = false) where T : EntityBase
        {
            if (with_idrop)
            {
                return entity.AsCreateIDropProc(create_or_alter, force_fake);
            }
            else
            {
                return entity.AsCreateNormalDropProc(create_or_alter, force_fake);
            }
        }


    }
}
