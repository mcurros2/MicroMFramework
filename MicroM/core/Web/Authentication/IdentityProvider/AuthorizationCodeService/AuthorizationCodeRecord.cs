namespace MicroM.Web.Authentication.SSO;

public record AuthorizationCodeRecord(
    string Code,
    string ClientId,
    string RedirectUri,
    string UserId,
    string? Sid,
    string? CodeChallenge,
    string? CodeChallengeMethod,
    DateTimeOffset ExpiresAt
);

