using MicroM.Core;
using MicroM.Data;
using MicroM.Extensions;
using MicroM.Generators.Extensions;
using static MicroM.Generators.Constants;

namespace MicroM.Generators.SQLGenerator
{
    /// <summary>
    /// Extension helpers for generating DROP stored procedures.
    /// </summary>
    internal static class DropExtensions
    {
        /// <summary>
        /// Creates scripts for the transactional <c>_idrop</c> procedure and a wrapper <c>_drop</c>
        /// that invokes it.
        /// </summary>
        /// <typeparam name="T">Entity type.</typeparam>
        /// <param name="entity">Entity definition.</param>
        /// <param name="create_or_alter">True to generate <c>create or alter</c> header.</param>
        /// <param name="force_fake">Generate code even if the entity is marked as fake.</param>
        /// <returns>Scripts for the <c>_idrop</c> and calling <c>_drop</c> procedures.</returns>
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
        /// Generates the basic <c>_drop</c> procedure that removes a record and
        /// its related category/status rows.
        /// </summary>
        /// <typeparam name="T">Entity type.</typeparam>
        /// <param name="entity">Entity definition.</param>
        /// <param name="create_or_alter">True to generate <c>create or alter</c> header.</param>
        /// <param name="force_fake">Generate code even if the entity is marked as fake.</param>
        /// <returns>Script for the <c>_drop</c> stored procedure.</returns>
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
        /// Returns the scripts required to create the drop stored procedure for
        /// an entity, optionally including the transactional variant.
        /// </summary>
        /// <typeparam name="T">Entity type.</typeparam>
        /// <param name="entity">Entity definition.</param>
        /// <param name="create_or_alter">True to generate <c>create or alter</c> header.</param>
        /// <param name="with_idrop">Include <c>_idrop</c> procedure if true.</param>
        /// <param name="force_fake">Generate code even if entity is marked as fake.</param>
        /// <returns>One or more SQL scripts implementing drop logic.</returns>
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
