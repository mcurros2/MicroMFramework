using MicroM.Data;
using System.Globalization;
using System.Text;
using static MicroM.Generators.Constants;

namespace MicroM.Generators.ReactGenerator
{
    /// <summary>
    /// Extension methods for generating view related TypeScript code for
    /// the React client.
    /// </summary>
    public static class ViewExtensions
    {
        /// <summary>
        /// Creates the key mapping portion of a view definition.
        /// </summary>
        /// <param name="view">View definition to translate.</param>
        /// <returns>A comma separated list of key mappings.</returns>
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
        /// Builds the TypeScript object that defines the entity views.
        /// </summary>
        /// <param name="views">Collection of view definitions.</param>
        /// <param name="indent">Indentation used in the generated template.</param>
        /// <returns>TypeScript code describing the views.</returns>
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
