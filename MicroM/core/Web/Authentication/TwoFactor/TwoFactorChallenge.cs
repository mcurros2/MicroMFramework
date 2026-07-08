namespace MicroM.Web.Authentication;

/// <summary>
/// Represents a pending two-factor authentication challenge.
/// </summary>
public class TwoFactorChallenge
{
    public required string UserId { get; init; }
    public required string Username { get; init; }
    public required string DeviceId { get; init; }
    public required string ApplicationId { get; init; }
    public required string LocalDeviceId { get; init; }
    public required DateTime CreatedUtc { get; init; }
    public required DateTime ExpiresUtc { get; init; }
    public Dictionary<string, string> Metadata { get; init; } = new(StringComparer.OrdinalIgnoreCase);
}
