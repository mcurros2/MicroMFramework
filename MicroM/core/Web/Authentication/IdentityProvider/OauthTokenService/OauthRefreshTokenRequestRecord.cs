namespace MicroM.Web.Authentication.SSO;

public record OauthRefreshTokenRequestRecord
(
    string grant_type,
    string refresh_token,
    string client_id
);

