using MicroM.Core;
using MicroM.Data;
using MicroM.Extensions;
using MicroM.Generators.Extensions;
using static MicroM.Generators.Constants;

namespace MicroM.Generators.SQLGenerator
{
    /// <summary>
    /// Extension methods that generate SQL scripts for drop stored procedures.
    /// </summary>
    internal static class DropExtensions
    {
        /// <summary>
        /// Builds scripts for an internal drop procedure and its caller for the specified entity.
        /// </summary>
        /// <typeparam name="T">Type of entity to drop.</typeparam>
        /// <param name="entity">Entity definition used to generate the scripts.</param>
        /// <param name="create_or_alter">Whether to emit a CREATE or CREATE OR ALTER statement.</param>
        /// <param name="force_fake">Generate scripts even for entities marked as fake.</param>
        /// <returns>SQL scripts for the internal drop procedure and wrapper.</returns>
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

        /// <summary>
        /// Builds scripts for a standard drop procedure for the specified entity.
        /// </summary>
        /// <typeparam name="T">Type of entity to drop.</typeparam>
        /// <param name="entity">Entity definition used to generate the scripts.</param>
        /// <param name="create_or_alter">Whether to emit a CREATE or CREATE OR ALTER statement.</param>
        /// <param name="force_fake">Generate scripts even for entities marked as fake.</param>
        /// <returns>SQL scripts for the drop procedure.</returns>
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

        /// <summary>
        /// Builds drop procedure scripts, optionally generating the internal drop variant.
        /// </summary>
        /// <typeparam name="T">Type of entity to drop.</typeparam>
        /// <param name="entity">Entity definition used to generate the scripts.</param>
        /// <param name="create_or_alter">Whether to emit a CREATE or CREATE OR ALTER statement.</param>
        /// <param name="with_idrop">Generate internal drop scripts if set to <c>true</c>.</param>
        /// <param name="force_fake">Generate scripts even for entities marked as fake.</param>
        /// <returns>SQL scripts for the drop procedure.</returns>
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
