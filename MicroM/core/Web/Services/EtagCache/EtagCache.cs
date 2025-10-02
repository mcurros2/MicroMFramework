using MicroM.Web.Extensions;
using System.Collections.Concurrent;

namespace MicroM.Web.Services;

public class EtagCache
{
    private readonly ConcurrentDictionary<string, EtagContent> _cache = new();
    // Tracks exactly one in-flight computation per key
    private readonly ConcurrentDictionary<string, Task<EtagContent>> _inflight = new();

    public EtagContent GetOrAdd(string key, Func<string> valueFactory)
    {
        return _cache.GetOrAdd(key, k =>
        {
            var content = valueFactory();
            var etag = content.ETag();
            return new EtagContent { Content = content, Etag = etag };
        });
    }
    public EtagContent? Get(string key)
    {
        if (_cache.TryGetValue(key, out var value))
        {
            return value;
        }

        return null;
    }

    public void Clear()
    {
        _cache.Clear();
        _inflight.Clear();
    }

    public void Remove(string key)
    {
        _cache.TryRemove(key, out _);
        _inflight.TryRemove(key, out _);
    }

    public async ValueTask<EtagContent> GetOrAddAsync(
    string key,
    Func<CancellationToken, ValueTask<string>> valueFactory,
    bool serveStaleOnError,
    CancellationToken ct,
    int maxRetries = 2
    )
    {
        if (_cache.TryGetValue(key, out var hit))
            return hit;

        var task = _inflight.GetOrAdd(key, _ => CreateAndAddAsync(key, valueFactory, ct, maxRetries));
        try
        {
            return await task.ConfigureAwait(false);
        }
        catch
        {
            if (serveStaleOnError && _cache.TryGetValue(key, out var stale))
                return stale;  // graceful degradation

            throw;
        }
        finally
        {
            _inflight.TryRemove(key, out _);
        }
    }

    private async Task<EtagContent> CreateAndAddAsync(
    string key,
    Func<CancellationToken, ValueTask<string>> valueFactory,
    CancellationToken ct,
    int maxRetries = 2)
    {
        Exception? last = null;

        for (int attempt = 0; attempt <= maxRetries; attempt++)
        {
            // Another producer may have filled it while we were backing off
            if (_cache.TryGetValue(key, out var existing))
                return existing;

            try
            {
                var content = await valueFactory(ct).ConfigureAwait(false);
                var etag = content.ETag();
                var result = new EtagContent { Content = content, Etag = etag };
                _cache.TryAdd(key, result);
                return _cache[key];
            }
            catch (Exception ex) when (!ct.IsCancellationRequested)
            {
                last = ex;
                if (attempt == maxRetries) break;

                // simple decorrelated jitter backoff
                var delayMs = 100 * (attempt + 1) + Random.Shared.Next(0, 100);
                try { await Task.Delay(delayMs, ct).ConfigureAwait(false); }
                catch (OperationCanceledException) { throw; }
            }
        }

        throw last!;
    }

}
