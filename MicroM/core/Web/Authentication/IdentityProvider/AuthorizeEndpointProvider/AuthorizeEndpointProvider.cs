using MicroM.Configuration;
using MicroM.Core;
using Microsoft.AspNetCore.Http;
using Microsoft.IdentityModel.JsonWebTokens;
using System.Security.Cryptography.X509Certificates;

namespace MicroM.Web.Authentication.SSO;

public static class AuthorizeEndpointProvider
{

    private static OIDCAuthorizeRequest GetAuthorizeRequest(IQueryCollection query_string)
    {
        OIDCAuthorizeRequest auth_request = new
        (
            response_type: query_string["response_type"],
            client_id: query_string["client_id"],
            redirect_uri: query_string["redirect_uri"],
            scope: query_string["scope"],
            state: query_string["state"],
            nonce: query_string["nonce"],
            display: query_string["display"],
            prompt: query_string["prompt"],
            max_age: query_string["max_age"],
            ui_locales: query_string["ui_locales"],
            id_token_hint: query_string["id_token_hint"],
            login_hint: query_string["login_hint"],
            acr_values: query_string["acr_values"]
        );

        return auth_request;
    }

    public static async Task<ResultWithStatus<OIDCAuthorizeRequest, ErrorResult>> ValidateAndOverrideWithPARAuthorizationRequest(
        ApplicationOption app,
        X509Certificate2 idp_cert,
        IIdPClientSigningKeysCacheService clientSigningKeysCache,
        IPushedAuthorizationService par_service,
        IQueryCollection query_string,
        CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        var authorize_request = GetAuthorizeRequest(query_string);

        // PAR
        if (!string.IsNullOrEmpty(authorize_request.request_uri))
        {
            var (pushed, rawJwt, stored_header) = par_service.ConsumeRequest(authorize_request.request_uri);

            if (pushed == null)
            {
                return new(null, new("invalid_request", "request_uri not found or expired"));
            }

            if (string.IsNullOrEmpty(pushed.client_id))
            {
                return new(null, new("invalid_request", "Missing client_id in pushed request"));
            }

            // client_id must match the authenticated client used at PAR time
            if (!string.IsNullOrEmpty(authorize_request.client_id) &&
                !string.Equals(authorize_request.client_id, pushed.client_id, StringComparison.Ordinal))
            {
                return new(null, new("invalid_request", "client_id in authorize request does not match PAR client_id"));
            }

            // If we have a Request Object, decrypt (if JWE) and validate its signature
            JsonWebToken? requestObjectJwt = null;

            if (!string.IsNullOrEmpty(rawJwt))
            {
                // 1) Get client signing keys (from hosted cert or client JWKS)
                var signingKeys = await clientSigningKeysCache
                    .GetClientSigningKeysAsync(app, pushed.client_id, ct)
                    .ConfigureAwait(false);

                if (signingKeys == null || signingKeys.Count == 0)
                {
                    return new(null, new("invalid_request_object", "No client signing keys available for request object"));
                }

                // 2) Use stored header from PAR validation
                var header = stored_header;

                if (header == null)
                {
                    return new(null, new("invalid_request_object", "request_object not available from PAR"));
                }

                if (!string.IsNullOrEmpty(header.ParseError))
                {
                    return new(null, new("invalid_request_object", $"request_object header parse error: {header.ParseError}"));
                }

                string signedRequestJwt;

                if (header.IsJwe)
                {
                    // Decrypt JWE -> inner signed JWS
                    var decryptResult = await JwksProvider
                        .DecryptRequestObjectAsync(app, rawJwt, idp_cert, ct)
                        .ConfigureAwait(false);

                    if (decryptResult.Result == null)
                    {
                        var msg = decryptResult.Status ?? "request_object decryption failed";
                        return new(null, new("invalid_request_object", msg));
                    }

                    // EncodedToken here is the decrypted JWS compact string.
                    signedRequestJwt = decryptResult.Result.EncodedToken;
                }
                else
                {
                    // Already a plain signed JWS
                    signedRequestJwt = rawJwt;
                }

                // 3) Validate signature of the (possibly decrypted) Request Object
                var validateResult = await JwksProvider
                    .ValidateSignedRequestObjectAsync(signedRequestJwt, signingKeys, ct)
                    .ConfigureAwait(false);

                if (validateResult.Result == null)
                {
                    var msg = validateResult.Status ?? "request_object signature validation failed";
                    return new(null, new("invalid_request_object", msg));
                }

                requestObjectJwt = validateResult.Result;

                // 4) Enforce that client_id in the Request Object (if present) matches the authenticated client
                var roClientId = requestObjectJwt.Claims.FirstOrDefault(c => c.Type == "client_id")?.Value;
                if (!string.IsNullOrEmpty(roClientId) &&
                    !string.Equals(roClientId, pushed.client_id, StringComparison.Ordinal))
                {
                    return new(null, new("invalid_request", "client_id in request object does not match authenticated client"));
                }

                // 5) Build the effective authorize request from Request Object + PAR data + original QS
                string? roResponseType = requestObjectJwt.Claims.FirstOrDefault(c => c.Type == "response_type")?.Value;
                string? roRedirectUri = requestObjectJwt.Claims.FirstOrDefault(c => c.Type == "redirect_uri")?.Value;
                string? roScope = requestObjectJwt.Claims.FirstOrDefault(c => c.Type == "scope")?.Value;
                string? roState = requestObjectJwt.Claims.FirstOrDefault(c => c.Type == "state")?.Value;
                string? roNonce = requestObjectJwt.Claims.FirstOrDefault(c => c.Type == "nonce")?.Value;
                string? roCodeChallenge = requestObjectJwt.Claims.FirstOrDefault(c => c.Type == "code_challenge")?.Value;
                string? roCodeChallengeMethod = requestObjectJwt.Claims.FirstOrDefault(c => c.Type == "code_challenge_method")?.Value;

                // enforce that if state/nonce appear both in RO and query, they match
                if (!string.IsNullOrEmpty(roState) &&
                    !string.IsNullOrEmpty(authorize_request.state) &&
                    !string.Equals(roState, authorize_request.state, StringComparison.Ordinal))
                {
                    return new(null, new("invalid_request", "state in request object does not match authorize request"));
                }

                if (!string.IsNullOrEmpty(roNonce) &&
                    !string.IsNullOrEmpty(authorize_request.nonce) &&
                    !string.Equals(roNonce, authorize_request.nonce, StringComparison.Ordinal))
                {
                    return new(null, new("invalid_request", "nonce in request object does not match authorize request"));
                }

                // Precedence:
                //   response_type / redirect_uri / scope: RO > PAR > original
                //   state: query > RO > PAR (query wins)
                var finalResponseType = roResponseType ?? pushed.response_type ?? authorize_request.response_type;
                var finalRedirectUri = roRedirectUri ?? pushed.redirect_uri ?? authorize_request.redirect_uri;
                var finalScope = roScope ?? pushed.scope ?? authorize_request.scope;

                var finalState = roState ?? pushed.state ?? authorize_request.state;
                var finalNonce = roNonce ?? pushed.nonce ?? authorize_request.nonce;

                var finalCodeChallenge = roCodeChallenge ?? pushed.code_challenge;
                var finalCodeMethod = roCodeChallengeMethod ?? pushed.code_challenge_method;

                authorize_request = new
                (
                    response_type: finalResponseType,
                    client_id: pushed.client_id,
                    redirect_uri: finalRedirectUri,
                    scope: finalScope,
                    state: finalState,
                    nonce: finalNonce,
                    display: authorize_request.display,
                    prompt: authorize_request.prompt,
                    max_age: authorize_request.max_age,
                    ui_locales: authorize_request.ui_locales,
                    id_token_hint: authorize_request.id_token_hint,
                    login_hint: authorize_request.login_hint,
                    acr_values: authorize_request.acr_values,
                    code_challenge: finalCodeChallenge,
                    code_challenge_method: finalCodeMethod
                );
            }
            else
            {
                // PAR without Request Object: copy pushed fields
                authorize_request = new
                (
                    client_id: pushed.client_id,
                    response_type: pushed.response_type,
                    redirect_uri: pushed.redirect_uri,
                    state: string.IsNullOrEmpty(authorize_request.state) ? pushed.state : authorize_request.state,
                    nonce: string.IsNullOrEmpty(authorize_request.nonce) ? pushed.nonce : authorize_request.nonce,
                    scope: pushed.scope,
                    code_challenge: pushed.code_challenge,
                    code_challenge_method: pushed.code_challenge_method
                );
            }
        }

        // Basic validation
        if (string.IsNullOrEmpty(authorize_request.response_type) || !string.Equals(authorize_request.response_type, "code", StringComparison.OrdinalIgnoreCase))
        {
            return new(null, new("unsupported_response_type", "response_type must be 'code'"));
        }

        if (string.IsNullOrEmpty(authorize_request.client_id))
        {
            return new(null, new("invalid_request", "client_id is required"));
        }

        if (string.IsNullOrEmpty(authorize_request.redirect_uri))
        {
            return new(null, new("invalid_request", "redirect_uri is required"));
        }

        // Validate client registration and redirect_uri
        if (app.OIDCClientConfiguration == null || !app.OIDCClientConfiguration.TryGetValue(authorize_request.client_id, out var clientCfg))
        {
            return new(null, new("invalid_client", "Unknown client_id"));
        }

        if (clientCfg.URLAuthorizedRedirects == null || clientCfg.URLAuthorizedRedirects.Count == 0)
        {
            return new(null, new("invalid_request", "Client has no registered redirect URIs"));
        }

        var matched = clientCfg.URLAuthorizedRedirects.Any(registered => par_service.RedirectUriMatches(registered, authorize_request.redirect_uri));
        if (!matched)
        {
            return new(null, new("invalid_request", "redirect_uri not registered for client"));
        }

        return new(authorize_request, null);
    }

