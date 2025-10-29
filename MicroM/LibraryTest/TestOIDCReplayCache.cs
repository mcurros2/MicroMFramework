using MicroM.Web.Authentication.SSO;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;

namespace LibraryTest;

[TestClass]
public class TestOIDCReplayCache
{
    [TestMethod]
    public void TryStore_AddedThenReplay()
    {
        // Arrange
        var cache = new MemoryCache(new MemoryCacheOptions());
        var svc = new OIDCReplayCacheService(cache, NullLogger<OIDCReplayCacheService>.Instance);
        var jti = Guid.NewGuid().ToString("N");
        var iat = DateTimeOffset.UtcNow.AddMinutes(-1);

        // Act
        var first = svc.TryStore(jti, iat);
        var second = svc.TryStore(jti, iat);

        // Assert
        Assert.AreEqual(ReplayCacheStatus.Added, first.Status, first.Reason);
        Assert.AreEqual(ReplayCacheStatus.Replay, second.Status, second.Reason);
    }

    [TestMethod]
    public void TryStore_ExpiredIat_ReturnsStale()
    {
        // Arrange
        var cache = new MemoryCache(new MemoryCacheOptions());
        var svc = new OIDCReplayCacheService(cache, NullLogger<OIDCReplayCacheService>.Instance);
        var jti = Guid.NewGuid().ToString("N");
        var iatExpired = DateTimeOffset.UtcNow.AddMinutes(-30); // much older than TTL

        // Act
        var result = svc.TryStore(jti, iatExpired);

        // Assert
        Assert.AreEqual(ReplayCacheStatus.Stale, result.Status);
        StringAssert.Contains(result.Reason ?? string.Empty, "expired", StringComparison.OrdinalIgnoreCase);
    }

    [TestMethod]
    public void TryStore_FutureIatBeyondSkew_ReturnsSkew()
    {
        // Arrange
        var cache = new MemoryCache(new MemoryCacheOptions());
        var svc = new OIDCReplayCacheService(cache, NullLogger<OIDCReplayCacheService>.Instance);
        var jti = Guid.NewGuid().ToString("N");
        var iatFuture = DateTimeOffset.UtcNow.AddMinutes(5); // beyond allowed skew

        // Act
        var result = svc.TryStore(jti, iatFuture);

        // Assert
        Assert.AreEqual(ReplayCacheStatus.Skew, result.Status);
        StringAssert.Contains(result.Reason ?? string.Empty, "skew", StringComparison.OrdinalIgnoreCase);
    }

    [TestMethod]
    public void TryStore_JtiTooLong_ReturnsInvalid()
    {
        // Arrange
        var cache = new MemoryCache(new MemoryCacheOptions());
        var svc = new OIDCReplayCacheService(cache, NullLogger<OIDCReplayCacheService>.Instance);
        var jti = new string('a', 1024); // far beyond max length
        var iat = DateTimeOffset.UtcNow.AddMinutes(-1);

        // Act
        var result = svc.TryStore(jti, iat);

        // Assert
        Assert.AreEqual(ReplayCacheStatus.Invalid, result.Status);
        StringAssert.Contains(result.Reason ?? string.Empty, "jti", StringComparison.OrdinalIgnoreCase);
    }
}