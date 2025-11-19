using MicroM.Configuration;
using MicroM.Web.Services;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using System.Collections.Concurrent;
using System.Text.Json;

namespace MicroM.Web.Authentication.SSO;

// Caches JWKS per jwks_uri. Stores server & local ETag, raw JSON, parsed OIDC model and materialized SecurityKey map.
// Adds structured logging markers for cache hit/miss, forced refresh, ETag sent/received, and 304 Not Modified path.
public class JWKSFetchCacheService(
    IOIDCHttpClient oidcHttpClient,
    IEtagCacheService etagCache,
    ILogger<JWKSFetchCacheService> logger
    ) : IJWKSFetchCacheService
{
    private readonly ConcurrentDictionary<string, JwksCacheResult> _jwksResults = new(StringComparer.OrdinalIgnoreCase);
    private readonly ConcurrentDictionary<string, string> _serverEtags = new(StringComparer.OrdinalIgnoreCase); // last server ETag
    private static readonly JsonSerializerOptions _jsonOptions = new() { PropertyNameCaseInsensitive = true };
    private static TimeSpan DefaultTTL = TimeSpan.FromSeconds(ConfigurationDefaults.JwksCacheDurationSeconds);
    private static string ToEtagKey(string jwksUri) => $"jwks:{jwksUri}";

    public async Task<SecurityKey?> GetKeyByKidAsync(string jwksUri, string kid, CancellationToken ct)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(jwksUri);
        ArgumentException.ThrowIfNullOrWhiteSpace(kid);

        var jwks = await GetAsync(jwksUri, ct).ConfigureAwait(false);
        if (jwks.Keys.TryGetValue(kid, out var key)) return key;

        // kid miss fallback - force a refresh once to tolerate rotations
        var refreshed = await GetAsync(jwksUri, ct, forceRefresh: true).ConfigureAwait(false);
        var found = refreshed.Keys.TryGetValue(kid, out var keyAfterRefresh) ? "true" : "false";

        logger.LogTrace("JWKS_CACHE_KEY_LOOKUP jwks_uri={jwksUri} kid={kid} kid_found_after_refresh={kidFound} forced_refresh=true server_etag={serverETag} local_etag={localETag} keys_count={keysCount}",
            jwksUri, kid, found, refreshed.ServerETag ?? "n/a", refreshed.ETag ?? "n/a", refreshed.Keys.Count);

        return keyAfterRefresh;
    }

    public void Invalidate(string jwksUri)
    {
        if (string.IsNullOrWhiteSpace(jwksUri)) return;
        etagCache.Remove(ToEtagKey(jwksUri));
        _jwksResults.TryRemove(jwksUri, out _);
        _serverEtags.TryRemove(jwksUri, out _);
        logger.LogTrace("JWKS_CACHE_INVALIDATE jwks_uri={jwksUri}", jwksUri);
    }

    public async Task<JwksCacheResult> GetAsync(string jwksUri, CancellationToken ct, bool forceRefresh = false)
    {
        ct.ThrowIfCancellationRequested();
        if (string.IsNullOrWhiteSpace(jwksUri)) throw new ArgumentException("jwksUri required", nameof(jwksUri));

        string? serverEtagPrev = null;
        _serverEtags.TryGetValue(jwksUri, out serverEtagPrev);

        if (forceRefresh)
        {
            etagCache.Remove(ToEtagKey(jwksUri));
            _serverEtags.TryRemove(jwksUri, out _);
        }

        // Flags captured from HTTP fetch lambda
        bool serverNotModified = false;
        string? sentIfNoneMatch = null;

        var etagContent = await etagCache.GetOrAddAsync(
            ToEtagKey(jwksUri),
            async (token) =>
            {
                var previousContent = etagCache.Get(ToEtagKey(jwksUri));
                _serverEtags.TryGetValue(jwksUri, out var prevServerEtag);
                sentIfNoneMatch = prevServerEtag;

                var resp = await oidcHttpClient.GetJwksJsonAsync(jwksUri, token, prevServerEtag).ConfigureAwait(false);

                if (resp.NotModified)
                {
                    serverNotModified = true;

                    if (!string.IsNullOrWhiteSpace(resp.ETag))
                        _serverEtags[jwksUri] = resp.ETag;

                    if (previousContent != null && !string.IsNullOrEmpty(previousContent.Content))
                        return previousContent.Content;

                    // Fallback unconditional fetch (rare path)
                    var fresh = await oidcHttpClient.GetJwksJsonAsync(jwksUri, token, ifNoneMatch: null).ConfigureAwait(false);
                    if (!fresh.IsSuccessStatusCode)
                        throw new InvalidOperationException(fresh.Error ?? $"GET JWKS failed: {fresh.StatusCode}");

                    if (!string.IsNullOrWhiteSpace(fresh.ETag))
                        _serverEtags[jwksUri] = fresh.ETag;

                    return fresh.Body ?? "";
                }

                if (!resp.IsSuccessStatusCode)
                    throw new InvalidOperationException(resp.Error ?? $"GET JWKS failed: {resp.StatusCode}");

                if (!string.IsNullOrWhiteSpace(resp.ETag))
                    _serverEtags[jwksUri] = resp.ETag;

                return resp.Body ?? "";
            },
            serveStaleOnError: true,
            ttl: DefaultTTL,
            ct: ct
        ).ConfigureAwait(false);

        var localContentEtag = etagContent.Etag;
        var rawJson = etagContent.Content ?? "";

        // Cache hit if parsed result exists with same local ETag
        var cacheHit = _jwksResults.TryGetValue(jwksUri, out var existing) &&
                       !string.IsNullOrEmpty(existing.ETag) &&
                       string.Equals(existing.ETag, localContentEtag, StringComparison.Ordinal);

        if (cacheHit)
        {
            var reused = existing with
            {
                FromCache = true,
                WasRefreshed = false,
                FetchedUtc = DateTimeOffset.UtcNow,
                ServerETag = _serverEtags.TryGetValue(jwksUri, out var se) ? se : null,
                ServerNotModified = serverNotModified,
                SentIfNoneMatch = sentIfNoneMatch
            };

            logger.LogTrace("JWKS_CACHE_EVENT jwks_uri={jwksUri} cache_hit=true force_refresh={forceRefresh} server_etag_prev={serverEtagPrev} server_etag_new={serverETagNew} local_etag={localETag} not_modified={notModified} sent_if_none_match={sentIfNoneMatch} keys_count={keysCount}",
                jwksUri,
                forceRefresh,
                serverEtagPrev ?? "n/a",
                reused.ServerETag ?? "n/a",
                localContentEtag ?? "n/a",
                serverNotModified,
                sentIfNoneMatch ?? "n/a",
                reused.Keys.Count);

            return reused;
        }

        // Parse JWKS
        OIDCJwksResponse? parsed = null;
        try
        {
            parsed = JsonSerializer.Deserialize<OIDCJwksResponse>(rawJson, _jsonOptions);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "JWKS_PARSE_FAILED jwks_uri={jwksUri}", jwksUri);
        }

        // Materialize keys
        IReadOnlyDictionary<string, SecurityKey> keys = BuildSecurityKeyMap(rawJson);

        var serverEtagNew = _serverEtags.TryGetValue(jwksUri, out var se2) ? se2 : null;

        var result = new JwksCacheResult(
            JwksUri: jwksUri,
            ETag: localContentEtag,
            RawJson: rawJson,
            Parsed: parsed,
            Keys: keys,
            FetchedUtc: DateTimeOffset.UtcNow,
            FromCache: false,
            WasRefreshed: existing != null,
            ServerETag: serverEtagNew,
            ServerNotModified: serverNotModified,
            SentIfNoneMatch: sentIfNoneMatch
        );

        _jwksResults[jwksUri] = result;

        logger.LogTrace("JWKS_CACHE_EVENT jwks_uri={jwksUri} cache_hit=false force_refresh={forceRefresh} server_etag_prev={serverEtagPrev} server_etag_new={serverEtagNew} local_etag={localETag} not_modified={notModified} sent_if_none_match={sentIfNoneMatch} keys_count={keysCount} parsed={parsed}",
            jwksUri,
            forceRefresh,
            serverEtagPrev ?? "n/a",
            serverEtagNew ?? "n/a",
            localContentEtag ?? "n/a",
            serverNotModified,
            sentIfNoneMatch ?? "n/a",
            keys.Count,
            parsed != null);

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