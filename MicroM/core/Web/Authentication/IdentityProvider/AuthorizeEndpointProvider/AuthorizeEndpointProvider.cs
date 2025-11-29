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
            response_type: query_string[WellknownIdentityConstants.ResponseType].ToString(),
            client_id: query_string[WellknownIdentityConstants.ClientId].ToString(),
            redirect_uri: query_string[WellknownIdentityConstants.RedirectUri].ToString(),
            scope: query_string[WellknownIdentityConstants.Scope].ToString(),
            state: query_string[WellknownIdentityConstants.State].ToString(),
            nonce: query_string[WellknownIdentityConstants.Nonce].ToString(),
            display: query_string[WellknownIdentityConstants.Display].ToString(),
            prompt: query_string[WellknownIdentityConstants.Prompt].ToString(),
            max_age: query_string[WellknownIdentityConstants.MaxAge].ToString(),
            ui_locales: query_string[WellknownIdentityConstants.UiLocales].ToString(),
            id_token_hint: query_string[WellknownIdentityConstants.IdTokenHint].ToString(),
            login_hint: query_string[WellknownIdentityConstants.LoginHint].ToString(),
            acr_values: query_string[WellknownIdentityConstants.AcrValues].ToString(),
            request: query_string[WellknownIdentityConstants.Request].ToString(),
            request_uri: query_string[WellknownIdentityConstants.RequestUri].ToString()
        );

        return auth_request;
    }

    private static ResultWithStatus<bool, ErrorResult> ValidateRequestObjectClaims(
        JsonWebToken requestObjectJwt,
        string clientId,
        OIDCWellKnownResponse wellKnown)
    {
        // iss MUST equal client_id
        if (!string.Equals(requestObjectJwt.Issuer, clientId, StringComparison.Ordinal))
        {
            return new(false, new("invalid_request_object", "iss in request object must equal client_id"));
        }

        // sub, if present, MUST equal client_id
        var sub = requestObjectJwt.Subject;
        if (!string.IsNullOrEmpty(sub) && !string.Equals(sub, clientId, StringComparison.Ordinal))
        {
            return new(false, new("invalid_request_object", "sub in request object must equal client_id when present"));
        }

        // aud MUST contain AS issuer or authorization_endpoint (FAPI 2.0 allows issuer or endpoint)
        var audiences = requestObjectJwt.Audiences?.ToList() ?? [];
        var acceptedAudiences = new[]
        {
            wellKnown.issuer,
            wellKnown.authorization_endpoint,
            wellKnown.pushed_authorization_request_endpoint,
            wellKnown.token_endpoint
        }.Where(a => !string.IsNullOrEmpty(a)).ToHashSet(StringComparer.OrdinalIgnoreCase);

        if (!audiences.Any(aud => acceptedAudiences.Contains(aud)))
        {
            return new(false, new("invalid_request_object", "aud in request object does not match this authorization server"));
        }

        // Lifetime: exp / nbf
        var now = DateTimeOffset.UtcNow;
        var expClaim = requestObjectJwt.GetClaim(JwtRegisteredClaimNames.Exp)?.Value;
        var nbfClaim = requestObjectJwt.GetClaim(JwtRegisteredClaimNames.Nbf)?.Value;

        if (expClaim != null && long.TryParse(expClaim, out var expSeconds))
        {
            var exp = DateTimeOffset.FromUnixTimeSeconds(expSeconds);
            if (exp < now)
            {
                return new(false, new("invalid_request_object", "request object is expired"));
            }

            // opcional: max lifetime window (ej. 10 minutos)
            if (exp - now > TimeSpan.FromMinutes(10))
            {
                return new(false, new("invalid_request_object", "request object lifetime exceeds policy window"));
            }
        }

        if (nbfClaim != null && long.TryParse(nbfClaim, out var nbfSeconds))
        {
            var nbf = DateTimeOffset.FromUnixTimeSeconds(nbfSeconds);
            // leve clock skew permitido
            if (nbf - now > TimeSpan.FromMinutes(1))
            {
                return new(false, new("invalid_request_object", "request object not yet valid (nbf in the future)"));
            }
        }

        return new(true, null);
    }





    public static async Task<ResultWithStatus<OIDCAuthorizeRequest, ErrorResult>> ValidateAndOverrideWithPARAuthorizationRequest(
        ApplicationOption app,
        X509Certificate2 idp_cert,
        OIDCWellKnownResponse idp_wellknown,
        IIdPClientSigningKeysCacheService clientSigningKeysCache,
        IPushedAuthorizationService par_service,
        IQueryCollection query_string,
        CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        var authorize_request = GetAuthorizeRequest(query_string);

        bool requirePar = OIDCCryptoCapabilities.Idp.RequirePushedAuthorizationRequests;
        bool hasRequestUri = !string.IsNullOrEmpty(authorize_request.request_uri);
        bool hasRequest = !string.IsNullOrEmpty(authorize_request.request);


        // We do not support Request Object by value on /oauth2/authorize.
        // Clients must either:
        //  - send plain query parameters, or
        //  - use PAR: POST to /oauth2/par (with or without 'request') and then call
        //    /oauth2/authorize with the returned 'request_uri'.
        if (hasRequest)
        {
            return new(null, new("invalid_request", "request parameter is not supported at authorize endpoint; use PAR (request_uri) instead."));
        }

        if (requirePar && !hasRequestUri)
        {
            return new(null, new("invalid_request", "Pushed authorization request is required: call /oauth2/par first and pass 'request_uri' to /oauth2/authorize."));
        }

        // PAR
        if (hasRequestUri)
        {
            var (pushed, rawJwt, stored_header) = par_service.ConsumeRequest(authorize_request.request_uri!);

            if (pushed == null)
            {
                return new(null, new("invalid_request", "request_uri not found or expired"));
            }

            if (string.IsNullOrEmpty(pushed.client_id))
            {
                return new(null, new("invalid_request", "Missing client_id in pushed request"));
            }

            // client_id must match the authenticated client used at PAR time
            if (!string.IsNullOrEmpty(authorize_request.client_id) && authorize_request.client_id != pushed.client_id)
            {
                return new(null, new("invalid_request", "client_id in authorize request does not match PAR client_id"));
            }

            // valid response type
            if (string.IsNullOrEmpty(pushed.response_type) || pushed.response_type != WellknownIdentityConstants.Code)
            {
                return new(null, new("invalid_request", "Invalid response type in PAR; only 'code' is supported."));
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

                    // safety cap against JWE inflation attacks
                    const int MAX_REQUEST_OBJECT_PAYLOAD_CHARS = 64 * 1024;

                    var rawPayloadLength = (decryptResult.Result.EncodedPayload ?? string.Empty).Length;
                    if (rawPayloadLength > MAX_REQUEST_OBJECT_PAYLOAD_CHARS)
                    {
                        return new(null, new("invalid_request_object", "request object payload exceeds size limit"));
                    }

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
                if (!string.IsNullOrEmpty(roClientId) && roClientId != pushed.client_id)
                {
                    return new(null, new("invalid_request", "client_id in request object does not match authenticated client"));
                }

                var (ok, claimError) = ValidateRequestObjectClaims(requestObjectJwt, pushed.client_id!, idp_wellknown);

                if (!ok)
                {
                    return new(null, claimError);
                }

                // 5) Build the effective authorize request from Request Object + PAR data + original QS
                string? roResponseType = requestObjectJwt.Claims.FirstOrDefault(c => c.Type == WellknownIdentityConstants.ResponseType)?.Value;
                string? roRedirectUri = requestObjectJwt.Claims.FirstOrDefault(c => c.Type == WellknownIdentityConstants.RedirectUri)?.Value;
                string? roScope = requestObjectJwt.Claims.FirstOrDefault(c => c.Type == WellknownIdentityConstants.Scope)?.Value;
                string? roState = requestObjectJwt.Claims.FirstOrDefault(c => c.Type == WellknownIdentityConstants.State)?.Value;
                string? roNonce = requestObjectJwt.Claims.FirstOrDefault(c => c.Type == WellknownIdentityConstants.Nonce)?.Value;
                string? roCodeChallenge = requestObjectJwt.Claims.FirstOrDefault(c => c.Type == WellknownIdentityConstants.CodeChallenge)?.Value;
                string? roCodeChallengeMethod = requestObjectJwt.Claims.FirstOrDefault(c => c.Type == WellknownIdentityConstants.CodeChallengeMethod)?.Value;

                // enforce that if state/nonce appear both in RO and query, they match
                if (!string.IsNullOrEmpty(roState) &&
                    !string.IsNullOrEmpty(authorize_request.state) &&
                    roState != authorize_request.state)
                {
                    return new(null, new("invalid_request", "state in request object does not match authorize request"));
                }

                if (!string.IsNullOrEmpty(roNonce) &&
                    !string.IsNullOrEmpty(authorize_request.nonce) &&
                    roNonce != authorize_request.nonce)
                {
                    return new(null, new("invalid_request", "nonce in request object does not match authorize request"));
                }

                // Critical fields: only RO and/or PAR, no query fallback
                var finalResponseType = roResponseType ?? pushed.response_type;
                var finalRedirectUri = roRedirectUri ?? pushed.redirect_uri;
                var finalScope = roScope ?? pushed.scope;
                var finalState = roState ?? pushed.state;
                var finalNonce = roNonce ?? pushed.nonce;
                var finalCodeChallenge = roCodeChallenge ?? pushed.code_challenge;
                var finalCodeMethod = roCodeChallengeMethod ?? pushed.code_challenge_method;

                if (string.IsNullOrEmpty(finalResponseType) || !string.Equals(finalResponseType, WellknownIdentityConstants.Code, StringComparison.OrdinalIgnoreCase))
                {
                    return new(null, new("invalid_request", "Missing or invalid response_type in PAR/Request Object"));
                }

                if (string.IsNullOrEmpty(finalRedirectUri))
                    return new(null, new("invalid_request", "Missing redirect_uri in PAR/Request Object"));

                if (string.IsNullOrEmpty(finalScope))
                    return new(null, new("invalid_request", "Missing scope in PAR/Request Object"));

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
                    state: pushed.state,
                    nonce: pushed.nonce,
                    scope: pushed.scope,
                    code_challenge: pushed.code_challenge,
                    code_challenge_method: pushed.code_challenge_method
                );
            }

            // Enforce that query string state/nonce cannot contradict values sent at PAR time
            if (!string.IsNullOrEmpty(pushed.state) && !string.IsNullOrEmpty(authorize_request.state) && pushed.state != authorize_request.state)
            {
                return new(null, new("invalid_request", "state in authorize request does not match PAR state"));
            }

            if (!string.IsNullOrEmpty(pushed.nonce) && !string.IsNullOrEmpty(authorize_request.nonce) && pushed.nonce != authorize_request.nonce)
            {
                return new(null, new("invalid_request", "nonce in authorize request does not match PAR nonce"));
            }

        }

        // Basic validation
        if (string.IsNullOrEmpty(authorize_request.response_type) ||
            !string.Equals(authorize_request.response_type, WellknownIdentityConstants.Code, StringComparison.OrdinalIgnoreCase))
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
