namespace MicroM.Web.Services;

public static class MicroMServicesConstants
{
    public const string JWTCookiePolicy = "JwtCookie";
    public const string JWTCookiePolicyDisplayName = "Jwt/Cookie";
    public const string AuthenticationCookieName = "microm-a";
    public const string IdPClientSchemeDisplayName = "IdP Client auth";
    public const string IdPClientScheme = "IdPClient";

    public const string RateLimitingRefreshPolicy = "RateLimitingRefreshPolicy";
    public const string RateLimitingAuthLoginPolicy = "RateLimitingAuthLoginPolicy";
    public const string RateLimitingAuthRecoveryPolicy = "RateLimitingAuthRecoveryPolicy";
    public const string RateLimitingAuthIsLoggedInPolicy = "RateLimitingAuthIsLoggedInPolicy";
    public const string RateLimitingAuthLogoffPolicy = "RateLimitingAuthLogoffPolicy";

    public const string RateLimitingOidcPARPolicy = "RatelimitingOidcPARPolicy";
    public const string RateLimitingOidcTokenPolicy = "RateLimitingOidcTokenPolicy";
    public const string RateLimitingOidcAuthorizePolicy = "RateLimitingOidcAuthorizePolicy";
    public const string RateLimitingOidcEndSessionPolicy = "RateLimitingOidcEndSessionPolicy";
    public const string RateLimitingOidcMetadataPolicy = "RateLimitingOidcMetadataPolicy";

    public const string RateLimitingBackchannelLogoutPolicy = "RateLimitingBackchannelLogoutPolicy";
    public const string RateLimitingFrontchannelLogoutPolicy = "RateLimitingFrontchannelLogoutPolicy";

    public const string RateLimitingPublicGetPolicy = "RateLimitingPublicGetPolicy";
    public const string RateLimitingPublicMutationPolicy = "RateLimitingPublicMutationPolicy";
}
