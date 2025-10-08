namespace MicroM.Web.Authentication;

public record ExternalIdentity(
    string Provider,
    string? Subject,
    string Username,
    string? Email,
    string? SessionId,
    string? IdpRefreshToken,
    DateTimeOffset? IdpRefreshExpirationUtc,
    IReadOnlyDictionary<string, string> Claims
);