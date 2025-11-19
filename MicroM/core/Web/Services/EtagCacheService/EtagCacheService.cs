using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Headers;
using Microsoft.Net.Http.Headers;

namespace MicroM.Web.Services;

public record EtagCacheServiceCacheCheckResult(
    bool not_modified,
    EtagContent etag_content,
    ResponseHeaders result_headers
    );

public class EtagCacheService : IEtagCacheService
{
    private EtagCache _etagCache = new();

    public void ClearCache() => _etagCache.Clear();

    public EtagContent? Get(string key) => _etagCache.Get(key);

    public EtagContent GetOrAdd(string key, Func<string> valueFactory, TimeSpan? ttl = null) => _etagCache.GetOrAdd(key, valueFactory, ttl);


    public async ValueTask<EtagContent> GetOrAddAsync(
        string key,
        Func<CancellationToken, ValueTask<string>> valueFactory,
        bool serveStaleOnError,
        CancellationToken ct,
        int maxRetries = 2,
        TimeSpan? ttl = null)
    {
        return await _etagCache.GetOrAddAsync(key, valueFactory, serveStaleOnError, ct, maxRetries, ttl);
    }

    private static ResponseHeaders GetResponseHeaders(EtagContent content, IHeaderDictionary response_headers, double cache_duration_seconds)
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

    private static bool IsEtagInCache(EtagContent content, RequestHeaders request_headers)
    {
        var etag = new EntityTagHeaderValue($"\"{content.Etag}\"");

        var none_match = request_headers.IfNoneMatch;

        bool anyWildcard = none_match != null && none_match.Any(n => n == EntityTagHeaderValue.Any || string.Equals(n.Tag.Value, "*", StringComparison.Ordinal));
        bool tagMatch = none_match != null && none_match.Contains(etag);

        return anyWildcard || tagMatch;
    }


    public EtagCacheServiceCacheCheckResult GetOrAddResponseWithCacheCheck(
        string key,
        RequestHeaders request_headers,
        IHeaderDictionary response_headers,
        double cache_duration_seconds,
        Func<string> valueFactory)
    {
        var ttl = TimeSpan.FromSeconds(cache_duration_seconds);
        var content = GetOrAdd(key, valueFactory, ttl);

        var in_cache = IsEtagInCache(content, request_headers);
        var result_headers = GetResponseHeaders(content, response_headers, cache_duration_seconds);

        return new EtagCacheServiceCacheCheckResult(in_cache, content, result_headers);
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
