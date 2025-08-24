using MicroM.Core;
using MicroM.Data;

namespace MicroM.Extensions
{
    public static class ProcedureExtensions
    {
        public static void SetParmsValues(this ProcedureDefinition proc, Dictionary<string, object> values)
        {
            foreach (var key in values.Keys)
            {
                if (proc.Parms.TryGetValue(key, out ColumnBase? col))
                {
                    col.ValueObject = values[key];
                }
            }
        }
        public static void SetParmsValues(this ProcedureDefinition proc, IReadonlyOrderedDictionary<ColumnBase> cols)
        {
            foreach (var key in cols.Keys)
            {
                if (proc.Parms.TryGetValue(key, out ColumnBase? col))
                {
                    col.ValueObject = cols[key]!.ValueObject;
                }
            }
        }

    }
}
