using MicroM.Core;
using MicroM.Generators.Extensions;
using System.Globalization;
using System.Text;
using static MicroM.Generators.Constants;

namespace MicroM.Generators.ReactGenerator
{
    /// <summary>
    /// Extension methods used by the React generator to produce procedure definitions for an entity.
    /// Each procedure is represented as an entry in a JavaScript object that exposes the procedure name.
    /// </summary>
    public static class ProcsExtensions
    {

        /// <summary>
        /// Constructs the procedure definition block for an entity.
        /// The method enumerates <see cref="EntityDefinition.Procs"/> and builds a JavaScript object where
        /// each procedure is mapped to an object containing its name.  The resulting object is then
        /// inserted into the <c>ENTITY_PROCS_TEMPLATE</c> using <see cref="TemplateValues"/>.
        /// </summary>
        /// <param name="def">Entity definition containing procedure information.</param>
        /// <param name="indent">Indentation to prepend to each generated line.</param>
        /// <returns>
        /// A string containing the formatted procedure definitions or an empty string when no procedures exist.
        /// </returns>
        public static string AsProcsDefinition(this EntityDefinition def, string indent = $"{TAB}")
        {
            if(def.Procs.Count == 0) return "";
            
            var procs_enumerator = def.Procs.GetEnumerator();

            StringBuilder sb = new();

            if (procs_enumerator.MoveNext())
            {
                var proc = procs_enumerator.Current.Value;
                sb.Append(CultureInfo.InvariantCulture, $"{{\n{indent}{TAB}{proc.Name}: {{ name: '{proc.Name}' }}");

                while (procs_enumerator.MoveNext())
                {
                    proc = procs_enumerator.Current.Value;
                    sb.Append(CultureInfo.InvariantCulture, $",\n{indent}{TAB}{proc.Name}: {{ name: '{proc.Name}' }}");
                }

                sb.Append(CultureInfo.InvariantCulture, $"\n{indent}}}");
            }

            var parms = new TemplateValues()
            {
                PROCS_DEFINITION = sb.ToString()
            };

            return Templates.ENTITY_PROCS_TEMPLATE.ReplaceTemplate(parms);
        }

    }
}
