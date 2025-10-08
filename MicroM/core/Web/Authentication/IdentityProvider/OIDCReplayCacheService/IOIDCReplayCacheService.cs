namespace MicroM.Web.Authentication.SSO;

public enum ReplayCacheStatus
{
    Added,
    Replay,
    Stale,
    Skew,
    Invalid
}

public readonly record struct ReplayCacheResult(ReplayCacheStatus Status, string? Reason = null);

public interface IOIDCReplayCacheService
{
    ReplayCacheResult TryStore(string jti, DateTimeOffset iatUtc);
}
