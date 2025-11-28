
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace MicroM.Web.Authentication.SSO;

public class OIDCReplayCacheService(IMemoryCache cache, ILogger<OIDCReplayCacheService> log) : IOIDCReplayCacheService
{

    private const int MAX_JTI_LENGTH = 512;

    public ReplayCacheResult TryStore(string purpose, string jti, DateTimeOffset iatUtc, TimeSpan ttl, TimeSpan clockSkew)
    {

        if (string.IsNullOrWhiteSpace(jti))
            return new(ReplayCacheStatus.Invalid, "empty_jti");

        if (jti.Length > MAX_JTI_LENGTH)
            return new(ReplayCacheStatus.Invalid, "jti_too_long");

        var now = DateTimeOffset.UtcNow;
        var age = now - iatUtc;

        if (age > ttl)
        {
            log.LogDebug("Expired detected for purpose={purpose}, jti={jti}", purpose, jti);
            return new(ReplayCacheStatus.Stale, "expired_iat");
        }

        if (age < TimeSpan.Zero && (-age) > clockSkew)
        {
            log.LogWarning("Skew detected for purpose={purpose}, jti={jti}", purpose, jti);
            return new(ReplayCacheStatus.Skew, "future_iat_exceeds_skew");
        }

        var key = $"oidc:{purpose}:jti:{jti}";
        if (cache.TryGetValue(key, out _))
        {
            log.LogWarning("Replay detected for purpose={purpose}, jti={jti}", purpose, jti);
            return new(ReplayCacheStatus.Replay);
        }

        // Remaining time before TTL boundary
        var remaining = ttl - (age < TimeSpan.Zero ? TimeSpan.Zero : age);
        if (remaining <= TimeSpan.Zero) remaining = TimeSpan.FromSeconds(1);

        cache.Set(key, 1, new MemoryCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = remaining,
            Priority = CacheItemPriority.Low
        });

        log.LogDebug("Stored {purpose} jti {jti} (age={age:F1}s, ttlRemaining={rem:F1}s)", purpose, jti, age.TotalSeconds, remaining.TotalSeconds);

        return new(ReplayCacheStatus.Added);
    }
}
