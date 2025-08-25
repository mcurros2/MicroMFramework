namespace MicroM.Extensions;

public static class StringExtensions
{
    /// <summary>
    /// Truncates the string to the specified maximum length.
    /// </summary>
    /// <param name="value">Source string.</param>
    /// <param name="maxLength">Maximum length.</param>
    /// <returns>Truncated string.</returns>
    public static string Truncate(this string value, int maxLength)
    {
        if (string.IsNullOrEmpty(value)) return value;
        return value[..(value.Length > maxLength ? maxLength : value.Length)];
    }

    /// <summary>
    /// Removes surrounding quotes from a string and optionally unescapes inner quotes.
    /// </summary>
    /// <param name="value">String to process.</param>
    /// <param name="unescape">Whether to unescape double quotes.</param>
    /// <returns>Unquoted string.</returns>
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

    /// <summary>
    /// Removes surrounding quotes from each string in the collection.
    /// </summary>
    /// <param name="value">Collection of strings.</param>
    /// <returns>Collection of unquoted strings.</returns>
    public static IEnumerable<string> Unquote(this IEnumerable<string> value)
    {
        return value.Select(x => x.Unquote()).ToArray();
    }

    /// <summary>
    /// Trims whitespace from each string in the collection.
    /// </summary>
    /// <param name="value">Collection of strings.</param>
    /// <returns>Collection of trimmed strings.</returns>
    public static IEnumerable<string> Trim(this IEnumerable<string> value)
    {
        return value.Select(x => x.Trim()).ToArray();
    }

    /// <summary>
    /// Returns a fallback value when the string is null or empty.
    /// </summary>
    /// <param name="value">Source string.</param>
    /// <param name="null_or_empty_value">Value returned when source is null or empty.</param>
    /// <returns>Original or fallback value.</returns>
    public static string IfNullOrEmpty(this string value, string null_or_empty_value)
    {
        return string.IsNullOrEmpty(value) ? null_or_empty_value : value;
    }

    /// <summary>
    /// Throws an exception if the string is null or empty.
    /// </summary>
    /// <param name="value">String to validate.</param>
    /// <param name="parm_name">Parameter name for the exception.</param>
    /// <returns>The original string.</returns>
    public static string ThrowIfNullOrEmpty(this string value, string? parm_name)
    {
        if (string.IsNullOrEmpty(value)) throw new ArgumentNullException(parm_name);
        return value;
    }

    /// <summary>
    /// Determines whether the string is null or empty.
    /// </summary>
    /// <param name="value">String to check.</param>
    /// <returns><c>true</c> if null or empty.</returns>
    public static bool IsNullOrEmpty(this string? value)
    {
        return string.IsNullOrEmpty(value);
    }

}
