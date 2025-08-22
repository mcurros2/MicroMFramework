using MicroM.Data;

namespace MicroM.Extensions
{
    public static class ViewExtensions
    {
        /// <summary>
        /// Converts view parameters to their underlying column definitions.
        /// </summary>
        /// <param name="parms">View parameters.</param>
        /// <returns>Enumeration of column definitions.</returns>
        public static IEnumerable<ColumnBase> ToColumnBaseEnumerable(this Dictionary<string, ViewParm> parms)
        {
            List<ColumnBase> ret = [];

            foreach (var parm in parms.Values)
            {
                ret.Add(parm.Column);
            }

            return ret;
        }

        /// <summary>
        /// Filters a column dictionary by included or excluded names.
        /// </summary>
        /// <param name="parms">Source columns.</param>
        /// <param name="include">Names to include.</param>
        /// <param name="exclude">Names to exclude.</param>
        /// <returns>Filtered column sequence.</returns>
        public static IEnumerable<ColumnBase> FilterByName(this Dictionary<string, ColumnBase> parms, string[]? include = null, string[]? exclude = null)
        {
            List<ColumnBase> ret = [];

            foreach (var parm in parms.Values)
            {
                if(include != null && include.Length > 0)
                {
                    if (include.Contains(parm.Name, StringComparer.OrdinalIgnoreCase)) ret.Add(parm);
                }
                else if(exclude != null && exclude.Length > 0)
                {
                    if (!exclude.Contains(parm.Name, StringComparer.OrdinalIgnoreCase)) ret.Add(parm);
                }
                else
                {
                    ret.Add(parm);
                }
            }

            return ret;
        }

    }
}
