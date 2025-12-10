using MicroM.Configuration;
using MicroM.Core;
using MicroM.DataDictionary.Entities;
using MicroM.Extensions;
using MicroM.Web.Extensions;
using MicroM.Web.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Headers;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using System.Net.Mime;
using System.Security.Claims;
using System.Security.Cryptography.X509Certificates;
using System.Text.Json;

namespace MicroM.Web.Authentication.SSO;

public class OIDCClientService(
    IEtagCacheService<OIDCWellKnownResponse> wk_cache,
    IEtagCacheService<OIDCJwksResponse> client_jwks_cache,
    IEtagCacheService<OIDCJwksResponse> remote_jwks_cache,
    IApplicationCertificateCacheService certificate_cache,
    IStateAndNonceService state_and_nonce_service,
    IOIDCHttpClient oidcHttpClient,
    IOIDCReplayCacheService replay_cache,
    ILogger<OIDCClientService> log,
    IDeviceIdService deviceid_service,
    IMicroMEncryption encryptor,
    IOptions<MicroMOptions> microMOptions
) : IOIDCClientService
{
    private readonly JsonSerializerOptions _jsonOptionsUnsafe = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull,
        Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };

    private PathString _api_path => new($"/{microMOptions.Value.MicroMAPIBaseRootPath}/");

    // Scrub helper: never log raw tokens, only length & segment count
    private static string DescribeToken(string? token)
    {
        if (string.IsNullOrWhiteSpace(token)) return "empty";
        var segs = token.Split('.');
        return $"segments={segs.Length} len={token.Length}";
    }

    public static string BuildJwksCacheKey(ApplicationOption app)
    {
        return $"oidc_client:{app.ApplicationID}_JWKS";
    }

    public static string BuildWellknownCacheKey(ApplicationOption client_app)
    {
        return $"oidc_client:{client_app.ApplicationID}_WK";
    }


    public EtagCacheServiceCacheCheckResult<OIDCJwksResponse>? HandleClientJwks(ApplicationOption app, RequestHeaders request_headers, IHeaderDictionary response_headers)
    {
        // Build JWKS for this client app using its certificate (kid = CertificateUniqueID)
        if (app.OIDCCertificateBlob == null || app.OIDCCertificateBlob.Length == 0)
        {
            log.LogWarning("CLIENT JWKS requested for app {app} which has no certificate configured", app.ApplicationID);
            return null;
        }

        string key = BuildJwksCacheKey(app);
        var result = client_jwks_cache.GetOrAddResponseWithCacheCheck(
            key,
            request_headers,
            response_headers,
            cache_duration_seconds: ConfigurationDefaults.JwksCacheDurationSeconds,
            (existing) =>
        {
            X509Certificate2? cert = certificate_cache.GetCertificate(app);
            OIDCJwksKeyResponse? k = cert != null ? JwksProvider.GetRSAKey(app, cert) : null;
            var jwks = new OIDCJwksResponse(keys: k != null ? [k] : []);
            return (json: JsonSerializer.Serialize(jwks, _jsonOptionsUnsafe), parsed: jwks, etag: null);
        });

        return result;
    }

    private async Task<ResultWithStatus<OIDCWellKnownResponse, string>> DiscoverWellKnown(ApplicationOption app, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(app.OIDCWellKnownURL))
        {
            return new(null, "IdP discovery URL not configured for this client app");
        }

        try
        {
            string key = BuildWellknownCacheKey(app);

            var wellknown_etag_content = await wk_cache.GetOrAddAsync(
                key,
                serveStaleOnError: true,
                ct: ct,
                valueFactory: async (existing, ct) =>
                {
                    var result = await oidcHttpClient.GetWellKnownJsonAsync(app.OIDCWellKnownURL, ct);

                    if (result.IsSuccessStatusCode)
                    {
                        try
                        {
                            var wellknown_response = JsonSerializer.Deserialize<OIDCWellKnownResponse>(result.Body, _jsonOptionsUnsafe);
                            return (json: result.Body, parsed: wellknown_response, etag: null);
                        }
                        catch (Exception ex)
                        {
                            log.LogError(ex, "Failed to parse IdP discovery document for app {app}", app.ApplicationID);
                        }
                    }
                    else
                    {
                        log.LogError("Failed to retrieve IdP discovery document for app {app} from {wkuri}: {status} {error}", app.ApplicationID, app.OIDCWellKnownURL, result.StatusCode, result.Error);
                    }

                    return (json: "", parsed: null, etag: null);
                }
            );

            if (string.IsNullOrEmpty(wellknown_etag_content.Content) || wellknown_etag_content.Parsed == null)
            {
                return new(null, "Failed to retrieve IdP discovery document");
            }

            return new(wellknown_etag_content.Parsed, null);
        }
        catch (Exception ex)
        {
            return new(null, $"Failed to discover IdP configuration: {ex.Message}");
        }
    }

    public async Task<OIDCHttpClientPostResponse> HandleOidcClientPAR(ApplicationOption app, IHeaderDictionary requestHeaders, IFormCollection form, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(app.OIDCWellKnownURL))
        {
            log.LogWarning("OIDC SignIn requested for app {app} which has no IdP discovery URL configured", app.ApplicationID);
            return new(400, false, MediaTypeNames.Application.Json, JsonSerializer.Serialize(new { error = "invalid_request", error_description = "IdP discovery URL not configured for this client app" }));
        }

        // Discover IdP PAR endpoint
        var (wellknown_result, wellknown_error) = await DiscoverWellKnown(app, ct);
        if (wellknown_error != null || wellknown_result == null)
        {
            log.LogWarning("Failed to discover IdP configuration for app {app}: {error}", app.ApplicationID, wellknown_error);
            return new(502, false, MediaTypeNames.Application.Json, JsonSerializer.Serialize(new { error = "server_error", error_description = "IdP discovery failed" }));
        }

        // Choose PKCE method based on IdP metadata
        var challengeMethod = OIDCCodeChallengeMethod.S256;
        var methods = wellknown_result.code_challenge_methods_supported;

        if (methods != null && methods.Count > 0)
        {
            if (methods.Contains(OIDCCodeChallengeMethod.S256))
            {
                challengeMethod = OIDCCodeChallengeMethod.S256;
            }
            else if (methods.Contains(OIDCCodeChallengeMethod.plain))
            {
                challengeMethod = OIDCCodeChallengeMethod.plain;
            }
            else
            {
                log.LogWarning("IdP for app {app} does not support S256 or plain PKCE methods", app.ApplicationID);
                return new(400, false, MediaTypeNames.Application.Json,
                    JsonSerializer.Serialize(new
                    {
                        error = "unsupported_operation",
                        error_description = "IdP PKCE methods are not compatible (no S256 or plain)"
                    }));
            }
        }

        string? parEndpoint = wellknown_result.pushed_authorization_request_endpoint;
        if (string.IsNullOrEmpty(parEndpoint))
        {
            log.LogWarning("IdP for app {app} does not expose a pushed_authorization_request_endpoint", app.ApplicationID);
            return new(400, false, MediaTypeNames.Application.Json, JsonSerializer.Serialize(new { error = "unsupported_operation", error_description = "IdP does not expose a pushed_authorization_request_endpoint" }));
        }

        // Local device ID (optional)
        string? providedDeviceId = form.TryGetValue(WellknownIdentityConstants.LocalDeviceId, out var d) ? d.ToString() : null;

        // Optional target_link_uri
        string? targetLinkUri = form.TryGetValue(WellknownIdentityConstants.TargetLinkUri, out var t) ? t.ToString() : null;

        if (!string.IsNullOrEmpty(targetLinkUri))
        {
            var (normalizedTargetLinkUri, targetLinkError) = OIDCClientServiceProvider.ValidateTargetLinkURIAllowed(targetLinkUri, app);
            if (targetLinkError != null)
            {
                log.LogWarning("Target link error: {targetLinkError} - {desc}", targetLinkError.Value.error, targetLinkError.Value.error_description);
                return new(400, false, MediaTypeNames.Application.Json, JsonSerializer.Serialize(targetLinkError.Value.error));
            }
            targetLinkUri = normalizedTargetLinkUri;
        }

        // Prepare state, nonce, PKCE
        var stateContext = state_and_nonce_service.EnsureStateNonceAndPkce(form, providedDeviceId, challengeMethod, targetLinkUri);

        // Validate and Prepare IdP request PAR body - forward incoming form params
        var (valid_form, form_error) = OIDCClientServiceProvider.ValidateClientSignInForm(app, stateContext.AdjustedForm!);

        if (form_error != null || valid_form == null)
        {
            log.LogWarning("Invalid OIDC SignIn form for app {app}: {error}", app.ApplicationID, form_error);
            return new(400, false, MediaTypeNames.Application.Json, JsonSerializer.Serialize(form_error?.error));
        }

        var forward = valid_form;

        // Remove local-only params
        forward.Remove(WellknownIdentityConstants.LocalDeviceId);
        forward.Remove(WellknownIdentityConstants.TargetLinkUri);

        X509Certificate2? cert = certificate_cache.GetCertificate(app);

        var (authHeader, authHeaderError) = PushedAuthorizationProvider.GetClientAuthorizationHeader(
            requestHeaders,
            cert,
            parEndpoint,
            forward,
            wellknown_result.pushed_authorization_request_endpoint_auth_methods_supported,
            wellknown_result.token_endpoint_auth_signing_alg_values_supported
            );

        if (authHeaderError != null)
        {
            log.LogWarning("Client authentication failed for app {app}: {error}", app.ApplicationID, authHeaderError);
            return new(400, false, MediaTypeNames.Application.Json, JsonSerializer.Serialize(new { error = "invalid_client", error_description = authHeaderError }));
        }

        // Build and send PAR request
        var parResult = await oidcHttpClient.PostPushedAuthorizationRequestAsync(parEndpoint, forward, authHeader, ct);

        // Store state and nonce associated with this client_id and request_uri
        if (parResult.IsSuccessStatusCode)
        {
            state_and_nonce_service.StoreStateCookie(app, app.JWTKey, stateContext.Data);

            // Build authorize_url for redirect
            if (!string.IsNullOrWhiteSpace(wellknown_result.authorization_endpoint))
            {
                try
                {
                    using var parDoc = JsonDocument.Parse(parResult.Body);
                    var requestUri = parDoc.RootElement.ReadString(WellknownIdentityConstants.RequestUri);

                    if (!string.IsNullOrWhiteSpace(requestUri))
                    {
                        var authorizeQuery = new Dictionary<string, string?>(StringComparer.Ordinal)
                        {
                            [WellknownIdentityConstants.ClientId] = app.ApplicationID,
                            [WellknownIdentityConstants.RequestUri] = requestUri
                        };

                        string authorizeUrl = QueryHelpers.AddQueryString(wellknown_result.authorization_endpoint, authorizeQuery);

                        // Return a JSON body with the authorize_url so the client can redirect
                        return new OIDCHttpClientPostResponse(
                            StatusCode: 200,
                            IsSuccessStatusCode: true,
                            ContentType: MediaTypeNames.Application.Json,
                            Body: JsonSerializer.Serialize(new { authorize_url = authorizeUrl }, _jsonOptionsUnsafe)
                        );
                    }
                    else
                    {
                        log.LogWarning("PAR response missing request_uri for app {app}", app.ApplicationID);
                    }
                }
                catch (Exception ex)
                {
                    log.LogWarning(ex, "Failed to parse PAR response for app {app}", app.ApplicationID);
                }
            }
            else
            {
                log.LogWarning("IdP discovery missing authorization_endpoint for app {app}", app.ApplicationID);
            }

        }
        else
        {
            log.LogWarning("PAR request failed for app {app}: {status} {body} {error}", app.ApplicationID, parResult.StatusCode, parResult.Body, parResult.Error);
        }

        return parResult;
    }

    public async Task<ResultWithStatus<OIDCClientCallbackResult, string>> HandleAuthorizationCallback(
            ApplicationOption app,
            string code,
            string redirectUri,
            string? codeVerifier,
            string state,
            string? authorizationResponseIssuer,
            CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(app.OIDCWellKnownURL))
            return new(null, "IdP discovery URL not configured for this client app");

        if (string.IsNullOrWhiteSpace(code) || string.IsNullOrWhiteSpace(redirectUri))
            return new(null, "Missing required parameters");

        // Validate state and consume cookie
        var (state_result, state_error) = state_and_nonce_service.ValidateAndConsumeStateCookie(app.ApplicationID, app.JWTKey, state);

        if (state_result == null || state_error != null)
        {
            return new(null, "invalid_state");
        }

        // Prefer CodeVerifier from state cookie
        var effectiveCodeVerifier = state_result.CodeVerifier ?? codeVerifier;
        if (string.IsNullOrWhiteSpace(effectiveCodeVerifier))
        {
            return new(null, "Missing code_verifier");
        }

        var (get_result, get_error) = await GetToken(
            app,
            (cert, wk) => PushedAuthorizationProvider.BuildTokenExchangeFormPrivateKeyJwt(
                cert,
                clientId: app.ApplicationID,
                wk.token_endpoint,
                code,
                redirectUri,
                effectiveCodeVerifier,
                wk.token_endpoint_auth_signing_alg_values_supported),
            authorizationResponseIssuer,
            ct
            );

        if (get_error != null || get_result == null || get_result.validate_result == null)
        {
            log.LogTrace("ID_TOKEN_VALIDATION_FAILED: {app_id}, Error: {get_error}", app.ApplicationID, get_error);
            return new(null, get_error ?? "Invalid id_token");
        }

        // Nonce verification against id_token
        var nonceFromIdToken = get_result.validate_result.Principal.FindFirst(WellknownIdentityConstants.Nonce)?.Value;
        if (!string.IsNullOrWhiteSpace(state_result.Nonce))
        {
            if (string.IsNullOrWhiteSpace(nonceFromIdToken) || nonceFromIdToken != state_result.Nonce)
            {
                return new(null, "invalid_nonce");
            }
        }

        return new(
            new(
                get_result.validate_result.Principal,
                get_result.validate_result.ExpiresUtc,
                get_result.idp_refresh_token,
                state_result.DeviceId,
                get_result.refresh_expiration,
                state_result.TargetLinkUri
                ),
            null);
    }

    public async Task<ResultWithStatus<string, string>> HandleInitiateLoginAsync
        (
        ApplicationOption app,
        OIDCInitiateLoginRequest request,
        HttpRequest httpRequest,
        CancellationToken ct
        )
    {
        if (string.IsNullOrWhiteSpace(request.Iss))
        {
            return new(null, "missing_iss");
        }

        // 1) Discover IdP metadata
        var (wk, wkErr) = await DiscoverWellKnown(app, ct);
        if (wkErr != null || wk == null || string.IsNullOrWhiteSpace(wk.issuer))
        {
            return new(null, wkErr ?? "discovery_failed");
        }

        // 2) Validate iss against IdP issuer; target_link_uri
        if (!string.Equals(wk.issuer, request.Iss, StringComparison.Ordinal))
        {
            log.LogWarning("INITIATE_LOGIN_ISS_MISMATCH app={app} expected_iss={expected} received_iss={received}",
                app.ApplicationID, wk.issuer, request.Iss);

            return new(null, "iss_mismatch");
        }

        var (normalizedTargetLinkUri, targetLinkError) = OIDCClientServiceProvider.ValidateTargetLinkURIAllowed(request.TargetLinkUri, app);
        if (targetLinkError != null)
        {
            log.LogWarning("Target link error: {targetLinkError} - {desc}", targetLinkError.Value.error, targetLinkError.Value.error_description);
            return new(null, targetLinkError.Value.error);
        }

        // 3) Build redirect_uri to client callback
        //    For TP initiated login we will use the server-side endpoint:
        //    GET/POST {app_id}/oidc-client/auth-callback
        var redirectUri = $"{httpRequest.Scheme}://{httpRequest.Host}{_api_path}{app.ApplicationID}/oidc-client/auth-callback";

        // 4) Build "form" for the PAR reusing the same logic as usual
        var dict = new Dictionary<string, StringValues>(StringComparer.Ordinal)
        {
            [WellknownIdentityConstants.ResponseType] = WellknownIdentityConstants.Code,
            [WellknownIdentityConstants.Scope] = WellknownIdentityConstants.OpenID,
            [WellknownIdentityConstants.RedirectUri] = redirectUri,
            [WellknownIdentityConstants.ClientId] = app.ApplicationID
        };

        if (!string.IsNullOrWhiteSpace(request.LoginHint))
        {
            dict[WellknownIdentityConstants.LoginHint] = request.LoginHint;
        }

        // target_link_uri as a local-only parameter so it ends up in the state cookie,
        // it will be stripped before sending the PAR request to the IdP.
        if (!string.IsNullOrWhiteSpace(normalizedTargetLinkUri))
        {
            dict[WellknownIdentityConstants.TargetLinkUri] = normalizedTargetLinkUri;
        }

        // LocalDeviceId is optional. We have no SPA here yet, so we don't send it.

        var form = new FormCollection(dict);

        // 5) Reuse HandleOidcClientPAR
        var parResult = await HandleOidcClientPAR(app, httpRequest.Headers, form, ct);

        if (!parResult.IsSuccessStatusCode)
        {
            log.LogWarning("INITIATE_LOGIN_PAR_FAILED app={app} status={status} error={error}",
                app.ApplicationID, parResult.StatusCode, parResult.Error ?? "");

            return new(null, $"par_failed:{parResult.StatusCode}");
        }

        // 6) Parse authorize_url from the PAR JSON response
        try
        {
            using var doc = JsonDocument.Parse(parResult.Body);
            var authorizeUrl = doc.RootElement.ReadString(WellknownIdentityConstants.AuthorizeUrl);
            if (string.IsNullOrWhiteSpace(authorizeUrl))
            {
                return new(null, "par_response_authorize_url_empty");
            }

            return new(authorizeUrl, null);
        }
        catch (Exception ex)
        {
            log.LogWarning(ex, "INITIATE_LOGIN_PARSE_ERROR app={app}", app.ApplicationID);
            return new(null, "par_response_invalid_json");
        }
    }


    public async Task<ResultWithStatus<OIDCFrontChannelLogoutInitiation, string>> BuildEndSessionRequest(ApplicationOption app, string idTokenHint, string? postLogoutRedirectUri, string? state, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(app.OIDCWellKnownURL))
        {
            return new(null, "IdP discovery URL not configured for this client app");
        }

        if (string.IsNullOrWhiteSpace(idTokenHint))
        {
            return new(null, "id_token_hint is required");
        }

        var (wk, err) = await DiscoverWellKnown(app, ct);
        if (err != null || wk == null || string.IsNullOrWhiteSpace(wk.end_session_endpoint))
        {
            return new(null, err ?? "IdP does not advertise end_session_endpoint");
        }

        // Generate/keep state (optional but recommended for CSRF)
        string effState = string.IsNullOrWhiteSpace(state) ? CryptClass.GenerateBase64UrlRandomCode(32) : state;

        // Build end_session URL
        var query = new Dictionary<string, string?>(StringComparer.Ordinal)
        {
            [WellknownIdentityConstants.IdTokenHint] = idTokenHint,
            [WellknownIdentityConstants.State] = effState
        };
        if (!string.IsNullOrWhiteSpace(postLogoutRedirectUri))
        {
            query["post_logout_redirect_uri"] = postLogoutRedirectUri;
        }

        string url = QueryHelpers.AddQueryString(wk.end_session_endpoint!, query!);

        return new(new OIDCFrontChannelLogoutInitiation(url, effState), null);
    }

    public async Task<ResultWithStatus<bool, string>> HandleFrontChannelLogout(ApplicationOption app, string? state, CancellationToken ct)
    {
        // For now, just acknowledge and let the SPA clear UI state.
        return await Task.FromResult(new ResultWithStatus<bool, string>(true, null));
    }

    public async Task<OIDCBackchannelLogoutResult> HandleBackchannelLogout(ApplicationOption app, string logoutTokenJwt, CancellationToken ct)
    {
        // 1) Discover IdP metadata (issuer + JWKS)
        var (wk, wkErr) = await DiscoverWellKnown(app, ct);
        if (wkErr != null || wk == null || string.IsNullOrWhiteSpace(wk.jwks_uri) || string.IsNullOrWhiteSpace(wk.issuer))
        {
            log.LogWarning("Backchannel logout: discovery failed for app {app}: {err}", app.ApplicationID, wkErr ?? "invalid metadata");
            return new(OIDCLogoutProcessingStatus.InvalidIssuer, wkErr ?? "discovery_failed");
        }

        // 2) Validate JWT signature, issuer, audience (lifetime ignored; will validate iat/exp manually)
        JsonWebToken? parsed = null;
        try
        {
            var jwks = await JwksProvider.FetchAndCacheRemoteJwksAsync(wk.jwks_uri, oidcHttpClient, remote_jwks_cache, ct);
            var handler = new JsonWebTokenHandler();
            var tvp = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidIssuer = wk.issuer,
                ValidateAudience = true,
                ValidAudience = app.ApplicationID,
                ValidateLifetime = false, // iat checked manually
                RequireSignedTokens = true,
                IssuerSigningKeys = jwks.Keys.Values
            };

            var validation = await handler.ValidateTokenAsync(logoutTokenJwt, tvp);
            if (!validation.IsValid || validation.SecurityToken is not JsonWebToken jwt)
            {
                var ex = validation.Exception;
                if (ex is SecurityTokenInvalidAudienceException) return new(OIDCLogoutProcessingStatus.InvalidAudience, "invalid_audience");
                if (ex is SecurityTokenInvalidIssuerException) return new(OIDCLogoutProcessingStatus.InvalidIssuer, "invalid_issuer");
                if (ex is SecurityTokenInvalidSignatureException) return new(OIDCLogoutProcessingStatus.InvalidSignature, "invalid_signature");
                return new(OIDCLogoutProcessingStatus.InvalidSignature, ex?.Message ?? "token_validation_failed");
            }
            parsed = jwt;
        }
        catch (Exception ex)
        {
            log.LogWarning(ex, "Backchannel logout: token validation error for app {app}", app.ApplicationID);
            return new(OIDCLogoutProcessingStatus.InvalidSignature, "token_validation_exception");
        }

        // 3) Extract required claims: jti, iat, events, sid/sub
        string jti = parsed.Id;
        if (jti.IsNullOrEmpty())
        {
            parsed.TryGetClaim(WellknownIdentityConstants.JWTID, out Claim jti_claim);
            jti = parsed.Id ?? jti_claim?.Value ?? "";
        }

        if (string.IsNullOrWhiteSpace(jti)) return new(OIDCLogoutProcessingStatus.MissingEvent, "missing_jti");

        long iatSecs = 0;
        if (!(parsed.TryGetPayloadValue<long>(WellknownIdentityConstants.IssuedAt, out iatSecs) ||
              long.TryParse(parsed?.Claims?.FirstOrDefault(c => c.Type == WellknownIdentityConstants.IssuedAt)?.Value, out iatSecs)))
        {
            return new(OIDCLogoutProcessingStatus.Expired, "missing_iat");
        }
        var iatUtc = DateTimeOffset.FromUnixTimeSeconds(iatSecs);

        // events must contain backchannel logout event URI
        bool hasEvent = false;
        if (parsed.TryGetPayloadValue<object>(WellknownIdentityConstants.Events, out var evRaw) && evRaw is not null)
        {
            var evStr = evRaw.ToString() ?? string.Empty;
            hasEvent = evStr.Contains(WellknownIdentityConstants.BackchannelLogoutEventUri);
        }
        else
        {
            var evClaim = parsed.Claims.FirstOrDefault(c => c.Type == WellknownIdentityConstants.Events)?.Value;
            hasEvent = !string.IsNullOrEmpty(evClaim) && evClaim.Contains(WellknownIdentityConstants.BackchannelLogoutEventUri);
        }
        if (!hasEvent) return new(OIDCLogoutProcessingStatus.MissingEvent, "missing_backchannel_event");

        string? sid = parsed.Claims.FirstOrDefault(c => c.Type == WellknownIdentityConstants.SessionIdentifier)?.Value;
        string? sub = parsed.Claims.FirstOrDefault(c => c.Type == WellknownIdentityConstants.SubjectIdentifier)?.Value;
        if (string.IsNullOrWhiteSpace(sid) && string.IsNullOrWhiteSpace(sub))
            return new(OIDCLogoutProcessingStatus.MissingSidOrSub, "missing_sid_and_sub");

        // 4) Replay check
        var replay = replay_cache.TryStore("logout", jti, iatUtc, TimeSpan.FromMinutes(10), TimeSpan.FromMinutes(2));
        if (replay.Status == ReplayCacheStatus.Replay)
        {
            log.LogInformation("Backchannel logout replay for app {app}, jti {jti}", app.ApplicationID, jti);
            return new(OIDCLogoutProcessingStatus.Replay, null);
        }
        if (replay.Status is ReplayCacheStatus.Stale or ReplayCacheStatus.Skew or ReplayCacheStatus.Invalid)
        {
            log.LogWarning("Backchannel logout rejected by replay cache for app {app}: {status} ({reason})", app.ApplicationID, replay.Status, replay.Reason);
            return new(OIDCLogoutProcessingStatus.Expired, replay.Reason);
        }

        // 5) Delete all SUB sessions
        using var dbc = app.CreateDatabaseClient(log, deviceid_service, null);
        try
        {
            await dbc.Connect(ct);

            if (sub.IsNullOrEmpty())
            {
                if (!sid.IsNullOrEmpty())
                {
                    // Determine sub in this app to purge
                    sub = await ApplicationOidcActiveSessions.GetSUBFromSID(dbc, app.ApplicationID, sid!, ct);
                    if (sub.IsNullOrEmpty())
                    {
                        log.LogWarning("Backchannel logout: no active session found for app {app} sid={sid}", app.ApplicationID, sid);
                        return new(OIDCLogoutProcessingStatus.Success, null);
                    }
                }
                else
                {
                    log.LogWarning("Backchannel logout: both sid and sub are missing for app {app}", app.ApplicationID);
                    return new(OIDCLogoutProcessingStatus.Success, null);
                }
            }

            await ApplicationOidcActiveSessions.DeleteSessionsBySUB(dbc, sub!, ct);

            return new(OIDCLogoutProcessingStatus.Success, null);
        }
        catch (Exception ex)
        {
            log.LogError(ex, "Backchannel logout: session store error app={app} sid={sid} sub={sub}", app.ApplicationID, sid, sub);
            return new(OIDCLogoutProcessingStatus.SessionStoreError, "session_store_error");
        }
        finally
        {
            if (dbc.ConnectionState == System.Data.ConnectionState.Open) await dbc.Disconnect();
        }

    }

    private sealed record GetTokenResult(
        string? id_token,
        string? idp_refresh_token,
        DateTimeOffset? refresh_expiration,
        OIDCTokenResponse? token_result,
        JWTTokenResult? validate_result
        );

    private async Task<ResultWithStatus<GetTokenResult?, string?>>
    GetToken(
       ApplicationOption app,
       Func<X509Certificate2, OIDCWellKnownResponse, Dictionary<string, string>> tokenFormBuilder,
       string? responseIssuer,
       CancellationToken ct
       )
    {

        // Discover IdP metadata
        var (wellknown, wellknown_error) = await DiscoverWellKnown(app, ct);
        if (wellknown_error != null || wellknown == null)
            return new(null, wellknown_error ?? "Discovery failed");

        var issuer = wellknown.issuer;
        var tokenEndpoint = wellknown.token_endpoint;
        var jwksUri = wellknown.jwks_uri;
        if (string.IsNullOrEmpty(issuer) || string.IsNullOrEmpty(tokenEndpoint) || string.IsNullOrEmpty(jwksUri))
            return new(null, "Invalid IdP discovery document");

        // Mix-up mitigation: validate authorization response issuer ('iss' param) if present
        if (!string.IsNullOrWhiteSpace(responseIssuer) &&
            !string.Equals(responseIssuer, issuer, StringComparison.Ordinal))
        {
            log.LogWarning("TOKEN_RESPONSE_ISS_MISMATCH app={app} expected={expected} received={received}", app.ApplicationID, issuer, responseIssuer);
            return new(null, "invalid_token_response_iss");
        }

        // Enforce token endpoint auth (prefer/require private_key_jwt)
        var allowed = wellknown.token_endpoint_auth_methods_supported;
        var allowPrivateKeyJwt = allowed == null || allowed.Count == 0 || allowed.Contains(OIDCTokenEndpointAuthMethod.private_key_jwt);
        if (!allowPrivateKeyJwt)
            return new(null, "IdP does not allow private_key_jwt for token endpoint");

        // Call IdP token endpoint
        string id_token;
        string? idpRefreshToken = null;
        DateTimeOffset? idpRefreshExpirationUtc = null;

        X509Certificate2? cert = certificate_cache.GetCertificate(app);
        if (cert == null)
            return new(null, "Client certificate not configured");

        var tokenForm = tokenFormBuilder(cert!, wellknown);

        var (token_result, token_error) = await OIDCClientServiceProvider.PostToTokenEndpoint(oidcHttpClient, tokenEndpoint, tokenForm, ct);
        if (!string.IsNullOrEmpty(token_error) || token_result == null)
        {
            return new(null, token_error ?? "IdP token failed");
        }

        id_token = token_result.id_token ?? "";
        if (string.IsNullOrEmpty(id_token))
        {
            return new(null, "IdP token response missing id_token");
        }

        idpRefreshToken = token_result.refresh_token;

        var rexp = token_result.refresh_expiration_utc;
        if (!string.IsNullOrWhiteSpace(rexp) && DateTimeOffset.TryParse(rexp, out var parsed))
        {
            idpRefreshExpirationUtc = parsed.ToUniversalTime();
        }

        log.LogTrace("TOKEN_RECEIVED app_id={appId} {meta} Refresh: {refresh} expiration: {exp}", app.ApplicationID, DescribeToken(id_token), DescribeToken(idpRefreshToken), idpRefreshExpirationUtc);

        // Validate id_token via IdP JWKS
        var (validate_result, validate_error) = await OIDCClientServiceProvider.ValidateToken(
            certificate_cache,
            app,
            wellknown,
            id_token,
            oidcHttpClient,
            remote_jwks_cache,
            ct);

        GetTokenResult result = new(
            id_token,
            idpRefreshToken,
            idpRefreshExpirationUtc,
            token_result,
            validate_result
            );

        if (validate_error != null || validate_result == null)
        {
            log.LogTrace("ID_TOKEN_VALIDATION_FAILED: {app_id}, id_token: {meta}. Error: {validate_error}", app.ApplicationID, DescribeToken(id_token), validate_error);
            return new(result, validate_error ?? "Invalid id_token");
        }

        return new(result, null);

    }

    private async Task<ResultWithStatus<OIDCClientCallbackResult, string>> RefreshBackchannelIdpTokens(
        ApplicationOption app,
        string idpRefreshToken,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(app.OIDCWellKnownURL))
            return new(null, "IdP discovery URL not configured for this client app");

        var (get_result, get_error) = await GetToken(
            app,
            (cert, wk) => PushedAuthorizationProvider.BuildRefreshTokenFormPrivateKeyJwt(
                cert,
                clientId: app.ApplicationID,
                wk.token_endpoint,
                idpRefreshToken,
                wk.token_endpoint_auth_signing_alg_values_supported),
            null,
            ct
            );

        if (get_error != null || get_result == null || get_result.validate_result == null)
        {
            log.LogTrace("ID_TOKEN_REFRESH_FAILED: {app_id}, Error: {get_error}", app.ApplicationID, get_error);
            return new(null, get_error ?? "Invalid id_token");
        }

        // Return principal and the (rotated) IdP refresh token info
        var newIdpRefreshToken = get_result.idp_refresh_token;
        var jwt_result = get_result.validate_result;
        var idpRefreshExpirationUtc = get_result.refresh_expiration;

        var effectiveRefreshToken = string.IsNullOrWhiteSpace(newIdpRefreshToken) ? idpRefreshToken : newIdpRefreshToken;

        return new(new(jwt_result.Principal, jwt_result.ExpiresUtc, effectiveRefreshToken, DeviceId: null, idpRefreshExpirationUtc, null), null);
    }

    public async Task<bool> RefreshIdpToken(
            ApplicationOption app,
            string sid,
            string device_id,
            CancellationToken ct)
    {

        string app_id = app.ApplicationID;
        using var dbc = app.CreateDatabaseClient(log, deviceid_service, null);

        try
        {
            await dbc.Connect(ct);

            // Load IdP refresh token for this sid (prefer current device row if present)
            var session = await ApplicationOidcActiveSessions.GetSessionBySID(dbc, app.ApplicationID, sid, ct, encryptor);

            if (session == null || string.IsNullOrWhiteSpace(session.vc_oidc_refreshtoken))
            {
                log.LogTrace("REFRESH_TOKEN: APP_ID {app_id} no IdP refresh token found for sid={sid}", app_id, sid);
                return false;
            }

            if (session.dt_refresh_expiration == null || session.dt_refresh_expiration <= DateTime.UtcNow)
            {
                log.LogTrace("REFRESH_TOKEN: APP_ID {app_id} IdP refresh token expired for sid={sid}", app_id, sid);
                return false;
            }

            // Call IdP to refresh using client credentials (private_key_jwt)
            var (idpResult, idpError) = await RefreshBackchannelIdpTokens(app, session.vc_oidc_refreshtoken, ct);
            if (idpError != null || idpResult == null || idpResult.Principal == null)
            {
                log.LogWarning("REFRESH_TOKEN: APP_ID {app_id} IdP refresh failed for sid={sid}: {err}", app_id, sid, idpError ?? "unknown");
                return false;
            }

            await ApplicationOidcActiveSessions.CreateOrUpdateExternalSignInSession(
                app_id,
                session.vc_username,
                session.c_user_id,
                device_id,
                sid,
                session.vc_oidc_sub,
                idpResult.IdpRefreshToken,
                idpResult.IdpRefreshExpirationUtc?.UtcDateTime,
                dbc,
                encryptor,
                ct
            );

            return true;
        }
        catch (Exception ex)
        {
            log.LogError(ex, "REFRESH_TOKEN: APP_ID {app_id} IdP fallback error for sid={sid}", app_id, sid);
            return false;
        }
        finally
        {
            if (dbc.ConnectionState == System.Data.ConnectionState.Open) await dbc.Disconnect();
        }
    }


}