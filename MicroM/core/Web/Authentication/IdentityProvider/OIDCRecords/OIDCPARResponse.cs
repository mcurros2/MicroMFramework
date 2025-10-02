namespace MicroM.Web.Authentication.SSO;

public record OIDCPARResponse(
    string request_uri,
    int expires_in
);


