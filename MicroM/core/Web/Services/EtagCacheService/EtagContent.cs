namespace MicroM.Web.Services;

public sealed class EtagContent<T> where T : class?
{
    public string Content { get; init; }
    public string Etag { get; init; }

    public DateTimeOffset CachedUtc { get; init; }

    public DateTimeOffset? ExpiresUtc { get; init; }

    public T? Parsed { get; init; } = null;

    public bool IsExpired(DateTimeOffset nowUtc) => ExpiresUtc.HasValue && nowUtc >= ExpiresUtc.Value;
}
