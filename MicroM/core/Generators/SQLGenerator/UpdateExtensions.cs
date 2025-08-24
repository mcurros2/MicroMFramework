using MicroM.Core;
using MicroM.Data;
using MicroM.Extensions;
using MicroM.Generators.Extensions;
using System.Data;
using static MicroM.Generators.Constants;

namespace MicroM.Generators.SQLGenerator
{
    internal static class UpdateExtensions
    {
        private static string GetAutonum(this EntityBase entity)
        {
            string ret = "";
            if (entity.Def.AutonumColumn != null)
            {
                ret = $"declare @id bigint\n{TAB}{TAB}exec num_iGetNewNumber '{entity.Def.Mneo}', @nextnumber = @id out";
                if (entity.Def.AutonumColumn.SQLMetadata.SQLType.IsIn(SqlDbType.Char, SqlDbType.NChar))
                {
                    ret = $"{ret}\n{TAB}{TAB}select @{entity.Def.AutonumColumn.SQLParameterName} = right('0000000000'+rtrim(@id),10)\n";
                }
                else
                {
                    ret = $"{ret}\n{TAB}{TAB}select @{entity.Def.AutonumColumn.SQLParameterName} = @id\n";
                }
            }

            return ret;
        }

        private static string GetAutonumReturn(this EntityBase entity, bool with_iupdate = false)
        {
            string result_prefix = "", msg_prefix = "";

            if (with_iupdate)
            {
                result_prefix = "@result = ";
                msg_prefix = "@msg = ";
            }

            return entity.Def.AutonumColumn != null ? $"select{TAB}{result_prefix}15, {msg_prefix}rtrim(@{entity.Def.AutonumColumn.SQLParameterName})" : $"select{TAB}{result_prefix}0, {msg_prefix}'OK'";
        }


        internal static string GetUpdateProc<T>(this T entity, bool create_or_alter = false, bool force = false) where T : EntityBase
        {
            if (entity.Def.Fake && force == false) return "";

            var ins_cols = entity.Def.Columns.GetWithFlags(ColumnFlags.Insert, exclude_names: [SystemColumnNames.dt_lu, SystemColumnNames.webusr]);
            var upd_cols = entity.Def.Columns.GetWithFlags(ColumnFlags.Update, exclude_flags: ColumnFlags.PK | ColumnFlags.Fake, exclude_names: [SystemColumnNames.dt_lu, SystemColumnNames.webusr]);
            string autonum = entity.GetAutonum();
            string autonum_return = entity.GetAutonumReturn(false);

            string where_clause = entity.Def.Columns.GetWithFlags(ColumnFlags.PK).AsColumnValuePairs(union_string: $"\n{TAB}{TAB}{TAB}and ");
            string table_name = $"[{entity.Def.TableName}]";
            string categories_update = entity.AsCategoriesUpdateTemplateValues();
            string status_update = entity.AsStatusUpdateTemplateValues();

            string update_clause = "";
            if (upd_cols.Count > 0)
            {
                var update_parms = new TemplateValues()
                {
                    TABLE_NAME = table_name,
                    WHERE_CLAUSE = where_clause,
                    UPDATE_VALUES = upd_cols.AsColumnValuePairs(union_string: $"\n{TAB}{TAB}{TAB}, "),
                };
                update_clause = Templates.UPDATE_CLAUSE_TEMPLATE.ReplaceTemplate(update_parms);
            }

            var parms = new TemplateValues(create_or_alter)
            {
                MNEO = entity.Def.Mneo,
                TABLE_NAME = table_name,
                PARMS_DECLARATION = entity.Def.Columns.GetWithFlags(ColumnFlags.Insert | ColumnFlags.Update, ColumnFlags.None).AsProcParmsDeclaration(separator: $"\n{TAB}{TAB}, "),
                PARMS_VALIDATION = entity.Def.Columns.GetWithFlags(ColumnFlags.PK | ColumnFlags.FK).AsValidateNotNullOrEmptyParm(),
                WHERE_CLAUSE = where_clause,
                INSERT_VALUES = ins_cols.AsProcParms(separator: $"\n{TAB}{TAB}{TAB}, "),
                AUTONUM = autonum,

                NULLIF_CHECKS = entity.Def.Columns.GetWithFlags(ColumnFlags.Insert | ColumnFlags.Update, ColumnFlags.None).AsNullIfChecks(),

                JSON_CATEGORIES = entity.AsJSONCategories(),
                JSON_CATEGORIES_INSERT = entity.AsInsertJSONCategories(),
                JSON_CATEGORIES_UPDATE = entity.AsUpdateJSONCategories(),

                CATEGORIES_INSERT = entity.AsCategoriesInsertValues(),
                CATEGORIES_UPDATE = categories_update,
                STATUS_INSERT = entity.AsStatusInsertValues(),
                STATUS_UPDATE = status_update,
                AUTONUM_RETURN = autonum_return,
                UPDATE_LU_CONTROL = (upd_cols.Count > 0 || !string.IsNullOrEmpty(categories_update)) ? Templates.UPDATE_LU_CONTROL_TEMPLATE : "",
                UPDATE_CLAUSE = update_clause
            };

            return Templates.UPDATE_TEMPLATE.ReplaceTemplate(parms).RemoveEmptyLines();
        }

