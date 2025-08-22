using MicroM.Core;
using MicroM.Data;
using MicroM.Extensions;
using MicroM.Generators.Extensions;
using static MicroM.Generators.Constants;

namespace MicroM.Generators.SQLGenerator
{
    internal static class ViewExtensions
    {
        public static string AsCreateViewProc<T>(this T entity, bool create_or_alter = false, bool force = false) where T : EntityBase
        {
            if (entity.Def.Fake && force == false) return "";
            if (!entity.Def.Views.ContainsKey($"{entity.Def.Mneo}_brwStandard"))
            {
                return "-- No standard view defined";
            }
            var likes = entity.Def.Columns.GetWithFlags(ColumnFlags.All, ColumnFlags.Fake, DefaultColumns.SystemNames);

            // MMC: parameters for the proc should contain the PKs used to filter results
            var view = entity.Def.Views[$"{entity.Def.Mneo}_brwStandard"];
            var parms = view.Proc.Parms;
            // MMC: take the PKs for the entity
            var pks = entity.Def.Columns.GetWithFlags(ColumnFlags.PK, ColumnFlags.Fake);

            // MMC: the filter part of the where clause should contain PKs defined as parameters for the view
            // except the one defined a browsing key.
            CustomOrderedDictionary<ColumnBase> where = new();
            foreach (var pk in pks)
            {
                if (parms.ContainsKey(pk.Name))
                {
                    if (pk.Name.IsIn(SystemViewParmNames.like, SystemViewParmNames.d)) continue;
                    if (view.BrowsingKeyParm.Column.Name == pk.Name) continue;
                    where.Add(pk.Name, pk);
                }
            }

            // MMC: For the view columns exclude all pks except the browsing key
            var insert_cols = entity.Def.Columns.GetWithFlags(ColumnFlags.Insert, exclude_names: [nameof(DefaultColumns.dt_lu)]);
            CustomOrderedDictionary<ColumnBase> viewcols = new();
            foreach (var col in insert_cols)
            {
                if (col.ColumnMetadata.HasFlag(ColumnFlags.PK) && col.Name != view.BrowsingKeyParm.Column.Name) continue;
                viewcols.Add(col.Name, col);
            }

            // MMC: for tables where are all pks, leave all insert columns
            if (viewcols.Count == 0)
            {
                viewcols = entity.Def.Columns.GetWithFlags(ColumnFlags.Insert, exclude_names: [nameof(DefaultColumns.dt_lu)]);
            }

            string like_template = "";
            // MMC: like template
            if (likes.Count > 0)
            {
                var lparms = new TemplateValues()
                {
                    LIKE_CLAUSE = $"{likes.AsLikeValuePairs(alias: "a", union_string: $"\n{TAB}{TAB}{TAB}{TAB}or ")}"
                };
                like_template = Templates.LIKE_TEMPLATE.ReplaceTemplate(lparms);
            }

            var tparms = new TemplateValues(create_or_alter)
            {
                MNEO = entity.Def.Mneo,
                TABLE_NAME = $"[{entity.Def.TableName}] a",
                PARMS_DECLARATION = parms.AsProcParmsDeclaration(separator: $"\n{TAB}{TAB}, "),
                WHERE_CLAUSE = $"{(where?.Count > 0 ? where.Values.AsColumnValuePairs(alias: "a", union_string: $"\n{TAB}{TAB}and ") : "")}{(where?.Count > 0 && likes.Count > 0 ? $"\n{TAB}{TAB}and\n{TAB}{TAB}" : "")}{like_template}",
                VIEW_COLUMNS = viewcols.AsTitleColumnPairs(alias: "a", union_string: $"\n{TAB}{TAB}, ")
            };
            return Templates.VIEW_TEMPLATE.ReplaceTemplate(tparms).RemoveEmptyLines();
        }


    }
}
