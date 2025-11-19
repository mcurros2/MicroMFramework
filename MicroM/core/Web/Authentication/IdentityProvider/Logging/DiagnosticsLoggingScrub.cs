namespace MicroM.Web.Authentication.SSO;

/// <summary>
/// Scrub helpers for diagnostics logging only. Never alters HTTP responses.
/// </summary>
internal static partial class DiagnosticsLoggingScrub
{
    public static string TruncateSid(string? sid)
    {
        if (string.IsNullOrWhiteSpace(sid)) return "none";
        return sid.Length <= 16 ? sid : sid[..16] + "...";
    }

    public static Dictionary<string, object?> BuildMarker(
        string markerType,
        string appId,
        string? clientId = null,
        string? sid = null,
        bool? encrypted = null,
        string? alg = null,
        string? enc = null,
        int? keysCount = null,
        string? etag = null,
        bool? notModified = null)
    {
        return new()
        {
            ["marker"] = markerType,
            ["app_id"] = appId,
            ["client_id"] = clientId,
            ["sid"] = TruncateSid(sid),
            ["encrypted"] = encrypted,
            ["alg"] = alg,
            ["enc"] = enc,
            ["keys_count"] = keysCount,
            ["etag"] = etag,
            ["not_modified"] = notModified
        };
    }


}