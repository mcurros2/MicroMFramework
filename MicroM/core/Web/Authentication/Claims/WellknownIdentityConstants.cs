namespace MicroM.Web.Authentication;

public class WellknownIdentityConstants
{
    public const string PreferredUsername = "preferred_username";
    public const string NameIdentifier = "nameid";
    public const string SubjectIdentifier = "sub";
    public const string IdToken = "id_token";
    public const string RefreshToken = "refresh_token";
    public const string ClientId = "client_id";
    public const string Nonce = "nonce";
    public const string State = "state";
    public const string SessionIdentifier = "sid";
    public const string Code = "code";
    public const string CodeVerifier = "code_verifier";
    public const string RequestUri = "request_uri";
    public const string RedirectUri = "redirect_uri";
    public const string LocalDeviceId = "local_device_id";
    public const string AuthorizedParty = "azp";
    public const string Audience = "aud";
    public const string Issuer = "iss";
    public const string IssuedAt = "iat";
    public const string Events = "events";
    public const string JWTID = "jti";
    public const string Expiration = "exp";
    public const string NotBefore = "nbf";
    public const string Scope = "scope";
    public const string LogoutToken = "logout_token";

    public const string BackchannelLogoutEventUri = "http://schemas.openid.net/event/backchannel-logout";
}
