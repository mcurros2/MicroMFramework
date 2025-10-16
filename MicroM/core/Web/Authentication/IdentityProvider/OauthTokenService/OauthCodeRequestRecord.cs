namespace MicroM.Web.Authentication.SSO;

public record OauthCodeRequestRecord(
    string grant_type,
    string code,
    string redirect_uri,
    string? code_verifier,
    string client_id
);


