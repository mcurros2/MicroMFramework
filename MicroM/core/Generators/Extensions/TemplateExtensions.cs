using System.Text;

namespace MicroM.Generators.Extensions
{
    internal static class TemplateExtensions
    {
        internal static string ReplaceTemplate(this string template, TemplateValuesBase values)
        {
            var sb = new StringBuilder(template);
            foreach (var pair in values.tokens)
            {
                sb.Replace($"{{{pair.Key}}}", pair.Value);
            }
            return sb.ToString();
        }

    }
}
