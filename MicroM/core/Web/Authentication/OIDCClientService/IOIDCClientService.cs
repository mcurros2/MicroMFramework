using MicroM.Configuration;
using MicroM.Core;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Headers;

namespace MicroM.Web.Authentication.SSO;

public interface IOIDCClientService
{
    // Client JWKS: mirrors IdP JWKS pattern, returns ETag-aware response info
    EtagCacheServiceCacheCheckResult? HandleClientJwks(ApplicationOption app, RequestHeaders request_headers, IHeaderDictionary response_headers);

    // Client-side PAR forwarder: returns (statusCode, contentType, body)
    Task<OIDCHttpClientPostResponse> SignInOidc(ApplicationOption app, IHeaderDictionary requestHeaders, IFormCollection form, CancellationToken ct);

    // OIDC authorization code callback: exchanges code at IdP /token (PKCE), validates id_token, returns a local ClaimsPrincipal
    Task<ResultWithStatus<OIDCClientCallbackResult, string>> HandleSignInOidcCallback(
        ApplicationOption app,
        string code,
        string redirectUri,
        string codeVerifier,
        string state,
        CancellationToken ct);

    Task HandleSignOut(ApplicationOption app, string id_token_hint, string? post_logout_redirect_uri, string? state, CancellationToken ct);
}
