using MicroM.Core;
using MicroM.Data;

namespace MicroM.Extensions
{
    public static class ProcedureExtensions
    {
        /// <summary>
        /// Sets procedure parameter values from the provided dictionary.
        /// </summary>
        /// <param name="proc">Procedure definition.</param>
        /// <param name="values">Parameter values keyed by name.</param>
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
        /// <summary>
        /// Sets procedure parameter values from a column collection.
        /// </summary>
        /// <param name="proc">Procedure definition.</param>
        /// <param name="cols">Source column values.</param>
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
