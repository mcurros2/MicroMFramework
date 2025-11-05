using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace MicroM.Web.Extensions;

public static class WebExtensions
{
    public static string ETag(this string data)
    {
        var cachedBytes = Encoding.UTF8.GetBytes(data);
        var hash = SHA256.HashData(cachedBytes);
        return Convert.ToBase64String(hash);
    }

    public static bool isValidHTTPSUrl(this string url)
    {
        if (Uri.TryCreate(url, UriKind.Absolute, out var uriResult))
        {
            return uriResult.Scheme == Uri.UriSchemeHttps;
        }
        return false;
    }

    public static string? ReadString(this JsonElement root, string name)
    {
        return root.TryGetProperty(name, out var prop) && prop.ValueKind == JsonValueKind.String ? prop.GetString() : null;
    }

}
