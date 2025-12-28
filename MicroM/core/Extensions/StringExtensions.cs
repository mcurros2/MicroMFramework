using System.Text;

namespace MicroM.Extensions;

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
        return [.. value.Select(x => x.Unquote())];
    }

    public static IEnumerable<string> Trim(this IEnumerable<string> value)
    {
        return [.. value.Select(x => x.Trim())];
    }

    public static string IfNullOrEmpty(this string value, string null_or_empty_value)
    {
        return string.IsNullOrEmpty(value) ? null_or_empty_value : value;
    }

    public static string ThrowIfNullOrEmpty(this string value, string? parm_name)
    {
        if (string.IsNullOrEmpty(value)) throw new ArgumentNullException(parm_name);
        return value;
    }

    public static bool IsNullOrEmpty(this string? value)
    {
        return string.IsNullOrEmpty(value);
    }

    public static string Mask(this string? value)
    {
        return string.IsNullOrEmpty(value) ? "empty" : $"<{value.Length} chars>";
    }

    public static bool IsContainedIn(this string? value, IEnumerable<string> list, StringComparison comparison = StringComparison.OrdinalIgnoreCase)
    {
        if (value == null) return false;
        foreach (var item in list)
        {
            if (string.Equals(value, item, comparison))
            {
                return true;
            }
        }
        return false;
    }

    public static bool StartsWithAny(this string? value, IEnumerable<string> prefixes, StringComparison comparison = StringComparison.OrdinalIgnoreCase)
    {
        if (value == null) return false;
        foreach (var prefix in prefixes)
        {
            if (value.StartsWith(prefix, comparison))
            {
                return true;
            }
        }
        return false;
    }

    public static string ToBase64(this string? value)
    {
        return string.IsNullOrEmpty(value) ? "" : Convert.ToBase64String(Encoding.UTF8.GetBytes(value));
    }

    extension(string)
    {
        /// <summary>
        /// This method checks if any of the provided string values are null, empty, or consist only of white-space characters.
        /// </summary>
        /// <remarks>
        /// TO AVOID CS8620 WARNING use like this: string.IsAnyNullOrWhiteSpace([str1, str2, str3]);
        /// Remove this remark when bug is https://github.com/dotnet/roslyn/issues/81699?utm_source=chatgpt.com is fixed
        /// </remarks>
        public static bool IsAnyNullOrWhiteSpace(params string?[] values)
        {
            if (values == null || values.Length == 0) return false;

            foreach (var value in values)
            {
                if (string.IsNullOrWhiteSpace(value?.ToString()))
                    return true;
            }
            return false;
        }
    }

}
