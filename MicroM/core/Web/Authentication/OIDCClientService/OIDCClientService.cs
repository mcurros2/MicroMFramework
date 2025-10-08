using MicroM.Configuration;
using MicroM.Core;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Headers;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;
using System.Security.Cryptography.X509Certificates;
using System.Text.Json;

namespace MicroM.Web.Authentication.SSO;

public class OIDCClientService(
    IEtagCacheService etag_cache,
    IApplicationCertificateCacheService certificate_cache,
    IHttpClientFactory httpClientFactory,
    IStateAndNonceService state_and_nonce_service,
    IOIDCHttpClient oidcHttpClient,
    ILogger<OIDCClientService> log
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
                    var (wellknown, error) = await oidcHttpClient.GetWellKnownJsonAsync(app.OIDCWellKnownURL, ct);

                    if (error != null || wellknown == null)
                    {
                        throw new Exception(error);
                    }

                    return wellknown;
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
            return new(400, "application/json", JsonSerializer.Serialize(new { error = "invalid_request", error_description = "IdP discovery URL not configured for this client app" }));
        }

        // Discover PAR endpoint
        var (wellknown_result, wellknown_error) = await DiscoverWellKnown(app, ct);
        if (wellknown_error != null || wellknown_result == null)
        {
            log.LogWarning("Failed to discover IdP configuration for app {app}: {error}", app.ApplicationID, wellknown_error);
            return new(502, "application/json", JsonSerializer.Serialize(new { error = "server_error", error_description = "IdP discovery failed" }));
        }

        string? parEndpoint = wellknown_result.pushed_authorization_request_endpoint;
        if (string.IsNullOrEmpty(parEndpoint))
        {
            log.LogWarning("IdP for app {app} does not expose a pushed_authorization_request_endpoint", app.ApplicationID);
            return new(400, "application/json", JsonSerializer.Serialize(new { error = "unsupported_operation", error_description = "IdP does not expose a pushed_authorization_request_endpoint" }));
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
            return new(400, "application/json", JsonSerializer.Serialize(form_result.error));
        }

        var forward = form_result.valid_form;
        // Remove local-only params
        forward.Remove(WellknownIdentityConstants.LocalDeviceId);

        var clientId = forward[WellknownIdentityConstants.ClientId];

        X509Certificate2? cert = certificate_cache.GetCertificate(app);

        var (authHeader, authHeaderError) = PushedAuthorizationProvider.GetClientAuthorizationHeader(requestHeaders, cert, parEndpoint, forward, wellknown_result.token_endpoint_auth_methods_supported);

        if (authHeaderError != null)
        {
            log.LogWarning("Client authentication failed for app {app}: {error}", app.ApplicationID, authHeaderError);
            return new(400, "application/json", JsonSerializer.Serialize(new { error = "invalid_client", error_description = authHeaderError }));
        }

        // Build and send PAR request
        var parResult = await oidcHttpClient.PostPushedAuthorizationRequestAsync(parEndpoint, forward, authHeader, ct);

        // Store state and nonce associated with this client_id and request_uri
        if (parResult.StatusCode is >= 200 and < 300)
        {
            state_and_nonce_service.StoreStateCookie(app, app.JWTKey, stateContext.Data);
        }
        else
        {
            log.LogWarning("PAR request failed for app {app}: {status} {body}", app.ApplicationID, parResult.StatusCode, parResult.Body);
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
            if (tokenResult.StatusCode < 200 || tokenResult.StatusCode >= 300)
            {
                return new(null, $"Token exchange failed: {tokenResult.Body}");
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

            if (doc.RootElement.TryGetProperty("refresh_expiration_utc", out var rexp))
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

    public Task<ResultWithStatus<OIDCFrontChannelLogoutInitiation, string>> BuildEndSessionRequest(ApplicationOption app, string idTokenHint, string? postLogoutRedirectUri, string? state, CancellationToken ct)
    {
        throw new NotImplementedException();
    }

    public Task<ResultWithStatus<bool, string>> HandleFrontChannelLogout(ApplicationOption app, string? state, CancellationToken ct)
    {
        throw new NotImplementedException();
    }

    public Task<OIDCBackchannelLogoutResult> HandleBackchannelLogout(ApplicationOption app, string logoutTokenJwt, CancellationToken ct)
    {
        throw new NotImplementedException();
    }
}