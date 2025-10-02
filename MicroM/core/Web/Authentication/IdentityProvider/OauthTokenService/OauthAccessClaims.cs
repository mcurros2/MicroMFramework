namespace MicroM.Web.Authentication.SSO;

public record OauthAccessClaims(
    string sub,
    string azp,
    string scope = "openid"
    );


