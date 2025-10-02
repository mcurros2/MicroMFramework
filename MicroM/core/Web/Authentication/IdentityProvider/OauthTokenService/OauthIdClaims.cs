namespace MicroM.Web.Authentication.SSO;

public record OauthIdClaims(
    string? sub,
    string? name
);

