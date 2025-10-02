namespace MicroM.Web.Authentication;

public record ExternalIdentity(
    string Provider,
    string Subject,
    string Username,
    string? Email,
    string? Sid,
    string? IdpRefreshToken,
    IReadOnlyDictionary<string, string> Claims
);