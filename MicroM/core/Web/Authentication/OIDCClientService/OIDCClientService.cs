using MicroM.Configuration;
using MicroM.Core;
using MicroM.DataDictionary.Entities;
using MicroM.Extensions;
using MicroM.Web.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Headers;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using System.Collections.Concurrent;
using System.Security.Claims;
using System.Security.Cryptography.X509Certificates;
using System.Text.Json;

namespace MicroM.Web.Authentication.SSO;

public class OIDCClientService(
    IEtagCacheService etag_cache,
    IApplicationCertificateCacheService certificate_cache,
    IHttpClientFactory httpClientFactory,
    IStateAndNonceService state_and_nonce_service,
    IOIDCHttpClient oidcHttpClient,
    IOIDCReplayCacheService replay_cache,
    ILogger<OIDCClientService> log,
    IDeviceIdService deviceid_service,
    IMicroMEncryption encryptor
) : IOIDCClientService
{
    private readonly JsonSerializerOptions _jsonOptionsUnsafe = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull,
        Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };

    private readonly ConcurrentDictionary<string, OIDCWellKnownResponse> _wellKnownCache = new();

    public EtagCacheServiceCacheCheckResult? HandleClientJwks(ApplicationOption app, RequestHeaders request_headers, IHeaderDictionary response_headers)
    {
        // Build JWKS for this client app using its certificate (kid = CertificateUniqueID)
        if (app.OIDCCertificateBlob == null || app.OIDCCertificateBlob.Length == 0)
        {
            log.LogWarning("CLIENT JWKS requested for app {app} which has no certificate configured", app.ApplicationID);
            return null;
        }

        string key = $"{app.ApplicationID}_CLIENT_JWKS";
        var result = etag_cache.GetOrAddResponseWithCacheCheck(
            key,
            request_headers,
            response_headers,
            cache_duration_seconds: ConfigurationDefaults.JwksCacheDurationSeconds,
            () =>
        {
            X509Certificate2? cert = certificate_cache.GetCertificate(app);
            OIDCJwksKeyResponse? k = cert != null ? JwksProvider.GetRSAKey(app, cert) : null;
            var jwks = new OIDCJwksResponse(keys: k != null ? [k] : []);
            return JsonSerializer.Serialize(jwks, _jsonOptionsUnsafe);
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
            string key = $"{app.ApplicationID}_CLIENT_WK";

            if (_wellKnownCache.TryGetValue(key, out var cached) && cached != null)
            {
                return new(cached, null);
            }

            var wellknown_etag_content = await etag_cache.GetOrAddAsync(
                key,
                serveStaleOnError: true,
                ct: ct,
                valueFactory: async (ct) =>
                {
                    var result = await oidcHttpClient.GetWellKnownJsonAsync(app.OIDCWellKnownURL, ct);

                    if (!result.IsSuccessStatusCode)
                    {
                        throw new Exception(result.Error);
                    }

                    return result.Body;
                }
            );

            if (wellknown_etag_content == null)
            {
                return new(null, "Failed to parse IdP discovery document");
            }

            var wellknown_response = JsonSerializer.Deserialize<OIDCWellKnownResponse>(wellknown_etag_content.Content, _jsonOptionsUnsafe);

            if (wellknown_response != null) _wellKnownCache.GetOrAdd(key, wellknown_response);

            return new(wellknown_response, null);
        }
        catch (Exception ex)
        {
            return new(null, $"Failed to discover IdP configuration: {ex.Message}");
        }
    }

    public async Task<OIDCHttpClientPostResponse> HandleSignInOidc(ApplicationOption app, IHeaderDictionary requestHeaders, IFormCollection form, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(app.OIDCWellKnownURL))
        {
            log.LogWarning("OIDC SignIn requested for app {app} which has no IdP discovery URL configured", app.ApplicationID);
            return new(400, false, "application/json", JsonSerializer.Serialize(new { error = "invalid_request", error_description = "IdP discovery URL not configured for this client app" }));
        }

        // Discover PAR endpoint
        var (wellknown_result, wellknown_error) = await DiscoverWellKnown(app, ct);
        if (wellknown_error != null || wellknown_result == null)
        {
            log.LogWarning("Failed to discover IdP configuration for app {app}: {error}", app.ApplicationID, wellknown_error);
            return new(502, false, "application/json", JsonSerializer.Serialize(new { error = "server_error", error_description = "IdP discovery failed" }));
        }

        string? parEndpoint = wellknown_result.pushed_authorization_request_endpoint;
        if (string.IsNullOrEmpty(parEndpoint))
        {
            log.LogWarning("IdP for app {app} does not expose a pushed_authorization_request_endpoint", app.ApplicationID);
            return new(400, false, "application/json", JsonSerializer.Serialize(new { error = "unsupported_operation", error_description = "IdP does not expose a pushed_authorization_request_endpoint" }));
        }

        // State and Nonce validation (if present)
        string? providedState = form.TryGetValue(WellknownIdentityConstants.State, out var s) ? s.ToString() : null;
        string? providedNonce = form.TryGetValue(WellknownIdentityConstants.Nonce, out var n) ? n.ToString() : null;
        string? providedDeviceId = form.TryGetValue(WellknownIdentityConstants.LocalDeviceId, out var d) ? d.ToString() : null;

        var stateContext = state_and_nonce_service.EnsureStateAndNonce(form, providedState, providedNonce, providedDeviceId);

        // Prepare PAR body - forward incoming form params
        var form_result = PushedAuthorizationProvider.ValidateSignInForm(app, stateContext.AdjustedForm!);

        if (form_result.error != null || form_result.valid_form == null)
        {
            log.LogWarning("Invalid OIDC SignIn form for app {app}: {error}", app.ApplicationID, form_result.error);
            return new(400, false, "application/json", JsonSerializer.Serialize(form_result.error));
        }

        var forward = form_result.valid_form;
        // Remove local-only params
        forward.Remove(WellknownIdentityConstants.LocalDeviceId);

        X509Certificate2? cert = certificate_cache.GetCertificate(app);

        var (authHeader, authHeaderError) = PushedAuthorizationProvider.GetClientAuthorizationHeader(requestHeaders, cert, parEndpoint, forward, wellknown_result.token_endpoint_auth_methods_supported);

        if (authHeaderError != null)
        {
            log.LogWarning("Client authentication failed for app {app}: {error}", app.ApplicationID, authHeaderError);
            return new(400, false, "application/json", JsonSerializer.Serialize(new { error = "invalid_client", error_description = authHeaderError }));
        }

        // Build and send PAR request
        var parResult = await oidcHttpClient.PostPushedAuthorizationRequestAsync(parEndpoint, forward, authHeader, ct);

        // Store state and nonce associated with this client_id and request_uri
        if (parResult.IsSuccessStatusCode)
        {
            state_and_nonce_service.StoreStateCookie(app, app.JWTKey, stateContext.Data);
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
            string codeVerifier,
            string state,
            CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(app.OIDCWellKnownURL))
            return new(null, "IdP discovery URL not configured for this client app");

        if (string.IsNullOrWhiteSpace(code) || string.IsNullOrWhiteSpace(redirectUri) || string.IsNullOrWhiteSpace(codeVerifier))
            return new(null, "Missing required parameters");

        // Validate state and consume cookie
        var (state_result, state_error) = state_and_nonce_service.ValidateAndConsumeStateCookie(app.ApplicationID, app.JWTKey, state);

        if (state_result == null || state_error != null)
        {
            return new(null, "invalid_state");
        }

        // Discover IdP metadata
        var (wellknown, wellknown_error) = await DiscoverWellKnown(app, ct);
        if (wellknown_error != null || wellknown == null)
            return new(null, wellknown_error ?? "Discovery failed");

        var issuer = wellknown.issuer;
        var tokenEndpoint = wellknown.token_endpoint;
        var jwksUri = wellknown.jwks_uri;
        if (string.IsNullOrEmpty(issuer) || string.IsNullOrEmpty(tokenEndpoint) || string.IsNullOrEmpty(jwksUri))
            return new(null, "Invalid IdP discovery document");

        // Enforce token endpoint auth (prefer/require private_key_jwt)
        var allowed = wellknown.token_endpoint_auth_methods_supported;
        var allowPrivateKeyJwt = allowed == null || allowed.Count == 0 || allowed.Contains(OIDCTokenEndpointAuthMethod.private_key_jwt);
        if (!allowPrivateKeyJwt)
            return new(null, "IdP does not allow private_key_jwt for token endpoint");

        // Build token request with private_key_jwt
        X509Certificate2? cert = certificate_cache.GetCertificate(app);
        if (cert == null)
            return new(null, "Client certificate not configured");

        var clientId = app.ApplicationID;
        var tokenForm = PushedAuthorizationProvider.BuildTokenExchangeFormPrivateKeyJwt(
            cert,
            clientId,
            tokenEndpoint,
            code,
            redirectUri,
            codeVerifier
        );

        // Exchange authorization code for tokens
        string id_token;
        string? idpRefreshToken = null;
        DateTimeOffset? idpRefreshExpirationUtc = null;

        try
        {
            var tokenResult = await oidcHttpClient.PostTokenAsync(tokenEndpoint, tokenForm, authorization: null, ct);
            if (!tokenResult.IsSuccessStatusCode)
            {
                return new(null, $"Token exchange failed: {tokenResult.Body}. Error: {tokenResult.Error}");
            }

            using var doc = JsonDocument.Parse(tokenResult.Body);

            id_token = doc.RootElement.TryGetProperty(WellknownIdentityConstants.IdToken, out var idt) ? idt.GetString() ?? "" : "";
            if (string.IsNullOrEmpty(id_token))
            {
                return new(null, "Token response missing id_token");
            }

            if (doc.RootElement.TryGetProperty(WellknownIdentityConstants.RefreshToken, out var rt))
            {
                idpRefreshToken = rt.GetString();
            }

            if (doc.RootElement.TryGetProperty(WellknownIdentityConstants.RefreshExpirationUtc, out var rexp))
            {
                var rexpStr = rexp.GetString();
                if (!string.IsNullOrWhiteSpace(rexpStr) && DateTimeOffset.TryParse(rexpStr, out var parsed))
                {
                    idpRefreshExpirationUtc = parsed.ToUniversalTime();
                }
            }
        }
        catch (Exception ex)
        {
            return new(null, $"Token request error: {ex.Message}");
        }

        // Validate id_token via IdP JWKS
        var (jwt_result, jwt_error) = await JwksProvider.ValidateIdTokenAsync(httpClientFactory, jwksUri, issuer, clientId, id_token, ct);
        if (jwt_error != null || jwt_result == null)
        {
            return new(null, jwt_error ?? "Invalid id_token");
        }

        // Nonce verification against id_token
        var nonceFromIdToken = jwt_result.Principal.FindFirst("nonce")?.Value;
        if (!string.IsNullOrWhiteSpace(state_result.Nonce))
        {
            if (string.IsNullOrWhiteSpace(nonceFromIdToken) || !string.Equals(nonceFromIdToken, state_result.Nonce, StringComparison.Ordinal))
            {
                return new(null, "invalid_nonce");
            }
        }

        return new(new(jwt_result.Principal, jwt_result.ExpiresUtc, idpRefreshToken, state_result.DeviceId, idpRefreshExpirationUtc), null);
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
        string effState = string.IsNullOrWhiteSpace(state) ? PushedAuthorizationProvider.GenerateBase64UrlCode(16) : state;

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
            var jwks = await JwksProvider.FetchJwksAsync(httpClientFactory, wk.jwks_uri!, ct);
            var handler = new JsonWebTokenHandler();
            var tvp = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidIssuer = wk.issuer,
                ValidateAudience = true,
                ValidAudience = app.ApplicationID,
                ValidateLifetime = false, // iat checked manually
                RequireSignedTokens = true,
                IssuerSigningKeys = jwks.Keys
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
            hasEvent = evStr.Contains(WellknownIdentityConstants.BackchannelLogoutEventUri, StringComparison.Ordinal);
        }
        else
        {
            var evClaim = parsed.Claims.FirstOrDefault(c => c.Type == WellknownIdentityConstants.Events)?.Value;
            hasEvent = !string.IsNullOrEmpty(evClaim) && evClaim.Contains(WellknownIdentityConstants.BackchannelLogoutEventUri, StringComparison.Ordinal);
        }
        if (!hasEvent) return new(OIDCLogoutProcessingStatus.MissingEvent, "missing_backchannel_event");

        string? sid = parsed.Claims.FirstOrDefault(c => c.Type == WellknownIdentityConstants.SessionIdentifier)?.Value;
        string? sub = parsed.Claims.FirstOrDefault(c => c.Type == WellknownIdentityConstants.SubjectIdentifier)?.Value;
        if (string.IsNullOrWhiteSpace(sid) && string.IsNullOrWhiteSpace(sub))
            return new(OIDCLogoutProcessingStatus.MissingSidOrSub, "missing_sid_and_sub");

        // 4) Replay check
        var replay = replay_cache.TryStore(jti, iatUtc);
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
                        log.LogInformation("Backchannel logout: no active session found for app {app} sid={sid}", app.ApplicationID, sid);
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

    private async Task<ResultWithStatus<OIDCClientCallbackResult, string>> RefreshBackchannelIdpTokens(
        ApplicationOption app,
        string idpRefreshToken,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(app.OIDCWellKnownURL))
        {
            return new(null, "IdP discovery URL not configured for this client app");
        }

        // Discover IdP metadata
        var (wellknown, wellknown_error) = await DiscoverWellKnown(app, ct);
        if (wellknown_error != null || wellknown == null)
        {
            return new(null, wellknown_error ?? "Discovery failed");
        }

        var issuer = wellknown.issuer;
        var tokenEndpoint = wellknown.token_endpoint;
        var jwksUri = wellknown.jwks_uri;
        if (string.IsNullOrEmpty(issuer) || string.IsNullOrEmpty(tokenEndpoint) || string.IsNullOrEmpty(jwksUri))
        {
            return new(null, "Invalid IdP discovery document");
        }

        // Enforce token endpoint auth (prefer/require private_key_jwt)
        var allowed = wellknown.token_endpoint_auth_methods_supported;
        var allowPrivateKeyJwt = allowed == null || allowed.Count == 0 || allowed.Contains(OIDCTokenEndpointAuthMethod.private_key_jwt);
        if (!allowPrivateKeyJwt)
        {
            return new(null, "IdP does not allow private_key_jwt for token endpoint");
        }

        // Build refresh token request with private_key_jwt
        X509Certificate2? cert = certificate_cache.GetCertificate(app);
        if (cert == null)
        {
            return new(null, "Client certificate not configured");
        }

        var refreshForm = PushedAuthorizationProvider.BuildRefreshTokenFormPrivateKeyJwt(
            cert,
            clientId: app.ApplicationID,
            tokenEndpoint,
            idpRefreshToken
        );

        // Call IdP token endpoint
        string id_token;
        string? newIdpRefreshToken = null;
        DateTimeOffset? idpRefreshExpirationUtc = null;

        try
        {
            var refreshResult = await oidcHttpClient.PostTokenAsync(tokenEndpoint, refreshForm, authorization: null, ct);
            if (!refreshResult.IsSuccessStatusCode)
            {
                return new(null, $"IdP refresh failed: {refreshResult.Body}. Error: {refreshResult.Error}");
            }

            using var doc = JsonDocument.Parse(refreshResult.Body);
            id_token = doc.RootElement.TryGetProperty(WellknownIdentityConstants.IdToken, out var idt) ? idt.GetString() ?? "" : "";
            if (string.IsNullOrEmpty(id_token))
            {
                return new(null, "IdP refresh response missing id_token");
            }

            if (doc.RootElement.TryGetProperty(WellknownIdentityConstants.RefreshToken, out var rt))
            {
                newIdpRefreshToken = rt.GetString();
            }

            if (doc.RootElement.TryGetProperty(WellknownIdentityConstants.RefreshExpirationUtc, out var rexp))
            {
                var rexpStr = rexp.GetString();
                if (!string.IsNullOrWhiteSpace(rexpStr) && DateTimeOffset.TryParse(rexpStr, out var parsed))
                {
                    idpRefreshExpirationUtc = parsed.ToUniversalTime();
                }
            }
        }
        catch (Exception ex)
        {
            return new(null, $"IdP refresh request error: {ex.Message}");
        }

        // Validate refreshed id_token
        var (jwt_result, jwt_error) = await JwksProvider.ValidateIdTokenAsync(httpClientFactory, jwksUri, issuer, app.ApplicationID, id_token, ct);
        if (jwt_error != null || jwt_result == null)
        {
            return new(null, jwt_error ?? "Invalid id_token");
        }

        // Return principal and the (rotated) IdP refresh token info
        var effectiveRefreshToken = string.IsNullOrWhiteSpace(newIdpRefreshToken) ? idpRefreshToken : newIdpRefreshToken;

        return new(new(jwt_result.Principal, jwt_result.ExpiresUtc, effectiveRefreshToken, DeviceId: null, idpRefreshExpirationUtc), null);


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