    public static string BuildLoginURL(ApplicationOption app, IQueryCollection query, string request_base)
    {
        // build a login URL. Prefer FrontendURLS[0] if available, otherwise fallback to request_base + "/login"
        var frontend = app.FrontendURLS != null && app.FrontendURLS.Count > 0 ? app.FrontendURLS[0] : null;
        string loginBase = !string.IsNullOrEmpty(frontend) ? frontend.TrimEnd('/') : request_base.TrimEnd('/');

        // Recreate original authorize URL to return to after login
        var q = System.Web.HttpUtility.ParseQueryString(string.Empty);
        foreach (var k in query.Keys) q[k] = query[k].ToString();
        var returnTo = $"{request_base}/oauth2/authorize?{q}";

        var loginUrl = $"{loginBase}/login?return_to={Uri.EscapeDataString(returnTo)}";
        return loginUrl;
    }

    public static string BuildRedirectURI(string redirect_uri, string? state, string code)
    {
        var separator = redirect_uri.Contains('?') ? '&' : '?';
        var redirectWithCode = $"{redirect_uri}{separator}code={Uri.EscapeDataString(code)}";
        if (!string.IsNullOrEmpty(state))
        {
            redirectWithCode += $"&state={Uri.EscapeDataString(state)}";
        }
        return redirectWithCode;
    }
}
