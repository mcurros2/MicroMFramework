﻿using MicroM.Core;
using MicroM.Data;
using MicroM.DataDictionary;
using MicroM.DataDictionary.Configuration;
using MicroM.Extensions;
using MicroM.Generators.Extensions;
using System.Text;
using static MicroM.Generators.Constants;

namespace MicroM.Generators.SQLGenerator
{
    internal static class StatusExtensions
    {
        /// <summary>
        /// Returns a two SQL scripts with the DDL to create a child status table and fk index to store related categories for an <see cref="Entity{TDefinition}"/> record.
        /// The status table name for the specified <seealso cref="Entity{TDefinition}"/> will be <![CDATA[<entity table name>_status]]>.
        /// The columns will contain the <see cref="Entity{TDefinition}"/> primary keys + <seealso cref="Status"/> primary keys + <see cref="DefaultColumns"/>
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="entity"></param>
        /// <returns></returns>
        internal static List<string> CreateStatusTable<T>(this T entity) where T : EntityBase
        {
            return entity.CreateCategoryOrStatusTable(true);
        }

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
