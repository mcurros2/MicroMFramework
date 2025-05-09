﻿using MicroM.Core;
using MicroM.Data;
using MicroM.Extensions;
using System.Globalization;
using System.Text;
using static MicroM.Generators.Constants;

namespace MicroM.Generators.SQLGenerator
{
    internal static class JoinExtensions
    {

        private static string AsSQLKeyMappings(this List<BaseColumnMapping> mappings, EntityBase child_entity, string parent_alias = "a", string child_alias = "b", string separator = " and ")
        {
            StringBuilder sb = new();

            if (mappings?.Count > 0)
            {
                using var map_enumerator = mappings.GetEnumerator();
                if (map_enumerator.MoveNext())
                {
                    var map = map_enumerator.Current;
                    sb.Append(CultureInfo.InvariantCulture, $"{child_alias}.{map.ChildColName} = {parent_alias}.{map.ParentColName}");
                    while (map_enumerator.MoveNext())
                    {
                        map = map_enumerator.Current;
                        sb.Append(CultureInfo.InvariantCulture, $"{separator}{child_alias}.{map.ChildColName} = {parent_alias}.{map.ParentColName}");
                    }
                }
            }
            else
            {
                var pks = child_entity.Def.Columns.GetWithFlags(ColumnFlags.PK);
                if (pks?.Count > 0)
                {
                    using var pks_enumerator = pks.GetEnumerator();
                    if (pks_enumerator.MoveNext())
                    {
                        var col = pks_enumerator.Current;
                        sb.Append(CultureInfo.InvariantCulture, $"{child_alias}.{col.Name} = {parent_alias}.{col.Name}");
                        while (pks_enumerator.MoveNext())
                        {
                            col = pks_enumerator.Current;
                            sb.Append(CultureInfo.InvariantCulture, $"{separator}{child_alias}.{col.Name} = {parent_alias}.{col.Name}");
                        }
                    }

                }
            }

            return sb.ToString();
        }

        internal static string AsSQLJoin<T>(this T parent_entity, T child_entity, string join_type = "join", string separator = $"\n{TAB}{TAB}{TAB}", string parent_alias = "a", string child_alias = "b") where T : EntityBase
        {
            StringBuilder sb = new();
            EntityForeignKeyBase? fk = child_entity.Def.GetForeignKey(parent_entity);
            if (fk != null)
            {
                sb.AppendFormat(CultureInfo.InvariantCulture, $"{separator}{join_type} [{child_entity.Def.TableName}] {child_alias}\n");
                sb.AppendFormat(CultureInfo.InvariantCulture, $"{separator}on({fk.KeyMappings.AsSQLKeyMappings(child_entity, parent_alias, child_alias)})");
            }
            return sb.ToString();
        }

        internal static string AsSQLJoinColumnsByName(this CustomOrderedDictionary<ColumnBase>? columns, string union_string = " and ", string parent_alias = "a", string child_alias = "b")
        {
            if (columns == null) return "";
            var sb = new StringBuilder();
            using var cols = columns.GetEnumerator();
            if (cols.MoveNext())
            {
                var col = cols.Current;
                sb.Append(CultureInfo.InvariantCulture, $"{parent_alias}.{col.Name} = {child_alias}.{col.Name}");
                while (cols.MoveNext())
                {
                    col = cols.Current;
                    sb.Append(CultureInfo.InvariantCulture, $"{union_string}{parent_alias}.{col.Name} = {child_alias}.{col.Name}");
                }
            }
            return sb.ToString();
        }


    }
}
