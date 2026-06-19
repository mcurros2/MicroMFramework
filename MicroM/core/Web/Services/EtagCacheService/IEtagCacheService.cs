using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Headers;

namespace MicroM.Web.Services;

public interface IEtagCacheService<T> where T : class?
{
    void ClearCache();
    EtagContent<T>? Get(string key);

    EtagContent<T> GetOrAdd(string key, Func<EtagContent<T>?, (string json, T? parsed, string? etag)> valueFactory, TimeSpan? ttl = null);

    ValueTask<EtagContent<T>> GetOrAddAsync(
        string key,
        Func<EtagContent<T>?, CancellationToken, ValueTask<(string json, T? parsed, string? etag)>> valueFactory,
        bool serveStaleOnError,
        CancellationToken ct,
        int maxRetries = 2,
        TimeSpan? ttl = null);

    EtagCacheServiceCacheCheckResult<T> GetOrAddResponseWithCacheCheck(
        string key,
        RequestHeaders request_headers,
        IHeaderDictionary response_headers,
        double cache_duration_seconds,
        Func<EtagContent<T>?, (string json, T? parsed, string? etag)> valueFactory);

    string? GetRequestEtag(RequestHeaders request_headers);

    void Remove(string key);

}