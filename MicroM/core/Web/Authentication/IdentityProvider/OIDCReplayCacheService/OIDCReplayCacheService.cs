
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace MicroM.Web.Authentication.SSO;

public class OIDCReplayCacheService(IMemoryCache cache, ILogger<OIDCReplayCacheService> log) : IOIDCReplayCacheService
{
    private const string KeyPrefix = "oidc:logout:jti:";

    private const int MAX_JTI_LENGTH = 256;
    private static readonly TimeSpan TOKEN_TTL = TimeSpan.FromMinutes(10);
    private static readonly TimeSpan CLOCK_SKEW = TimeSpan.FromMinutes(2);

    public ReplayCacheResult TryStore(string jti, DateTimeOffset iatUtc)
    {
        if (string.IsNullOrWhiteSpace(jti))
            return new(ReplayCacheStatus.Invalid, "empty_jti");

        if (jti.Length > MAX_JTI_LENGTH)
            return new(ReplayCacheStatus.Invalid, "jti_too_long");

        var now = DateTimeOffset.UtcNow;
        var age = now - iatUtc;

        if (age > TOKEN_TTL)
            return new(ReplayCacheStatus.Stale, "expired_iat");

        if (age < TimeSpan.Zero && (-age) > CLOCK_SKEW)
            return new(ReplayCacheStatus.Skew, "future_iat_exceeds_skew");

        var key = KeyPrefix + jti;
        if (cache.TryGetValue(key, out _))
        {
            log.LogWarning("Backchannel logout replay detected for jti {jti}", jti);
            return new(ReplayCacheStatus.Replay);
        }

        // Remaining time before TTL boundary
        var remaining = TOKEN_TTL - (age < TimeSpan.Zero ? TimeSpan.Zero : age);
        if (remaining <= TimeSpan.Zero) remaining = TimeSpan.FromSeconds(1);

        cache.Set(key, 1, new MemoryCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = remaining,
            Priority = CacheItemPriority.Low
        });

        log.LogDebug("Stored logout jti {jti} (age={age:F1}s, ttlRemaining={rem:F1}s)", jti, age.TotalSeconds, remaining.TotalSeconds);

        return new(ReplayCacheStatus.Added);
    }
}
