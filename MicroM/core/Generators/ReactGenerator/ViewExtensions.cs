using MicroM.Data;
using System.Globalization;
using System.Text;
using static MicroM.Generators.Constants;

namespace MicroM.Generators.ReactGenerator
{
    /// <summary>
    /// Extension methods that convert <see cref="ViewDefinition"/> instances into
    /// TypeScript-friendly structures used by the React generator.
    /// </summary>
    public static class ViewExtensions
    {
        /// <summary>
        /// Builds a comma-separated list of parameter to column index mappings for a view.
        /// </summary>
        /// <param name="view">The view definition whose parameter mappings are being generated.</param>
        /// <returns>A string representing the mappings in <c>columnName: index</c> format.</returns>
        internal static string AsKeyMappings(this ViewDefinition view)
        {
            var parms_enumerator = view.Parms.Values.GetEnumerator();
            StringBuilder sb = new();

            bool add_comma = false;
            while (parms_enumerator.MoveNext())
            {
                var parm = parms_enumerator.Current;
                if (parm.ColumnMapping != -1)
                {
                    sb.Append(CultureInfo.InvariantCulture, $"{(add_comma ? ", " : "")}{parm.Column.Name}: {parm.ColumnMapping}");
                    add_comma = true;
                }
            }

            return sb.ToString();
        }

        /// <summary>
        /// Produces a formatted views definition block for the provided views collection.
        /// </summary>
        /// <param name="views">The set of views to include in the definition.</param>
        /// <param name="indent">The indentation string to use for generated lines.</param>
        /// <returns>A string containing the views definition formatted for TypeScript output.</returns>
        public static string AsViewsDefinition(this IReadOnlyDictionary<string, ViewDefinition> views, string indent = $"{TAB}")
        {
            IEnumerator<ViewDefinition> views_enumerator = views.Values.GetEnumerator();
            StringBuilder sb = new();

            if (views_enumerator.MoveNext())
            {
                var view = views_enumerator.Current;
                sb.Append('{');

                sb.Append(CultureInfo.InvariantCulture, $"\n{indent}    {view.Proc.Name}: {{ name: '{view.Proc.Name}', keyMappings: {{ {view.AsKeyMappings()} }} }}");

                while (views_enumerator.MoveNext())
                {
                    view = views_enumerator.Current;
                    sb.Append(CultureInfo.InvariantCulture, $",\n{indent}    {view.Proc.Name}: {{ name: '{view.Proc.Name}', keyMappings: {{ {view.AsKeyMappings()} }} }}");
                }

                sb.Append(CultureInfo.InvariantCulture, $"\n{indent}}}");
            }

            return sb.ToString();
        }


    }
}
