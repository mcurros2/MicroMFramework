using System.Text;

namespace MicroM.Generators.Extensions
{
    /// <summary>
    /// Provides utility methods for processing string templates.
    /// </summary>
    internal static class TemplateExtensions
    {
        /// <summary>
        /// Replaces template tokens with the corresponding values supplied in <paramref name="values"/>.
        /// </summary>
        /// <param name="template">The template string containing tokens enclosed in braces.</param>
        /// <param name="values">The collection of token-value pairs used for replacement.</param>
        /// <returns>The template string with all tokens replaced by their matching values.</returns>
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
