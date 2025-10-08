namespace MicroM.Web.Authentication.SSO;

public enum OIDCLogoutProcessingStatus
{
    Success,
    InvalidSignature,
    InvalidAudience,
    InvalidIssuer,
    MissingEvent,
    MissingSidOrSub,
    Expired,
    Replay,
    AlreadyProcessed,
    UnknownSid,
    SessionStoreError
}

public sealed record OIDCBackchannelLogoutResult(OIDCLogoutProcessingStatus Status, string? Error);

public sealed record OIDCFrontChannelLogoutInitiation(string EndSessionUrl, string? State);

public sealed record OIDCSessionInvalidationResult(int SessionsRemoved, int DevicesAffected);

public sealed record OIDCLogoutTokenParseResult(
    string Issuer,
    string Audience,
    string Jti,
    DateTimeOffset IssuedAt,
    DateTimeOffset? ExpiresAt,
    string? Sid,
    string? Subject,
    IReadOnlyDictionary<string, string> RawClaims);