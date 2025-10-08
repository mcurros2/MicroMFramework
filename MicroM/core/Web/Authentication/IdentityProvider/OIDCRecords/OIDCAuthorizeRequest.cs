namespace MicroM.Web.Authentication.SSO;

public sealed record OIDCAuthorizeRequest
(
    string response_type,
    string client_id,
    string? request_uri = null,
    string? redirect_uri = null,
    string? scope = null,
    string? state = null,
    string? nonce = null,
    string? display = null,
    string? prompt = null,
    string? max_age = null,
    string? ui_locales = null,
    string? id_token_hint = null,
    string? login_hint = null,
    string? acr_values = null,
    string? code_challenge = null,
    string? code_challenge_method = null
);


