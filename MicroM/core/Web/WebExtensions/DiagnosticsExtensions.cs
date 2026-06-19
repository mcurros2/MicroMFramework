using System.Text.RegularExpressions;

namespace MicroM.Web.Extensions;

public static partial class DiagnosticsExtensions
{
    // Diagnostics logging scrubber (redacts common sensitive values)
    public static string ScrubForDiagnostics(this string? input)
    {
        if (string.IsNullOrEmpty(input)) return string.Empty;

        var s = input;

        // Redact common JSON fields that may carry sensitive values
        string RedactField(string src, string field) =>
            Regex.Replace(src,
                $"(\"{Regex.Escape(field)}\"\\s*:\\s*\")([^\"]+)(\")",
                $"$1<redacted>$3",
                RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

        foreach (var field in new[]
        {
            "id_token", "access_token", "refresh_token", "client_assertion",
            "logout_token", "client_secret", "authorization", "device_code"
        })
        {
            s = RedactField(s, field);
        }

        // Redact JWT-like tokens (three Base64URL segments)
        s = JwtRedactedRegex().Replace(s, "<jwt-redacted>");

        // Redact potential bearer tokens in text
        s = BearerReadactedRegex().Replace(s, "$1<redacted>");

        return s;
    }

    [GeneratedRegex(@"([A-Za-z0-9\-_]{8,})\.([A-Za-z0-9\-_]{8,})\.([A-Za-z0-9\-_]{8,})", RegexOptions.CultureInvariant)]
    private static partial Regex JwtRedactedRegex();

    [GeneratedRegex(@"(Authorization\s*:\s*Bearer\s+)[^\s]+", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex BearerReadactedRegex();
}
