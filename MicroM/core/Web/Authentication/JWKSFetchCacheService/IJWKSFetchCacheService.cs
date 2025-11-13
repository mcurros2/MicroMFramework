using Microsoft.IdentityModel.Tokens;

namespace MicroM.Web.Authentication.SSO;

public sealed record JwksCacheResult(
    string JwksUri,
    string? ETag,
    string? RawJson,
    OIDCJwksResponse? Parsed,
    IReadOnlyDictionary<string, SecurityKey> Keys,
    DateTimeOffset FetchedUtc,
    bool FromCache,
    bool WasRefreshed);

public interface IJWKSFetchCacheService
{
    Task<JwksCacheResult> GetAsync(string jwksUri, CancellationToken ct, bool forceRefresh = false);
    Task<SecurityKey?> GetKeyByKidAsync(string jwksUri, string kid, CancellationToken ct);
    void Invalidate(string jwksUri); // manual purge
}
