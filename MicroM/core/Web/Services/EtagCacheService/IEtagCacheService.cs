using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Headers;

namespace MicroM.Web.Services;

public interface IEtagCacheService
{
    void ClearCache();
    EtagContent? Get(string key);

    EtagContent GetOrAdd(string key, Func<string> valueFactory, TimeSpan? ttl = null);

    ValueTask<EtagContent> GetOrAddAsync(string key, Func<CancellationToken, ValueTask<string>> valueFactory, bool serveStaleOnError, CancellationToken ct, int maxRetries = 2, TimeSpan? ttl = null);

    EtagCacheServiceCacheCheckResult GetOrAddResponseWithCacheCheck(string key, RequestHeaders request_headers, IHeaderDictionary response_headers, double cache_duration_seconds, Func<string> valueFactory);

    string? GetRequestEtag(RequestHeaders request_headers);

    void Remove(string key);
}