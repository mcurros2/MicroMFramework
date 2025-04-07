using System.Text.RegularExpressions;

namespace MicroM.Generators
{
    public partial class GeneratorsRegex
    {
        [GeneratedRegex(@"[ \t]*\r?\n(\s*\r?\n)+", RegexOptions.Multiline)]
        public static partial Regex MultipleEmptyLines();

        [GeneratedRegex(@"(?<!^)(?=[A-Z])")]
        public static partial Regex SplitCamelCaseWords();
    }
}
