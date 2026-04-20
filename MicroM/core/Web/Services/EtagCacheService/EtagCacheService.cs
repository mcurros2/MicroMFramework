using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Headers;
using Microsoft.Extensions.Logging;
using Microsoft.Net.Http.Headers;

namespace MicroM.Web.Services;


public record EtagCacheServiceCacheCheckResult<T>(
    bool not_modified,
    EtagContent<T> etag_content,
    ResponseHeaders result_headers
) where T : class?;

public class EtagCacheService<T> : IEtagCacheService<T> where T : class?
{
    private EtagCache<T> _etagCache = new();


    public EtagCacheService(IMemoryEventsService bus, ILogger<EtagCacheService<T>> log)
    {
        bus.Subscribe<MicroMConfigurationReloaded>(_ =>
        {
            log.LogInformation("Clearing ETag cache due to MicroMConfigurationReloaded");
            ClearCache();
        });
    }

    public void ClearCache() => _etagCache.Clear();

    public EtagContent<T>? Get(string key) => _etagCache.Get(key);

    public EtagContent<T> GetOrAdd(string key, Func<EtagContent<T>?, (string json, T? parsed, string? etag)> valueFactory, TimeSpan? ttl = null) => _etagCache.GetOrAdd(key, valueFactory, ttl);


    public async ValueTask<EtagContent<T>> GetOrAddAsync(
        string key,
        Func<EtagContent<T>?, CancellationToken, ValueTask<(string json, T? parsed, string? etag)>> valueFactory,
        bool serveStaleOnError,
        CancellationToken ct,
        int maxRetries = 2,
        TimeSpan? ttl = null)
    {
        return await _etagCache.GetOrAddAsync(key, valueFactory, serveStaleOnError, ct, maxRetries, ttl);
    }

    private static ResponseHeaders GetResponseHeaders(EtagContent<T> content, IHeaderDictionary response_headers, double cache_duration_seconds)
    {
        var etag = new EntityTagHeaderValue($"\"{content.Etag}\"");
        ResponseHeaders result_headers = new(response_headers)
        {
            ETag = etag,
            CacheControl = new CacheControlHeaderValue()
            {
                Public = true,
                MaxAge = TimeSpan.FromSeconds(cache_duration_seconds),
                SharedMaxAge = TimeSpan.FromSeconds(cache_duration_seconds),
                MustRevalidate = true
            }
        };
        return result_headers;
    }

    private static bool IsEtagInCache(EtagContent<T> content, RequestHeaders request_headers)
    {
        var etag = new EntityTagHeaderValue($"\"{content.Etag}\"");

        var none_match = request_headers.IfNoneMatch;

        bool anyWildcard = none_match != null && none_match.Any(n => n == EntityTagHeaderValue.Any || n.Tag.Value == "*");
        bool tagMatch = none_match != null && none_match.Contains(etag);

        return anyWildcard || tagMatch;
    }


    public EtagCacheServiceCacheCheckResult<T> GetOrAddResponseWithCacheCheck(
        string key,
        RequestHeaders request_headers,
        IHeaderDictionary response_headers,
        double cache_duration_seconds,
        Func<EtagContent<T>?, (string json, T? parsed, string? etag)> valueFactory)
    {
        var ttl = TimeSpan.FromSeconds(cache_duration_seconds);
        var content = GetOrAdd(key, valueFactory, ttl);

        var in_cache = IsEtagInCache(content, request_headers);
        var result_headers = GetResponseHeaders(content, response_headers, cache_duration_seconds);

        return new EtagCacheServiceCacheCheckResult<T>(in_cache, content, result_headers);
    }

    public string? GetRequestEtag(RequestHeaders request_headers)
    {
        var none_match = request_headers.IfNoneMatch;
        if (none_match != null && none_match.Count > 0)
        {
            var etag = none_match.FirstOrDefault(n => n != EntityTagHeaderValue.Any);
            if (etag != null && !string.IsNullOrEmpty(etag.Tag.Value))
            {
                return etag.Tag.Value.Trim('"');
            }
        }
        return null;
    }

    public void Remove(string key) => _etagCache.Remove(key);
}
