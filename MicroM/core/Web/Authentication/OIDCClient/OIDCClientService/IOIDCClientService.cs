using MicroM.Configuration;
using MicroM.Core;
using MicroM.Web.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Headers;

namespace MicroM.Web.Authentication.SSO;

public sealed record OIDCInitiateLoginRequest(
        string Iss,
        string? LoginHint,
        string? TargetLinkUri
    );


public interface IOIDCClientService
{
    // Client JWKS: mirrors IdP JWKS pattern, returns ETag-aware response info
    EtagCacheServiceCacheCheckResult<OIDCJwksResponse>? HandleClientJwks(ApplicationOption app, RequestHeaders request_headers, IHeaderDictionary response_headers);

    // Client-side PAR forwarder: returns (statusCode, contentType, body)
    Task<OIDCHttpClientPostResponse> HandleOidcClientPAR(ApplicationOption app, string requestRootUrl, IHeaderDictionary requestHeaders, IFormCollection form, CancellationToken ct);

    // OIDC authorization code callback: exchanges code at IdP /token (PKCE), validates id_token, returns a local ClaimsPrincipal
    // Adds authorizationResponseIssuer (optional 'iss' param from authorization response) for mix-up mitigation.
    Task<ResultWithStatus<OIDCClientCallbackResult, string>> HandleAuthorizationCallback(
        ApplicationOption app,
        string code,
        string redirectUri,
        string state,
        string authorizationResponseIssuer,
        CancellationToken ct);

    // FRONT-CHANNEL LOGOUT INITIATION (Client side)
    Task<ResultWithStatus<OIDCFrontChannelLogoutInitiation, string>> BuildEndSessionRequest(
        ApplicationOption app,
        string idTokenHint,
        string? postLogoutRedirectUri,
        string? state,
        CancellationToken ct);

    // FRONT-CHANNEL LOGOUT COMPLETION (Client callback after IdP redirect, if using post_logout_redirect_uri)
    // Optionally clears local session if not already invalidated via backchannel.
    Task<ResultWithStatus<bool, string>> HandleFrontChannelLogout(
        ApplicationOption app,
        string? state,
        CancellationToken ct);

    // BACKCHANNEL LOGOUT (receiver)
    // Validates logout_token (signature, issuer, audience, events, iat/exp, sid/sub), enforces jti single-use,
    // invalidates local sessions/refresh tokens, and returns structured result.
    Task<OIDCBackchannelLogoutResult> HandleBackchannelLogout(
        ApplicationOption app,
        string logoutTokenJwt,
        CancellationToken ct);

    // IdP refresh_token grant for Strategy B fallback:
    // Calls IdP /oauth2/token with grant_type=refresh_token (private_key_jwt), validates id_token,
    // returns principal, expires, rotated IdP refresh token (or original if not rotated), and its UTC expiration.
    Task<bool> RefreshIdpToken(
        ApplicationOption app,
        string sid,
        string device_id,
        CancellationToken ct);

    // Initiate OIDC login request (Third party initiated login spec)
    Task<ResultWithStatus<string, string>> HandleInitiateLoginAsync(
            ApplicationOption app,
            OIDCInitiateLoginRequest request,
            HttpRequest httpRequest,
            CancellationToken ct);

}
