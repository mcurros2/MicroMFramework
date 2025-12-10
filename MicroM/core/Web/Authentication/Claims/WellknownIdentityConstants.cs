namespace MicroM.Web.Authentication;

public class WellknownIdentityConstants
{
    public const string PreferredUsername = "preferred_username";
    public const string NameIdentifier = "nameid";
    public const string SubjectIdentifier = "sub";
    public const string IdToken = "id_token";
    public const string IdTokenHint = "id_token_hint";
    public const string RefreshToken = "refresh_token";
    public const string ClientId = "client_id";
    public const string Nonce = "nonce";
    public const string State = "state";
    public const string Request = "request";
    public const string SessionIdentifier = "sid";
    public const string Code = "code";
    public const string CodeVerifier = "code_verifier";
    public const string RequestUri = "request_uri";
    public const string RedirectUri = "redirect_uri";
    public const string TargetLinkUri = "target_link_uri";
    public const string LocalDeviceId = "local_device_id";
    public const string AuthorizedParty = "azp";
    public const string Audience = "aud";
    public const string Issuer = "issuer";
    public const string IssuerClaim = "iss";
    public const string IssuedAt = "iat";
    public const string Events = "events";
    public const string JWTID = "jti";
    public const string Expiration = "exp";
    public const string ExpiresIn = "expires_in";
    public const string NotBefore = "nbf";
    public const string Scope = "scope";
    public const string LogoutToken = "logout_token";
    public const string Oidc = "oidc";
    public const string AuthorizationCode = "authorization_code";
    public const string GrantType = "grant_type";
    public const string Bearer = "Bearer";
    public const string OpenID = "openid";
    public const string ClientAssertion = "client_assertion";
    public const string ClientAssertionType = "client_assertion_type";
    public const string RefreshExpirationUtc = "refresh_expiration_utc";
    public const string ClientSecret = "client_secret";
    public const string AuthorizeUrl = "authorize_url";
    public const string TokenType = "token_type";
    public const string AccessToken = "access_token";

    public const string Display = "display";
    public const string Prompt = "prompt";
    public const string MaxAge = "max_age";
    public const string LoginHint = "login_hint";
    public const string UiLocales = "ui_locales";
    public const string AcrValues = "acr_values";

    public const string ResponseType = "response_type";
    public const string CodeChallenge = "code_challenge";
    public const string CodeChallengeMethod = "code_challenge_method";
    public const string Token = "token";
    public const string TokenTypeHint = "token_type_hint";
    public const string Keys = "keys";

    public const string BackchannelLogoutEventUri = "http://schemas.openid.net/event/backchannel-logout";
    public const string BackchannelLogoutEventJson = $"{{\"{WellknownIdentityConstants.BackchannelLogoutEventUri}\":{{}}}}";

    public const string PushedAuthorizationRequestEndpoint = "pushed_authorization_request_endpoint";
    public const string AuthorizationEndpoint = "authorization_endpoint";
    public const string TokenEndpoint = "token_endpoint";
    public const string DeviceAuthorizationEndpoint = "device_authorization_endpoint";
    public const string EndSessionEndpoint = "end_session_endpoint";
    public const string UserinfoEndpoint = "userinfo_endpoint";
    public const string RevocationEndpoint = "revocation_endpoint";
    public const string IntrospectionEndpoint = "introspection_endpoint";
    public const string JwksUri = "jwks_uri";

    public const string IdTokenSigningAlgValuesSupported = "id_token_signing_alg_values_supported";
    public const string TokenEndpointAuthSigningAlgValuesSupported = "token_endpoint_auth_signing_alg_values_supported";
    public const string RevocationEndpointAuthSigningAlgValuesSupported = "revocation_endpoint_auth_signing_alg_values_supported";
    public const string IntrospectionEndpointAuthSigningAlgValuesSupported = "introspection_endpoint_auth_signing_alg_values_supported";
    public const string UserinfoEndpointSigningAlgValuesSupported = "userinfo_signing_alg_values_supported";
    public const string RequestObjectSigningAlgValuesSupported = "request_object_signing_alg_values_supported";

    public const string IdTokenEncryptionAlgValuesSupported = "id_token_encryption_alg_values_supported";
    public const string IdTokenEncryptionEncValuesSupported = "id_token_encryption_enc_values_supported";
    public const string UserinfoEncryptionAlgValuesSupported = "userinfo_encryption_alg_values_supported";
    public const string UserinfoEncryptionEncValuesSupported = "userinfo_encryption_enc_values_supported";
    public const string RequestObjectEncryptionAlgValuesSupported = "request_object_encryption_alg_values_supported";
    public const string RequestObjectEncryptionEncValuesSupported = "request_object_encryption_enc_values_supported";
    public const string AuthorizationEncryptionAlgValuesSupported = "authorization_encryption_alg_values_supported";
    public const string AuthorizationEncryptionEncValuesSupported = "authorization_encryption_enc_values_supported";
    public const string PushedAuthorizationRequestEncryptionAlgValuesSupported = "pushed_authorization_request_encryption_alg_values_supported";
    public const string PushedAuthorizationRequestEncryptionEncValuesSupported = "pushed_authorization_request_encryption_enc_values_supported";

    public const string Kty = "kty";

    public const string ClientAssertionTypeJwtBearer = "urn:ietf:params:oauth:client-assertion-type:jwt-bearer";
    public const string Basic = "Basic";
}
