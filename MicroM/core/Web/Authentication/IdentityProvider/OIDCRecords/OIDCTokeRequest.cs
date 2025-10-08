namespace MicroM.Web.Authentication.SSO;

public sealed record OIDCTokenRequest
(
    OIDCGrantType grant_type,
    string? code = null,
    string? redirect_uri = null,
    string? client_id = null,
    string? client_secret = null,
    string? code_verifier = null,
    string? refresh_token = null,
    string? scope = null,
    string? assertion = null,
    string? client_assertion_type = "urn:ietf:params:oauth:client-assertion-type:jwt-bearer",
    string? client_assertion = null
    );

