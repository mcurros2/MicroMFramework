using Microsoft.Extensions.Caching.Memory;

namespace MicroM.Web.Authentication;


public interface ITwoFactorChallengeStore
{
    string CreateChallenge(string userId, string username, string deviceId, string applicationId, string localDeviceId, Dictionary<string, string>? metadata = null);
    TwoFactorChallenge? GetChallenge(string challengeId);
    void RemoveChallenge(string challengeId);
}

public class TwoFactorChallengeStore : ITwoFactorChallengeStore
{
    private readonly IMemoryCache _cache;
    private static readonly TimeSpan ChallengeLifetime = TimeSpan.FromMinutes(5);

    public TwoFactorChallengeStore(IMemoryCache cache)
    {
        _cache = cache;
    }

    public string CreateChallenge(string userId, string username, string deviceId, string applicationId, string localDeviceId, Dictionary<string, string>? metadata = null)
    {
        string challengeId = Guid.NewGuid().ToString("N");
        var challenge = new TwoFactorChallenge
        {
            UserId = userId,
            Username = username,
            DeviceId = deviceId,
            ApplicationId = applicationId,
            LocalDeviceId = localDeviceId,
            CreatedUtc = DateTime.UtcNow,
            ExpiresUtc = DateTime.UtcNow.Add(ChallengeLifetime),
            Metadata = metadata ?? new(StringComparer.OrdinalIgnoreCase)
        };

        _cache.Set(challengeId, challenge, ChallengeLifetime);
        return challengeId;
    }

    public TwoFactorChallenge? GetChallenge(string challengeId)
    {
        return _cache.TryGetValue(challengeId, out TwoFactorChallenge? challenge) ? challenge : null;
    }

    public void RemoveChallenge(string challengeId)
    {
        _cache.Remove(challengeId);
    }
}
