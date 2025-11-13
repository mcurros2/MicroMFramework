namespace MicroM.Web.Services;

public class EtagContent
{
    public string Content { get; init; }
    public string Etag { get; init; }

    public DateTimeOffset CachedUtc { get; init; }

    public DateTimeOffset? ExpiresUtc { get; init; }

    public bool IsExpired(DateTimeOffset nowUtc) => ExpiresUtc.HasValue && nowUtc >= ExpiresUtc.Value;
}
