namespace MicroM.Web.Authentication.SSO;

public record OIDCTokenResponse(
    string token_type,
    int expires_in,
    string access_token,
    string? refresh_token = null,
    string? id_token = null,
    string scope = "openid"
);
