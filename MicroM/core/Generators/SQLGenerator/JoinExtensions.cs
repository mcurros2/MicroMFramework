using MicroM.Core;
using MicroM.Data;
using MicroM.Extensions;
using System.Globalization;
using System.Text;
using static MicroM.Generators.Constants;

namespace MicroM.Generators.SQLGenerator
{
    /// <summary>
    /// Helper methods for building SQL JOIN clauses and key mapping expressions
    /// between related <see cref="EntityBase"/> instances.
    /// </summary>
    internal static class JoinExtensions
    {

        /// <summary>
        /// Builds a SQL expression mapping the key columns from a child entity to
        /// the corresponding columns of its parent entity.
        /// When explicit mappings are not supplied the child's primary keys are used.
        /// </summary>
        /// <param name="mappings">Explicit parent/child column mappings.</param>
        /// <param name="child_entity">Child entity whose keys are being joined.</param>
        /// <param name="parent_alias">SQL alias for the parent table.</param>
        /// <param name="child_alias">SQL alias for the child table.</param>
        /// <param name="separator">String used to separate multiple comparisons.</param>
        /// <returns>SQL equality expressions joining parent and child keys.</returns>
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

        /// <summary>
        /// Generates a SQL <c>JOIN</c> clause linking a child entity to its parent
        /// using the foreign key relationship defined between them.
        /// </summary>
        /// <typeparam name="T">Type of the entities being joined.</typeparam>
        /// <param name="parent_entity">The parent entity.</param>
        /// <param name="child_entity">The child entity to join.</param>
        /// <param name="join_type">SQL join type (e.g. "join", "left join").</param>
        /// <param name="separator">Formatting separator inserted before each line.</param>
        /// <param name="parent_alias">Alias for the parent table.</param>
        /// <param name="child_alias">Alias for the child table.</param>
        /// <returns>Formatted SQL JOIN string or an empty string if no relationship exists.</returns>
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

        /// <summary>
        /// Builds SQL equality comparisons for columns that have the same name in
        /// both the parent and child entities.
        /// </summary>
        /// <param name="columns">Collection of columns to compare.</param>
        /// <param name="union_string">Separator placed between comparisons.</param>
        /// <param name="parent_alias">Alias for the parent table.</param>
        /// <param name="child_alias">Alias for the child table.</param>
        /// <returns>A SQL string joining columns by matching name.</returns>
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
