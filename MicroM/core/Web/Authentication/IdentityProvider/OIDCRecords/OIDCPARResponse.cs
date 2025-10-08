namespace MicroM.Web.Authentication.SSO;

public sealed record OIDCPARResponse(
    string request_uri,
    int expires_in
);


