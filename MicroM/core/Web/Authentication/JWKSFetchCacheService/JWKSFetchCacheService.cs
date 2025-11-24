using MicroM.Configuration;
using MicroM.Web.Services;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using System.Collections.Concurrent;
using System.Text.Json;

namespace MicroM.Web.Authentication.SSO;

// Caches JWKS per jwks_uri. Adds trace log lines with cache metrics (hit/miss, etags, key counts).
public class JWKSFetchCacheService(
    IOIDCHttpClient oidcHttpClient,
    IEtagCacheService etagCache,
    ILogger<JWKSFetchCacheService> logger
    ) : IJWKSFetchCacheService
{
    private readonly ConcurrentDictionary<string, JwksCacheResult> _jwksResults = new(StringComparer.OrdinalIgnoreCase);
    private readonly ConcurrentDictionary<string, string> _serverEtags = new(StringComparer.OrdinalIgnoreCase);
    private static readonly JsonSerializerOptions _jsonOptions = new() { PropertyNameCaseInsensitive = true };
    private static readonly TimeSpan DefaultTTL = TimeSpan.FromSeconds(ConfigurationDefaults.JwksCacheDurationSeconds);
    private static string ToEtagKey(string jwksUri) => $"jwks:{jwksUri}";

    public async Task<SecurityKey?> GetKeyByKidAsync(string jwksUri, string kid, CancellationToken ct)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(jwksUri);
        ArgumentException.ThrowIfNullOrWhiteSpace(kid);

        var current = await GetAsync(jwksUri, ct).ConfigureAwait(false);
        if (current.Keys.TryGetValue(kid, out var key)) return key;

        var refreshed = await GetAsync(jwksUri, ct, forceRefresh: true).ConfigureAwait(false);
        var found = refreshed.Keys.TryGetValue(kid, out var newKey);

        logger.LogTrace("JWKS_KEY_LOOKUP jwks_uri={jwksUri} kid={kid} found_after_refresh={found} keys_before={before} keys_after={after} server_etag={serverETag} local_etag={localETag}",
            jwksUri, kid, found, current.Keys.Count, refreshed.Keys.Count, refreshed.ServerETag ?? "n/a", refreshed.ETag ?? "n/a");

        return newKey;
    }

    public void Invalidate(string jwksUri)
    {
        if (string.IsNullOrWhiteSpace(jwksUri)) return;
        etagCache.Remove(ToEtagKey(jwksUri));
        _jwksResults.TryRemove(jwksUri, out _);
        _serverEtags.TryRemove(jwksUri, out _);
        logger.LogTrace("JWKS_INVALIDATE jwks_uri={jwksUri}", jwksUri);
    }

    public async Task<JwksCacheResult> GetAsync(string jwksUri, CancellationToken ct, bool forceRefresh = false)
    {
        ct.ThrowIfCancellationRequested();
        if (string.IsNullOrWhiteSpace(jwksUri)) throw new ArgumentException("jwksUri required", nameof(jwksUri));

        _serverEtags.TryGetValue(jwksUri, out var serverEtagPrev);

        if (forceRefresh)
        {
            etagCache.Remove(ToEtagKey(jwksUri));
            _serverEtags.TryRemove(jwksUri, out _);
        }

        bool serverNotModified = false;
        string? sentIfNoneMatch = null;

        var etagContent = await etagCache.GetOrAddAsync(
            ToEtagKey(jwksUri),
            async token =>
            {
                var prevContent = etagCache.Get(ToEtagKey(jwksUri));
                _serverEtags.TryGetValue(jwksUri, out var prevServerEtag);
                sentIfNoneMatch = prevServerEtag;

                var resp = await oidcHttpClient.GetJwksJsonAsync(jwksUri, token, prevServerEtag).ConfigureAwait(false);

                if (resp.NotModified)
                {
                    serverNotModified = true;
                    if (!string.IsNullOrWhiteSpace(resp.ETag))
                        _serverEtags[jwksUri] = resp.ETag;

                    if (prevContent?.Content is { Length: > 0 })
                        return prevContent.Content;

                    var fresh = await oidcHttpClient.GetJwksJsonAsync(jwksUri, token, null).ConfigureAwait(false);
                    if (!fresh.IsSuccessStatusCode) throw new InvalidOperationException(fresh.Error ?? $"GET JWKS failed: {fresh.StatusCode}");
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

        var localEtag = etagContent.Etag;
        var rawJson = etagContent.Content ?? "";

        var cacheHit = _jwksResults.TryGetValue(jwksUri, out var existing) &&
                       !string.IsNullOrEmpty(existing.ETag) &&
                       existing.ETag == localEtag;

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

            logger.LogTrace("JWKS_CACHE_EVENT jwks_uri={jwksUri} hit=true force_refresh={forceRefresh} server_etag_prev={serverEtagPrev} server_etag_curr={serverEtagCurr} local_etag={localETag} not_modified={notModified} sent_if_none_match={sentIfNoneMatch} keys={keysCount} fetched_utc={fetchedUtc:o}",
                jwksUri,
                forceRefresh,
                serverEtagPrev ?? "n/a",
                reused.ServerETag ?? "n/a",
                localEtag ?? "n/a",
                serverNotModified,
                sentIfNoneMatch ?? "n/a",
                reused.Keys.Count,
                reused.FetchedUtc);

            return reused;
        }

        OIDCJwksResponse? parsed = null;
        try
        {
            parsed = JsonSerializer.Deserialize<OIDCJwksResponse>(rawJson, _jsonOptions);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "JWKS_PARSE_FAILED jwks_uri={jwksUri} local_etag={etag}", jwksUri, localEtag ?? "n/a");
        }

        var keys = BuildSecurityKeyMap(rawJson);
        _serverEtags.TryGetValue(jwksUri, out var serverEtagNew);

        var result = new JwksCacheResult(
            JwksUri: jwksUri,
            ETag: localEtag,
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

        logger.LogTrace("JWKS_CACHE_EVENT jwks_uri={jwksUri} hit=false force_refresh={forceRefresh} server_etag_prev={serverEtagPrev} server_etag_curr={serverEtagCurr} local_etag={localETag} not_modified={notModified} sent_if_none_match={sentIfNoneMatch} keys={keysCount} parsed={parsed} fetched_utc={fetchedUtc:o}",
            jwksUri,
            forceRefresh,
            serverEtagPrev ?? "n/a",
            serverEtagNew ?? "n/a",
            localEtag ?? "n/a",
            serverNotModified,
            sentIfNoneMatch ?? "n/a",
            keys.Count,
            parsed != null,
            result.FetchedUtc);

        return result;
    }

    public IEnumerable<JwksCacheMetric> GetCacheMetrics()
    {
        foreach (var kv in _jwksResults)
        {
            var r = kv.Value;
            yield return new JwksCacheMetric(
                jwks_uri: r.JwksUri,
                server_etag: r.ServerETag,
                local_etag: r.ETag,
                server_not_modified_last: r.ServerNotModified,
                was_refreshed_last: r.WasRefreshed,
                from_cache_last: r.FromCache,
                sent_if_none_match_last: r.SentIfNoneMatch,
                keys_count: r.Keys.Count,
                last_fetched_utc: r.FetchedUtc
            );
        }
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
                dict[kid] = jwk;
        }
        return dict;
    }
}