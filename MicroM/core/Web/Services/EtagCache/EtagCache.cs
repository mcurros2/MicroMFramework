using MicroM.Web.Extensions;
using System.Collections.Concurrent;

namespace MicroM.Web.Services;

public class EtagCache
{
    private readonly ConcurrentDictionary<string, EtagContent> _cache = new();

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
    }

    public void Remove(string key)
    {
        _cache.TryRemove(key, out _);
    }
}
