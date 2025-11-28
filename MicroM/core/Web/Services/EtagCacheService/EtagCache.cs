using MicroM.Web.Extensions;
using System.Collections.Concurrent;

namespace MicroM.Web.Services;

public class EtagCache<T> where T : class?
{
    private readonly ConcurrentDictionary<string, EtagContent<T>> _cache = new();
    // Tracks exactly one in-flight computation per key
    private readonly ConcurrentDictionary<string, Task<EtagContent<T>>> _inflight = new();

    public EtagContent<T>? Get(string key)
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


    public EtagContent<T> GetOrAdd(string key, Func<EtagContent<T>?, (string json, T? parsed, string? etag)> valueFactory)
    {
        return GetOrAdd(key, valueFactory, ttl: null);
    }

    public EtagContent<T> GetOrAdd(string key, Func<EtagContent<T>?, (string json, T? parsed, string? etag)> valueFactory, TimeSpan? ttl)
    {
        var now = DateTimeOffset.UtcNow;
        if (_cache.TryGetValue(key, out var existing))
        {
            if (ttl == null || !existing.IsExpired(now))
            {
                return existing;
            }
        }

        var (json, parsed, etagOverride) = valueFactory(existing);

        // we do not cache empty json
        if (string.IsNullOrEmpty(json))
        {
            // re-use previous
            if (existing != null && (ttl == null || !existing.IsExpired(now)))
            {
                return existing;
            }

            // Return empty etag
            var etagEmpty = !string.IsNullOrEmpty(etagOverride)
                ? etagOverride
                : "";

            return new EtagContent<T>
            {
                Content = "",
                Etag = etagEmpty,
                CachedUtc = now,
                Parsed = parsed,
                ExpiresUtc = null
            };
        }

        var etag = !string.IsNullOrEmpty(etagOverride) ? etagOverride : (json ?? "").ETag();

        var refreshed = new EtagContent<T>
        {
            Content = json ?? "",
            Etag = etag,
            CachedUtc = now,
            Parsed = parsed,
            ExpiresUtc = ttl.HasValue ? now.Add(ttl.Value) : null
        };

        _cache.AddOrUpdate(key, refreshed, (k, old) => refreshed);
        return _cache[key];
    }

    public async ValueTask<EtagContent<T>> GetOrAddAsync(
        string key,
        Func<EtagContent<T>?, CancellationToken, ValueTask<(string json, T? parsed, string? etag)>> valueFactory,
        bool serveStaleOnError,
        CancellationToken ct,
        int maxRetries = 2,
        TimeSpan? ttl = null
    )
    {
        var now = DateTimeOffset.UtcNow;

        if (_cache.TryGetValue(key, out var hit) &&
            (ttl == null || !hit.IsExpired(now)))
        {
            return hit;
        }


        var task = _inflight.GetOrAdd(key, _ => CreateAndAddAsync(key, valueFactory, ct, maxRetries, ttl));
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

    private async Task<EtagContent<T>> CreateAndAddAsync(
        string key,
        Func<EtagContent<T>?, CancellationToken, ValueTask<(string json, T? parsed, string? etag)>> valueFactory,
        CancellationToken ct,
        int maxRetries = 2,
        TimeSpan? ttl = null)
    {
        Exception? last = null;

        for (int attempt = 0; attempt <= maxRetries; attempt++)
        {
            // Someone else might have filled it while we were backing off
            var now = DateTimeOffset.UtcNow;
            if (_cache.TryGetValue(key, out var existing) && (ttl == null || !existing.IsExpired(now)))
            {
                return existing;
            }

            try
            {

                var (json, parsed, etagOverride) = await valueFactory(existing, ct).ConfigureAwait(false);

                if (string.IsNullOrEmpty(json))
                {
                    // Return previous if still valid
                    if (existing != null && (ttl == null || !existing.IsExpired(now)))
                    {
                        return existing;
                    }

                    // Return empty etag without caching
                    var etagEmpty = !string.IsNullOrEmpty(etagOverride)
                        ? etagOverride
                        : "";

                    return new EtagContent<T>
                    {
                        Content = "",
                        Etag = etagEmpty,
                        CachedUtc = now,
                        Parsed = parsed,
                        ExpiresUtc = null
                    };
                }

                var etag = !string.IsNullOrEmpty(etagOverride) ? etagOverride : (json ?? "").ETag();

                var result = new EtagContent<T>
                {
                    Content = json ?? "",
                    Etag = etag,
                    CachedUtc = now,
                    Parsed = parsed,
                    ExpiresUtc = ttl.HasValue ? now.Add(ttl.Value) : null
                };

                _cache[key] = result;
                return result;
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

        return new EtagContent<T>
        {
            Content = "",
            Etag = "",
            CachedUtc = DateTimeOffset.UtcNow,
            Parsed = null,
            ExpiresUtc = null
        };
    }
}
