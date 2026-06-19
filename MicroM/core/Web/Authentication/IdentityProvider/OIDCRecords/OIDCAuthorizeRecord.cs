namespace MicroM.Web.Authentication.SSO;

public sealed record OIDCAuthorizeRecord
(
    string? RedirectUri,
    string? LoginRedirectUri
);

