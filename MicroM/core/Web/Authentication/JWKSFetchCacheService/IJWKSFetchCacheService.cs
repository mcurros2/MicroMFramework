using Microsoft.IdentityModel.Tokens;

namespace MicroM.Web.Authentication.SSO;

public sealed record JwksCacheResult(
    string JwksUri,
    string? ETag,                // Local content-hash ETag
    string? RawJson,
    OIDCJwksResponse? Parsed,
    IReadOnlyDictionary<string, SecurityKey> Keys,
    DateTimeOffset FetchedUtc,
    bool FromCache,              // Parsed result reused (local cache hit)
    bool WasRefreshed,           // Parsed result replaced (content changed)
    string? ServerETag,          // Last server-provided ETag for jwks_uri (If-None-Match target)
    bool ServerNotModified,      // True when 304 Not Modified path observed
    string? SentIfNoneMatch      // ETag value sent in outbound If-None-Match header (when present)
);

public interface IJWKSFetchCacheService
{
    Task<JwksCacheResult> GetAsync(string jwksUri, CancellationToken ct, bool forceRefresh = false);
    Task<SecurityKey?> GetKeyByKidAsync(string jwksUri, string kid, CancellationToken ct);
    void Invalidate(string jwksUri); // manual purge
}
