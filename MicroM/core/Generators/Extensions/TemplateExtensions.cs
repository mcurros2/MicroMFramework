using System.Text;

namespace MicroM.Generators.Extensions
{
    /// <summary>
    /// Provides helper methods for working with string templates by replacing
    /// placeholder tokens with their corresponding values.
    /// </summary>
    internal static class TemplateExtensions
    {
        /// <summary>
        /// Replaces token placeholders within the <paramref name="template"/> string
        /// using values from the provided <paramref name="values"/> collection.
        /// </summary>
        /// <param name="template">The template string containing token placeholders.</param>
        /// <param name="values">The token/value pairs used to replace placeholders.</param>
        /// <returns>The template string with placeholders replaced by their values.</returns>
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
