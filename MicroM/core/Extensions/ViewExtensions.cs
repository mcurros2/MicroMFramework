using MicroM.Data;

namespace MicroM.Extensions
{
    public static class ViewExtensions
    {
        public static IEnumerable<ColumnBase> ToColumnBaseEnumerable(this Dictionary<string, ViewParm> parms)
        {
            List<ColumnBase> ret = [];

            foreach (var parm in parms.Values)
            {
                ret.Add(parm.Column);
            }

            return ret;
        }

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
