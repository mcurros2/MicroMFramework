namespace MicroM.Web.Authentication.SSO;

public record PushedAuthorizationRequest(
    string? client_id,
    string? response_type,
    string? redirect_uri,
    string? scope,
    string? state,
    string? code_challenge,
    string? code_challenge_method,
    string? request
    );


