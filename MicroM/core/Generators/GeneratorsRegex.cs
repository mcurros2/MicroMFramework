using System.Text.RegularExpressions;

namespace MicroM.Generators
{
    /// <summary>
    /// Provides commonly used regular expressions for code generation tasks.
    /// </summary>
    public partial class GeneratorsRegex
    {
        /// <summary>
        /// Matches occurrences of multiple consecutive empty lines.
        /// </summary>
        /// <returns>A <see cref="Regex"/> that identifies multiple empty lines.</returns>
        [GeneratedRegex(@"[ \t]*\r?\n(\s*\r?\n)+", RegexOptions.Multiline)]
        public static partial Regex MultipleEmptyLines();

        /// <summary>
        /// Splits camel case formatted strings into individual words.
        /// </summary>
        /// <returns>A <see cref="Regex"/> used to locate camel case word boundaries.</returns>
        [GeneratedRegex(@"(?<!^)(?=[A-Z])")]
        public static partial Regex SplitCamelCaseWords();
    }
}
