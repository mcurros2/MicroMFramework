namespace MicroM.Extensions
{
    public static class StringExtensions
    {
        public static string Truncate(this string value, int maxLength)
        {
            if (string.IsNullOrEmpty(value)) return value;
            return value[..(value.Length > maxLength ? maxLength : value.Length)];
        }

        public static string Unquote(this string value, bool unescape = false)
        {
            if (string.IsNullOrWhiteSpace(value) || value.Length < 2)
                return value;

            value = value.Trim();

            if (value.StartsWith('\"') && value.EndsWith('\"'))
            {
                value = value[1..^1];
            }

            if (unescape)
            {
                value = value.Replace("\"\"", "\"");
            }

            return value;
        }

        public static IEnumerable<string> Unquote(this IEnumerable<string> value)
        {
            return value.Select(x => x.Unquote()).ToArray();
        }

        public static IEnumerable<string> Trim(this IEnumerable<string> value)
        {
            return value.Select(x => x.Trim()).ToArray();
        }

    }
}
