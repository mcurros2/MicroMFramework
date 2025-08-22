namespace MicroM.Generators.Extensions
{
    public static class CommonExtensions
    {
        private static readonly string MULTIPLE_LINE_REPLACEMENT = Environment.NewLine + Environment.NewLine;

        /// <summary>
        /// Removes redundant empty lines from the text. See <see cref="GeneratorsRegex.MultipleEmptyLines"/>.
        /// It will replace multiple empty lines with Environment.NewLine.
        /// </summary>
        /// <param name="text"></param>
        /// <returns></returns>
        public static string RemoveEmptyLines(this string text)
        {
            return GeneratorsRegex.MultipleEmptyLines().Replace(text, MULTIPLE_LINE_REPLACEMENT);
        }

        /// <summary>
        /// Split a camel case word into separate words to create a description.
        /// </summary>
        /// <param name="text"></param>
        /// <returns></returns>
        public static string AddSpacesAndLowercaseShortWords(this string? text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return string.Empty;

            // Insert spaces before capital letters (except at the beginning of the text)
            string spacedText = GeneratorsRegex.SplitCamelCaseWords().Replace(text, " ");

            // Split the text into words, process each word, and reassemble
            string[] words = spacedText.Split();
            for (int i = 0; i < words.Length; i++)
            {
                if (words[i].Length <= 3 && i != 0)  // Lowercase words with 3 or fewer characters, except the first word
                {
                    words[i] = words[i].ToLowerInvariant();
                }
            }

            return string.Join(" ", words);
        }

    }
}
