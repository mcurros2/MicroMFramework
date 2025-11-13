using MicroM.Configuration;
using MicroM.Web.Services;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using System.Collections.Concurrent;
using System.Text.Json;

namespace MicroM.Web.Authentication.SSO;

// Caches JWKS per jwks_uri. Stores ETag, raw JSON, parsed OIDC model and materialized SecurityKey map.
// Relies on IEtagCacheService internal thread-safety (ConcurrentDictionary + in-flight task de-duplication).
public class JWKSFetchCacheService(
    IOIDCHttpClient oidcHttpClient,
    IEtagCacheService etagCache,
    ILogger<JWKSFetchCacheService> logger
    ) : IJWKSFetchCacheService
{
    private readonly ConcurrentDictionary<string, JwksCacheResult> _jwksResults = new(StringComparer.OrdinalIgnoreCase);

    // Tracks the last server-provided ETag per jwksUri (used for outbound If-None-Match).
    private readonly ConcurrentDictionary<string, string> _serverEtags = new(StringComparer.OrdinalIgnoreCase);

    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private static TimeSpan DefaultTTL = TimeSpan.FromSeconds(ConfigurationDefaults.JwksCacheDurationSeconds);

    private static string ToEtagKey(string jwksUri) => $"jwks:{jwksUri}";

    public async Task<SecurityKey?> GetKeyByKidAsync(string jwksUri, string kid, CancellationToken ct)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(jwksUri);
        ArgumentException.ThrowIfNullOrWhiteSpace(kid);

        // First attempt from cache (or normal fetch)
        var jwks = await GetAsync(jwksUri, ct).ConfigureAwait(false);
        if (jwks.Keys.TryGetValue(kid, out var key)) return key;

        // kid miss fallback - force a refresh once to tolerate rotations, then retry lookup
        var refreshed = await GetAsync(jwksUri, ct, forceRefresh: true).ConfigureAwait(false);
        return refreshed.Keys.TryGetValue(kid, out var keyAfterRefresh) ? keyAfterRefresh : null;
    }

    public void Invalidate(string jwksUri)
    {
        if (string.IsNullOrWhiteSpace(jwksUri)) return;
        etagCache.Remove(ToEtagKey(jwksUri));
        _jwksResults.TryRemove(jwksUri, out _);
        _serverEtags.TryRemove(jwksUri, out _);
    }

    public async Task<JwksCacheResult> GetAsync(string jwksUri, CancellationToken ct, bool forceRefresh = false)
    {
        if (forceRefresh)
        {
            // clear only the raw-json ETag entry to guarantee a re-fetch; parsed cache will be rebuilt below
            etagCache.Remove(ToEtagKey(jwksUri));
            _serverEtags.TryRemove(jwksUri, out _);
        }

        // Fetch (or reuse) raw JWKS JSON via ETag cache
        var etagContent = await etagCache.GetOrAddAsync(
            ToEtagKey(jwksUri),
            async (token) =>
            {
                // Read previously cached content (to reuse on 304)
                var previous = etagCache.Get(ToEtagKey(jwksUri));
                // Send If-None-Match using last seen server ETag (when not force-refreshing)
                _serverEtags.TryGetValue(jwksUri, out var prevServerEtag);

                var resp = await oidcHttpClient.GetJwksJsonAsync(jwksUri, token, prevServerEtag).ConfigureAwait(false);

                // 304 Not Modified -> reuse existing cached body if present
                if (resp.NotModified)
                {
                    if (!string.IsNullOrWhiteSpace(resp.ETag))
                        _serverEtags[jwksUri] = resp.ETag;

                    if (previous != null && !string.IsNullOrEmpty(previous.Content))
                    {
                        return previous.Content;
                    }

                    // As a fallback (rare): fetch unconditionally if we have no cached body
                    var fresh = await oidcHttpClient.GetJwksJsonAsync(jwksUri, token, ifNoneMatch: null).ConfigureAwait(false);
                    if (!fresh.IsSuccessStatusCode)
                    {
                        var msg = fresh.Error ?? $"GET JWKS failed: {fresh.StatusCode}";
                        throw new InvalidOperationException(msg);
                    }

                    if (!string.IsNullOrWhiteSpace(fresh.ETag))
                        _serverEtags[jwksUri] = fresh.ETag;

                    return fresh.Body ?? "";
                }

                if (!resp.IsSuccessStatusCode)
                {
                    var msg = resp.Error ?? $"GET JWKS failed: {resp.StatusCode}";
                    throw new InvalidOperationException(msg);
                }

                if (!string.IsNullOrWhiteSpace(resp.ETag))
                    _serverEtags[jwksUri] = resp.ETag;

                return resp.Body ?? "";
            },
            serveStaleOnError: true,
            ttl: DefaultTTL,
            ct: ct
        ).ConfigureAwait(false);

        var newEtag = etagContent.Etag;      // Content-hash ETag (local)
        var rawJson = etagContent.Content;   // Cached JWKS JSON body

        // Try reuse existing parsed result if ETag unchanged (content didn't change)
        if (_jwksResults.TryGetValue(jwksUri, out var existing) &&
            !string.IsNullOrEmpty(existing.ETag) &&
            string.Equals(existing.ETag, newEtag, StringComparison.Ordinal))
        {
            // Return cached parsed/mapped result
            return existing with
            {
                FromCache = true,
                WasRefreshed = false,
                FetchedUtc = DateTimeOffset.UtcNow
            };
        }

        // Parse OIDC JWKS response (best-effort)
        OIDCJwksResponse? parsed = null;
        try
        {
            parsed = JsonSerializer.Deserialize<OIDCJwksResponse>(rawJson, _jsonOptions);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to parse JWKS JSON at {jwksUri}", jwksUri);
        }

        // Materialize kid -> SecurityKey map
        IReadOnlyDictionary<string, SecurityKey> keys = BuildSecurityKeyMap(rawJson);

        var result = new JwksCacheResult(
            JwksUri: jwksUri,
            ETag: newEtag, // Note: content-hash ETag (local). Server ETag is stored internally in _serverEtags.
            RawJson: rawJson,
            Parsed: parsed,
            Keys: keys,
            FetchedUtc: DateTimeOffset.UtcNow,
            FromCache: false,
            WasRefreshed: existing != null
        );

        _jwksResults[jwksUri] = result;
        return result;
    }

    private static Dictionary<string, SecurityKey> BuildSecurityKeyMap(string rawJson)
    {
        var set = new JsonWebKeySet(rawJson);
        var dict = new Dictionary<string, SecurityKey>(StringComparer.Ordinal);
        foreach (var jwk in set.Keys)
        {
            var kid = jwk.Kid
                      ?? jwk.X5tS256
                      ?? jwk.X5t
                      ?? $"key-{dict.Count}";
            if (!dict.ContainsKey(kid))
            {
                dict[kid] = jwk;
            }
        }
        return dict;
    }
}