        internal static string GetIUpdateProc<T>(this T entity, bool create_or_alter = false, bool force = false) where T : EntityBase
        {
            if (entity.Def.Fake && force == false) return "";

            var ins_cols = entity.Def.Columns.GetWithFlags(ColumnFlags.Insert, exclude_names: [SystemColumnNames.dt_lu, SystemColumnNames.webusr]);
            var upd_cols = entity.Def.Columns.GetWithFlags(ColumnFlags.Update, exclude_flags: ColumnFlags.PK | ColumnFlags.Fake, exclude_names: [SystemColumnNames.dt_lu, SystemColumnNames.webusr]);
            string autonum = entity.GetAutonum();
            string autonum_return = entity.GetAutonumReturn(true);

            string where_clause = entity.Def.Columns.GetWithFlags(ColumnFlags.PK).AsColumnValuePairs(union_string: $"\n{TAB}{TAB}{TAB}and ");
            string table_name = $"[{entity.Def.TableName}]";
            string categories_update = entity.AsCategoriesUpdateTemplateValues();
            string status_update = entity.AsStatusUpdateTemplateValues();

            string update_clause = "";
            if (upd_cols.Count > 0)
            {
                var update_parms = new TemplateValues()
                {
                    TABLE_NAME = table_name,
                    WHERE_CLAUSE = where_clause,
                    UPDATE_VALUES = upd_cols.AsColumnValuePairs(union_string: $"\n{TAB}{TAB}{TAB}, "),
                };
                update_clause = Templates.UPDATE_CLAUSE_TEMPLATE.ReplaceTemplate(update_parms);
            }


            var parms = new TemplateValues(create_or_alter)
            {
                MNEO = entity.Def.Mneo,
                TABLE_NAME = table_name,
                PARMS_DECLARATION = entity.Def.Columns.GetWithFlags(ColumnFlags.Insert | ColumnFlags.Update, ColumnFlags.None).AsProcParmsDeclaration(separator: $"\n{TAB}{TAB}, "),
                WHERE_CLAUSE = where_clause,
                INSERT_VALUES = ins_cols.AsProcParms(separator: $"\n{TAB}{TAB}{TAB}, "),
                AUTONUM = autonum,

                JSON_CATEGORIES = entity.AsJSONCategories(),
                JSON_CATEGORIES_INSERT = entity.AsInsertJSONCategories(),
                JSON_CATEGORIES_UPDATE = entity.AsUpdateJSONCategories(),

                CATEGORIES_INSERT = entity.AsCategoriesInsertValues(),
                CATEGORIES_UPDATE = categories_update,
                STATUS_INSERT = entity.AsStatusInsertValues(),
                STATUS_UPDATE = status_update,

                AUTONUM_RETURN = autonum_return,
                UPDATE_LU_CONTROL = (upd_cols.Count > 0 || !string.IsNullOrEmpty(categories_update)) ? Templates.IUPDATE_LU_CONTROL_TEMPLATE : "",
                UPDATE_CLAUSE = update_clause
            };


            return Templates.IUPDATE_TEMPLATE.ReplaceTemplate(parms).RemoveEmptyLines();
        }


        internal static string GetUpdateForIUpdateProc<T>(this T entity, bool create_or_alter = false, bool force = false) where T : EntityBase
        {
            if (entity.Def.Fake && force == false) return "";

            var parms = new TemplateValues(create_or_alter)
            {
                MNEO = entity.Def.Mneo,
                PARMS_DECLARATION = entity.Def.Columns.GetWithFlags(ColumnFlags.Insert | ColumnFlags.Update, ColumnFlags.None).AsProcParmsDeclaration(separator: $"\n{TAB}{TAB}, "),
                PARMS = entity.Def.Columns.GetWithFlags(ColumnFlags.Insert | ColumnFlags.Update, ColumnFlags.None).AsProcParms(separator: $"\n{TAB}{TAB}{TAB}, "),
                PARMS_VALIDATION = entity.Def.Columns.GetWithFlags(ColumnFlags.PK | ColumnFlags.FK).AsValidateNotNullOrEmptyParm(),
                NULLIF_CHECKS = entity.Def.Columns.GetWithFlags(ColumnFlags.Insert | ColumnFlags.Update, ColumnFlags.None).AsNullIfChecks(),

            };

            return Templates.UPDATE_CALLS_IUPDATE_TEMPLATE.ReplaceTemplate(parms).RemoveEmptyLines();
        }

        /// <summary>
        /// Returns a SQL script to create the default _update stored procedure for the specified <see cref="Entity{TDefinition}"/>.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="entity"></param>
        /// <param name="create_or_alter"></param>
        /// <param name="with_iupdate"></param>
        /// <param name="force"></param>
        /// <returns></returns>
        public static List<string> AsCreateUpdateProc<T>(this T entity, bool create_or_alter = false, bool with_iupdate = false, bool force = false) where T : EntityBase
        {
            List<string> scripts = [];
            if (entity.Def.Fake && force == false) return scripts;

            if (with_iupdate)
            {
                scripts.Add(entity.GetIUpdateProc(create_or_alter, force: force));
                scripts.Add(entity.GetUpdateForIUpdateProc(create_or_alter, force: force));
            }
            else
            {
                scripts.Add(entity.GetUpdateProc(create_or_alter, force: force));
            }

            return scripts;
        }


    }
}
