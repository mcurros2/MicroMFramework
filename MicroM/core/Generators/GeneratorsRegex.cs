using System.Text.RegularExpressions;

namespace MicroM.Generators
{
    /// <summary>
    /// Provides reusable regular expressions for generator utilities.
    /// </summary>
    public partial class GeneratorsRegex
    {
        /// <summary>
        /// Matches two or more consecutive empty lines, optionally containing whitespace,
        /// enabling removal of redundant blank lines.
        /// </summary>
        [GeneratedRegex(@"[ \t]*\r?\n(\s*\r?\n)+", RegexOptions.Multiline)]
        public static partial Regex MultipleEmptyLines();

        /// <summary>
        /// Matches positions in a camel-cased identifier where an uppercase letter begins a
        /// new word, allowing strings to be split into their component words.
        /// </summary>
        [GeneratedRegex(@"(?<!^)(?=[A-Z])")]
        public static partial Regex SplitCamelCaseWords();
    }
}
