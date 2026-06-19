using System.Security.Claims;

namespace MicroM.Web.Authentication.SSO;

public sealed record OIDCClientCallbackResult(
    ClaimsPrincipal? Principal,
    DateTimeOffset? ExpiresUtc,
    string? IdpRefreshToken,
    string? DeviceId,
    DateTimeOffset? IdpRefreshExpirationUtc,
    string? TargetLinkURI
);