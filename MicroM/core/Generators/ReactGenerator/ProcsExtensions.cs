using MicroM.Core;
using MicroM.Generators.Extensions;
using System.Globalization;
using System.Text;
using static MicroM.Generators.Constants;

namespace MicroM.Generators.ReactGenerator
{
    public static class ProcsExtensions
    {

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
