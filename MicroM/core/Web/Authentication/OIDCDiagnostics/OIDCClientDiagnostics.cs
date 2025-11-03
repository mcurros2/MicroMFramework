using MicroM.Configuration;
using MicroM.Data;
using MicroM.DataDictionary.CategoriesDefinitions;
using MicroM.Extensions;
using MicroM.Web.Services;
using System.Net.Http.Headers;
using System.Text.Json;

namespace MicroM.Web.Authentication.SSO;

/// <summary>
/// Provides methods to test and diagnose OpenID Connect (OIDC) IDPClient role configuration.
/// </summary>
/// <remarks>
/// Validates the configured IdP for the given app_id by reading its discovery document and testing relevant IdP endpoints.
/// Does not assume co-location of client and IdP; all network calls go through IOIDCHttpClient. No client-initiated SLO here.
/// </remarks>
public class OIDCClientDiagnostics : IOIDCClientDiagnostics
{
    private static OIDCDiagnosticsResult? ValidateApp(string url_name, ApplicationOption? app)
    {
        if (app == null)
            return new(OIDCDiagnosticsTestType.WellKnownAndJWKS, null, [new("app_not_found", "Application configuration not found")]);
        if (app.IdentityProviderRoleType != nameof(IdentityProviderRole.IDPClient))
            return new(OIDCDiagnosticsTestType.WellKnownAndJWKS, null, [new("invalid_role", "Application is not configured as IDPClient")]);
        if (string.IsNullOrWhiteSpace(app.OIDCWellKnownURL))
            return new(OIDCDiagnosticsTestType.WellKnownAndJWKS, null, [new("url_missing", $"OIDC {url_name} URL is not configured")]);

        return null;
    }

    public async Task<List<OIDCDiagnosticsResult>> TestAllAsync(
        IEntityClient ec,
        string appId,
        IMicroMAppConfiguration appConfig,
        IIdentityProviderService idpService,
        IOIDCClientService clientService,
        IOIDCHttpClient httpClient,
        CancellationToken ct)
    {
        var results = new List<OIDCDiagnosticsResult>
        {
            await TestWellKnownAndJWKSAsync(appConfig, httpClient, appId, ct),
            await TestPARAsync(ec, appConfig, httpClient, appId, ct),
            await TestAuthorizeUrlBuildAndRedirectUriAsync(appConfig, clientService, httpClient, appId, ct),
            await TestIdpRefreshAsync(ec, appConfig, clientService, httpClient, appId, ct),
            await TestEndSessionEndpointAsync(appConfig, httpClient, appId, ct),
            await TestUserInfoEndpointAsync(appConfig, httpClient, appId, ct),
            await TestRevocationEndpointAsync(appConfig, httpClient, appId, ct),
            await TestIntrospectionEndpointAsync(appConfig, httpClient, appId, ct)
        };

        return results;
    }

    public async Task<OIDCDiagnosticsResult> TestAuthorizeUrlBuildAndRedirectUriAsync(
        IMicroMAppConfiguration appConfig,
        IOIDCClientService clientService,
        IOIDCHttpClient httpClient,
        string appId,
        CancellationToken ct)
    {
        var app = appConfig.GetAppConfiguration(appId);

        var validate_result = ValidateApp("well-known", app);
        if (validate_result != null || app == null)
            return validate_result!;

        try
        {
            var wk = await httpClient.GetWellKnownJsonAsync(app.OIDCWellKnownURL!, ct);
            if (!wk.IsSuccessStatusCode || string.IsNullOrWhiteSpace(wk.Body))
                return new(OIDCDiagnosticsTestType.WellKnownAndJWKS, null, [new("wellknown_http_error", wk.Error ?? "Failed to fetch discovery")]);

            using var doc = JsonDocument.Parse(wk.Body);
            var root = doc.RootElement;

            // authorization_endpoint (required)
            if (!root.TryGetProperty("authorization_endpoint", out var authProp) || authProp.ValueKind != JsonValueKind.String || string.IsNullOrWhiteSpace(authProp.GetString()))
                return new(OIDCDiagnosticsTestType.WellKnownAndJWKS, wk.Body, [new("authorize_missing", "Discovery is missing authorization_endpoint")]);

            // response_types_supported must include "code"
            bool supportsCode = true;
            if (root.TryGetProperty("response_types_supported", out var rts) && rts.ValueKind == JsonValueKind.Array)
                supportsCode = rts.EnumerateArray().Any(x => x.ValueKind == JsonValueKind.String && string.Equals(x.GetString(), WellknownIdentityConstants.Code, StringComparison.OrdinalIgnoreCase));
            if (!supportsCode)
                return new(OIDCDiagnosticsTestType.WellKnownAndJWKS, $"authorization_endpoint: {authProp.GetString()}", [new("authorize_code_missing", "IdP does not advertise response_types_supported=code")]);

            // PKCE S256 support is recommended
            bool supportsS256 = true;
            if (root.TryGetProperty("code_challenge_methods_supported", out var ccms) && ccms.ValueKind == JsonValueKind.Array)
                supportsS256 = ccms.EnumerateArray().Any(x => x.ValueKind == JsonValueKind.String && string.Equals(x.GetString(), "S256", StringComparison.OrdinalIgnoreCase));
            if (!supportsS256)
                return new(OIDCDiagnosticsTestType.WellKnownAndJWKS, $"authorization_endpoint: {authProp.GetString()}", [new("pkce_s256_missing", "IdP does not advertise PKCE S256 support")]);

            // If scopes_supported present, ensure openid included
            bool scopesOk = true;
            if (root.TryGetProperty("scopes_supported", out var scopes) && scopes.ValueKind == JsonValueKind.Array)
                scopesOk = scopes.EnumerateArray().Any(x => x.ValueKind == JsonValueKind.String && string.Equals(x.GetString(), WellknownIdentityConstants.OpenID, StringComparison.OrdinalIgnoreCase));

            var summary = $"authorization_endpoint: {authProp.GetString()}\nPKCE_S256: {supportsS256}\nScopesHasOpenId: {scopesOk}";
            return new(OIDCDiagnosticsTestType.WellKnownAndJWKS, summary, null);
        }
        catch (OperationCanceledException)
        {
            return new(OIDCDiagnosticsTestType.WellKnownAndJWKS, null, [new("cancelled", "Operation canceled")]);
        }
        catch (JsonException ex)
        {
            return new(OIDCDiagnosticsTestType.WellKnownAndJWKS, null, [new("json_error", ex.Message)]);
        }
        catch (Exception ex)
        {
            return new(OIDCDiagnosticsTestType.WellKnownAndJWKS, null, [new("unexpected_error", ex.Message)]);
        }
    }

    public Task<OIDCDiagnosticsResult> TestBackchannelReceiverAsync(
        IEntityClient ec,
        IMicroMAppConfiguration appConfig,
        IIdentityProviderService idpService,
        IOIDCClientService clientService,
        string appId,
        CancellationToken ct)
        => Task.FromResult(new OIDCDiagnosticsResult(OIDCDiagnosticsTestType.BackchannelReceiver, "Backchannel receiver diagnostic is pending", [new("not_implemented", "Pending implementation")]));

    public Task<OIDCDiagnosticsResult> TestCallbackEndpointAsync(
        IMicroMAppConfiguration appConfig,
        IOIDCHttpClient httpClient,
        string appId,
        CancellationToken ct)
        => Task.FromResult(new OIDCDiagnosticsResult(OIDCDiagnosticsTestType.Unknown, "Callback endpoint diagnostic is pending", [new("not_implemented", "Pending implementation")]));

    public async Task<OIDCDiagnosticsResult> TestIdpRefreshAsync(
        IEntityClient ec,
        IMicroMAppConfiguration appConfig,
        IOIDCClientService clientService,
        IOIDCHttpClient httpClient,
        string appId,
        CancellationToken ct)
    {
        var app = appConfig.GetAppConfiguration(appId);

        var validate_result = ValidateApp("well-known", app);
        if (validate_result != null || app == null)
            return validate_result!;


        string? wellKnownJson = null;
        string? tokenEndpoint = null;

        try
        {
            var wk = await httpClient.GetWellKnownJsonAsync(app.OIDCWellKnownURL!, ct);
            if (!wk.IsSuccessStatusCode || string.IsNullOrWhiteSpace(wk.Body))
                return new(OIDCDiagnosticsTestType.TokenEndpoint, null, [new("wellknown_http_error", wk.Error ?? "Failed to fetch discovery")]);

            wellKnownJson = wk.Body;
            using var doc = JsonDocument.Parse(wellKnownJson);
            var root = doc.RootElement;

            if (!root.TryGetProperty("token_endpoint", out var tokenProp) || tokenProp.ValueKind != JsonValueKind.String || string.IsNullOrWhiteSpace(tokenProp.GetString()))
                return new(OIDCDiagnosticsTestType.TokenEndpoint, wellKnownJson, [new("token_endpoint_missing", "Discovery is missing token_endpoint")]);

            tokenEndpoint = tokenProp.GetString()!;

            var form = new Dictionary<string, string>
            {
                [WellknownIdentityConstants.GrantType] = WellknownIdentityConstants.RefreshToken,
                [WellknownIdentityConstants.RefreshToken] = "invalid_refresh_token",
                [WellknownIdentityConstants.ClientId] = app.ApplicationID
            };

            var resp = await httpClient.PostTokenAsync(tokenEndpoint, form, authorization: null, ct);
            var status = resp.StatusCode;
            var body = resp.Body ?? string.Empty;

            if (resp.IsSuccessStatusCode)
            {
                return new(OIDCDiagnosticsTestType.TokenEndpoint,
                    $"Token endpoint responded without error for invalid refresh_token.\nEndpoint: {tokenEndpoint}\nHTTP {status}\nBody: {body.Truncate(2048)}",
                    [new("token_unexpected_success", "Expected an error for invalid refresh_token but received success")]);
            }

            if ((int)status is 400 or 401 or 403)
            {
                return new(OIDCDiagnosticsTestType.TokenEndpoint,
                    $"Status: OK (expected error semantics)\nEndpoint: {tokenEndpoint}\nHTTP {status}\nBody: {body.Truncate(2048)}\nError: {resp.Error}",
                    null);
            }

            return new(OIDCDiagnosticsTestType.TokenEndpoint,
                $"Endpoint: {tokenEndpoint}\nHTTP {status}\nBody: {body.Truncate(2048)}\nError: {resp.Error}",
                [new("token_unexpected_status", "Token endpoint returned an unexpected status code")]);
        }
        catch (OperationCanceledException)
        {
            return new(OIDCDiagnosticsTestType.TokenEndpoint, $"Well known: {wellKnownJson}\nToken: {tokenEndpoint}", [new("cancelled", "Operation canceled")]);
        }
        catch (JsonException ex)
        {
            return new(OIDCDiagnosticsTestType.TokenEndpoint, $"Well known: {wellKnownJson}", [new("json_error", ex.Message)]);
        }
        catch (Exception ex)
        {
            return new(OIDCDiagnosticsTestType.TokenEndpoint, $"Well known: {wellKnownJson}\nToken: {tokenEndpoint}", [new("unexpected_error", ex.Message)]);
        }
    }

    public Task<OIDCDiagnosticsResult> TestRefreshFallbackAsync(
        IEntityClient ec,
        IMicroMAppConfiguration appConfig,
        IOIDCClientService clientService,
        string appId,
        CancellationToken ct)
        => Task.FromResult(new OIDCDiagnosticsResult(OIDCDiagnosticsTestType.Unknown, "Refresh fallback diagnostic is pending", [new("not_implemented", "Pending implementation")]));

    public async Task<OIDCDiagnosticsResult> TestWellKnownAndJWKSAsync(IMicroMAppConfiguration appConfig, IOIDCHttpClient httpClient, string appId, CancellationToken ct)
    {
        var app = appConfig.GetAppConfiguration(appId);

        var validate_result = ValidateApp("well-known", app);
        if (validate_result != null || app == null)
            return validate_result!;

        string? wellKnownJson = null;
        string? jwksJson = null;
        try
        {
            var wellKnownResponse = await httpClient.GetWellKnownJsonAsync(app.OIDCWellKnownURL!, ct);
            if (!wellKnownResponse.IsSuccessStatusCode || string.IsNullOrWhiteSpace(wellKnownResponse.Body))
                return new(OIDCDiagnosticsTestType.WellKnownAndJWKS, null, [new("wellknown_http_error", wellKnownResponse.Error ?? "Failed to fetch discovery")]);

            wellKnownJson = wellKnownResponse.Body;
            using var doc = JsonDocument.Parse(wellKnownJson);
            var root = doc.RootElement;

            var hasIssuer = root.TryGetProperty("issuer", out var issuerProp) && issuerProp.ValueKind == JsonValueKind.String && !string.IsNullOrWhiteSpace(issuerProp.GetString());
            var hasJwksUri = root.TryGetProperty("jwks_uri", out var jwksProp) && jwksProp.ValueKind == JsonValueKind.String && !string.IsNullOrWhiteSpace(jwksProp.GetString());
            if (!hasIssuer || !hasJwksUri)
                return new(OIDCDiagnosticsTestType.WellKnownAndJWKS, wellKnownJson, [new("wellknown_invalid", "Discovery document missing required fields: issuer and/or jwks_uri")]);

            var jwksUri = jwksProp.GetString()!;
            var jwksResponse = await httpClient.GetJwksJsonAsync(jwksUri, ct);
            if (!jwksResponse.IsSuccessStatusCode || string.IsNullOrWhiteSpace(jwksResponse.Body))
                return new(OIDCDiagnosticsTestType.WellKnownAndJWKS, wellKnownJson, [new("jwks_http_error", jwksResponse.Error ?? "Failed to fetch JWKS")]);

            jwksJson = jwksResponse.Body;
            using var jwksDoc = JsonDocument.Parse(jwksJson);
            if (!jwksDoc.RootElement.TryGetProperty("keys", out var keysEl) || keysEl.ValueKind != JsonValueKind.Array || keysEl.GetArrayLength() <= 0)
                return new(OIDCDiagnosticsTestType.WellKnownAndJWKS, $"Well known: {wellKnownJson}\n\n JWKS: {jwksJson}", [new("jwks_empty", "JWKS contains no keys")]);

            return new(OIDCDiagnosticsTestType.WellKnownAndJWKS, $"Status: OK\n\nWell known: {wellKnownJson}\n\n JWKS: {jwksJson}", null);
        }
        catch (OperationCanceledException)
        {
            return new(OIDCDiagnosticsTestType.WellKnownAndJWKS, $"Well known: {wellKnownJson}\n\n JWKS: {jwksJson}", [new("cancelled", "Operation canceled")]);
        }
        catch (JsonException ex)
        {
            return new(OIDCDiagnosticsTestType.WellKnownAndJWKS, $"Well known: {wellKnownJson}\n\n JWKS: {jwksJson}", [new("json_error", ex.Message)]);
        }
        catch (Exception ex)
        {
            return new(OIDCDiagnosticsTestType.WellKnownAndJWKS, $"Well known: {wellKnownJson}\n\n JWKS: {jwksJson}", [new("unexpected_error", ex.Message)]);
        }
    }

    public async Task<OIDCDiagnosticsResult> TestPARAsync(IEntityClient ec, IMicroMAppConfiguration appConfig, IOIDCHttpClient httpClient, string appId, CancellationToken ct)
    {
        var app = appConfig.GetAppConfiguration(appId);

        var validate_result = ValidateApp("well-known", app);
        if (validate_result != null || app == null)
            return validate_result!;

        string? wellKnownJson = null;
        string? parEndpoint = null;

        try
        {
            var wk = await httpClient.GetWellKnownJsonAsync(app.OIDCWellKnownURL!, ct);
            if (!wk.IsSuccessStatusCode || string.IsNullOrWhiteSpace(wk.Body))
                return new(OIDCDiagnosticsTestType.PAR, null, [new("wellknown_http_error", wk.Error ?? "Failed to fetch discovery")]);

            wellKnownJson = wk.Body;
            using var doc = JsonDocument.Parse(wellKnownJson);
            var root = doc.RootElement;

            if (!root.TryGetProperty("pushed_authorization_request_endpoint", out var parProp) || parProp.ValueKind != JsonValueKind.String || string.IsNullOrWhiteSpace(parProp.GetString()))
                return new(OIDCDiagnosticsTestType.PAR, "PAR endpoint not advertised in discovery; skipping probe.", null);

            parEndpoint = parProp.GetString()!;

            var form = new Dictionary<string, string>
            {
                [WellknownIdentityConstants.ResponseType] = WellknownIdentityConstants.Code,
                [WellknownIdentityConstants.ClientId] = app.ApplicationID,
                [WellknownIdentityConstants.Scope] = WellknownIdentityConstants.OpenID
            };

            var parRes = await httpClient.PostPushedAuthorizationRequestAsync(parEndpoint, form, authorization: (AuthenticationHeaderValue?)null, ct);
            var statusCode = (int)parRes.StatusCode;
            var body = parRes.Body ?? string.Empty;

            if (parRes.IsSuccessStatusCode)
                return new(OIDCDiagnosticsTestType.PAR, $"Status: OK (PAR reachable)\nEndpoint: {parEndpoint}\nResponse: {body.Truncate(2048)}", null);

            if (statusCode is 400 or 401 or 403)
                return new(OIDCDiagnosticsTestType.PAR, $"Status: OK (expected error semantics)\nEndpoint: {parEndpoint}\nHTTP {statusCode}\nBody: {body.Truncate(2048)}\nError: {parRes.Error}", null);

            return new(OIDCDiagnosticsTestType.PAR, $"Endpoint: {parEndpoint}\nHTTP {statusCode}\nBody: {body.Truncate(2048)}\nError: {parRes.Error}", [new("par_unexpected_status", "PAR endpoint returned an unexpected status code")]);
        }
        catch (OperationCanceledException)
        {
            return new(OIDCDiagnosticsTestType.PAR, $"Well known: {wellKnownJson}\nPAR: {parEndpoint}", [new("cancelled", "Operation canceled")]);
        }
        catch (JsonException ex)
        {
            return new(OIDCDiagnosticsTestType.PAR, $"Well known: {wellKnownJson}", [new("json_error", ex.Message)]);
        }
        catch (Exception ex)
        {
            return new(OIDCDiagnosticsTestType.PAR, $"Well known: {wellKnownJson}\nPAR: {parEndpoint}", [new("unexpected_error", ex.Message)]);
        }
    }

    public async Task<OIDCDiagnosticsResult> TestEndSessionEndpointAsync(IMicroMAppConfiguration appConfig, IOIDCHttpClient httpClient, string appId, CancellationToken ct)
    {
        var app = appConfig.GetAppConfiguration(appId);

        var validate_result = ValidateApp("well-known", app);
        if (validate_result != null || app == null)
            return validate_result!;

        string? wellKnownJson = null;
        string? endSessionUrl = null;

        try
        {
            var wk = await httpClient.GetWellKnownJsonAsync(app.OIDCWellKnownURL!, ct);
            if (!wk.IsSuccessStatusCode || string.IsNullOrWhiteSpace(wk.Body))
                return new(OIDCDiagnosticsTestType.EndSessionEndpoint, null, [new("wellknown_http_error", wk.Error ?? "Failed to fetch discovery")]);

            wellKnownJson = wk.Body;
            using var doc = JsonDocument.Parse(wellKnownJson);
            var root = doc.RootElement;

            if (!root.TryGetProperty("end_session_endpoint", out var esProp) || esProp.ValueKind != JsonValueKind.String || string.IsNullOrWhiteSpace(esProp.GetString()))
                return new(OIDCDiagnosticsTestType.EndSessionEndpoint, "end_session_endpoint not advertised in discovery; skipping probe.", null);

            endSessionUrl = esProp.GetString()!;

            // Probe via POST with no params; many IdPs will return 400/401/403 or 405 (method not allowed)
            var resp = await httpClient.PostFormUrlEncodedAsync(endSessionUrl, Array.Empty<KeyValuePair<string, string>>(), ct);
            var status = resp.StatusCode;
            var body = resp.Body ?? string.Empty;

            if (resp.IsSuccessStatusCode || (int)status is 302 or 400 or 401 or 403 or 405)
                return new(OIDCDiagnosticsTestType.EndSessionEndpoint, $"Status: OK (endpoint reachable)\nEndpoint: {endSessionUrl}\nHTTP {status}\nBody: {body.Truncate(2048)}\nError: {resp.Error}", null);

            return new(OIDCDiagnosticsTestType.EndSessionEndpoint, $"Endpoint: {endSessionUrl}\nHTTP {status}\nBody: {body.Truncate(2048)}\nError: {resp.Error}", [new("end_session_unexpected_status", "End session endpoint returned an unexpected status code")]);
        }
        catch (OperationCanceledException)
        {
            return new(OIDCDiagnosticsTestType.EndSessionEndpoint, $"Well known: {wellKnownJson}\nEndSession: {endSessionUrl}", [new("cancelled", "Operation canceled")]);
        }
        catch (JsonException ex)
        {
            return new(OIDCDiagnosticsTestType.EndSessionEndpoint, $"Well known: {wellKnownJson}", [new("json_error", ex.Message)]);
        }
        catch (Exception ex)
        {
            return new(OIDCDiagnosticsTestType.EndSessionEndpoint, $"Well known: {wellKnownJson}\nEndSession: {endSessionUrl}", [new("unexpected_error", ex.Message)]);
        }
    }

    public async Task<OIDCDiagnosticsResult> TestUserInfoEndpointAsync(IMicroMAppConfiguration appConfig, IOIDCHttpClient httpClient, string appId, CancellationToken ct)
    {
        var app = appConfig.GetAppConfiguration(appId);

        var validate_result = ValidateApp("well-known", app);
        if (validate_result != null || app == null)
            return validate_result!;

        string? wellKnownJson = null;
        string? userInfoUrl = null;

        try
        {
            var wk = await httpClient.GetWellKnownJsonAsync(app.OIDCWellKnownURL!, ct);
            if (!wk.IsSuccessStatusCode || string.IsNullOrWhiteSpace(wk.Body))
                return new(OIDCDiagnosticsTestType.UserInfoEndpoint, null, [new("wellknown_http_error", wk.Error ?? "Failed to fetch discovery")]);

            wellKnownJson = wk.Body;
            using var doc = JsonDocument.Parse(wellKnownJson);
            var root = doc.RootElement;

            if (!root.TryGetProperty("userinfo_endpoint", out var uiProp) || uiProp.ValueKind != JsonValueKind.String || string.IsNullOrWhiteSpace(uiProp.GetString()))
                return new(OIDCDiagnosticsTestType.UserInfoEndpoint, "userinfo_endpoint not advertised in discovery; skipping probe.", null);

            userInfoUrl = uiProp.GetString()!;

            var resp = await httpClient.PostFormUrlEncodedAsync(userInfoUrl, Array.Empty<KeyValuePair<string, string>>(), ct);
            var status = resp.StatusCode;
            var body = resp.Body ?? string.Empty;

            if (resp.IsSuccessStatusCode || (int)status is 400 or 401 or 403 or 405)
                return new(OIDCDiagnosticsTestType.UserInfoEndpoint, $"Status: OK (endpoint reachable)\nEndpoint: {userInfoUrl}\nHTTP {status}\nBody: {body.Truncate(2048)}\nError: {resp.Error}", null);

            return new(OIDCDiagnosticsTestType.UserInfoEndpoint, $"Endpoint: {userInfoUrl}\nHTTP {status}\nBody: {body.Truncate(2048)}\nError: {resp.Error}", [new("userinfo_unexpected_status", "UserInfo endpoint returned an unexpected status code")]);
        }
        catch (OperationCanceledException)
        {
            return new(OIDCDiagnosticsTestType.UserInfoEndpoint, $"Well known: {wellKnownJson}\nUserInfo: {userInfoUrl}", [new("cancelled", "Operation canceled")]);
        }
        catch (JsonException ex)
        {
            return new(OIDCDiagnosticsTestType.UserInfoEndpoint, $"Well known: {wellKnownJson}", [new("json_error", ex.Message)]);
        }
        catch (Exception ex)
        {
            return new(OIDCDiagnosticsTestType.UserInfoEndpoint, $"Well known: {wellKnownJson}\nUserInfo: {userInfoUrl}", [new("unexpected_error", ex.Message)]);
        }
    }

    public async Task<OIDCDiagnosticsResult> TestRevocationEndpointAsync(IMicroMAppConfiguration appConfig, IOIDCHttpClient httpClient, string appId, CancellationToken ct)
    {
        var app = appConfig.GetAppConfiguration(appId);

        var validate_result = ValidateApp("well-known", app);
        if (validate_result != null || app == null)
            return validate_result!;

        string? wellKnownJson = null;
        string? revocationUrl = null;

        try
        {
            var wk = await httpClient.GetWellKnownJsonAsync(app.OIDCWellKnownURL!, ct);
            if (!wk.IsSuccessStatusCode || string.IsNullOrWhiteSpace(wk.Body))
                return new(OIDCDiagnosticsTestType.RevocationEndpoint, null, [new("wellknown_http_error", wk.Error ?? "Failed to fetch discovery")]);

            wellKnownJson = wk.Body;
            using var doc = JsonDocument.Parse(wellKnownJson);
            var root = doc.RootElement;

            if (!root.TryGetProperty("revocation_endpoint", out var rvProp) || rvProp.ValueKind != JsonValueKind.String || string.IsNullOrWhiteSpace(rvProp.GetString()))
                return new(OIDCDiagnosticsTestType.RevocationEndpoint, "revocation_endpoint not advertised in discovery; skipping probe.", null);

            revocationUrl = rvProp.GetString()!;

            var form = new Dictionary<string, string>
            {
                [WellknownIdentityConstants.Token] = "invalid_token",
                [WellknownIdentityConstants.TokenTypeHint] = WellknownIdentityConstants.RefreshToken,
                [WellknownIdentityConstants.ClientId] = app.ApplicationID
            };

            var resp = await httpClient.PostFormUrlEncodedAsync(revocationUrl, form, ct);
            var status = resp.StatusCode;
            var body = resp.Body ?? string.Empty;

            if (resp.IsSuccessStatusCode || (int)status is 400 or 401 or 403)
                return new(OIDCDiagnosticsTestType.RevocationEndpoint, $"Status: OK (endpoint reachable)\nEndpoint: {revocationUrl}\nHTTP {status}\nBody: {body.Truncate(2048)}\nError: {resp.Error}", null);

            return new(OIDCDiagnosticsTestType.RevocationEndpoint, $"Endpoint: {revocationUrl}\nHTTP {status}\nBody: {body.Truncate(2048)}\nError: {resp.Error}", [new("revocation_unexpected_status", "Revocation endpoint returned an unexpected status code")]);
        }
        catch (OperationCanceledException)
        {
            return new(OIDCDiagnosticsTestType.RevocationEndpoint, $"Well known: {wellKnownJson}\nRevocation: {revocationUrl}", [new("cancelled", "Operation canceled")]);
        }
        catch (JsonException ex)
        {
            return new(OIDCDiagnosticsTestType.RevocationEndpoint, $"Well known: {wellKnownJson}", [new("json_error", ex.Message)]);
        }
        catch (Exception ex)
        {
            return new(OIDCDiagnosticsTestType.RevocationEndpoint, $"Well known: {wellKnownJson}\nRevocation: {revocationUrl}", [new("unexpected_error", ex.Message)]);
        }
    }

    public async Task<OIDCDiagnosticsResult> TestIntrospectionEndpointAsync(IMicroMAppConfiguration appConfig, IOIDCHttpClient httpClient, string appId, CancellationToken ct)
    {
        var app = appConfig.GetAppConfiguration(appId);

        string? wellKnownJson = null;
        string? introspectionUrl = null;

        try
        {
            var wk = await httpClient.GetWellKnownJsonAsync(app.OIDCWellKnownURL!, ct);
            if (!wk.IsSuccessStatusCode || string.IsNullOrWhiteSpace(wk.Body))
                return new(OIDCDiagnosticsTestType.IntrospectionEndpoint, null, [new("wellknown_http_error", wk.Error ?? "Failed to fetch discovery")]);

            wellKnownJson = wk.Body;
            using var doc = JsonDocument.Parse(wellKnownJson);
            var root = doc.RootElement;

            if (!root.TryGetProperty("introspection_endpoint", out var inProp) || inProp.ValueKind != JsonValueKind.String || string.IsNullOrWhiteSpace(inProp.GetString()))
                return new(OIDCDiagnosticsTestType.IntrospectionEndpoint, "introspection_endpoint not advertised in discovery; skipping probe.", null);

            introspectionUrl = inProp.GetString()!;

            var form = new Dictionary<string, string>
            {
                [WellknownIdentityConstants.Token] = "invalid_token",
                [WellknownIdentityConstants.ClientId] = app.ApplicationID
            };

            var resp = await httpClient.PostFormUrlEncodedAsync(introspectionUrl, form, ct);
            var status = resp.StatusCode;
            var body = resp.Body ?? string.Empty;

            if (resp.IsSuccessStatusCode || (int)status is 400 or 401 or 403)
                return new(OIDCDiagnosticsTestType.IntrospectionEndpoint, $"Status: OK (endpoint reachable)\nEndpoint: {introspectionUrl}\nHTTP {status}\nBody: {body.Truncate(2048)}\nError: {resp.Error}", null);

            return new(OIDCDiagnosticsTestType.IntrospectionEndpoint, $"Endpoint: {introspectionUrl}\nHTTP {status}\nBody: {body.Truncate(2048)}\nError: {resp.Error}", [new("introspection_unexpected_status", "Introspection endpoint returned an unexpected status code")]);
        }
        catch (OperationCanceledException)
        {
            return new(OIDCDiagnosticsTestType.IntrospectionEndpoint, $"Well known: {wellKnownJson}\nIntrospection: {introspectionUrl}", [new("cancelled", "Operation canceled")]);
        }
        catch (JsonException ex)
        {
            return new(OIDCDiagnosticsTestType.IntrospectionEndpoint, $"Well known: {wellKnownJson}", [new("json_error", ex.Message)]);
        }
        catch (Exception ex)
        {
            return new(OIDCDiagnosticsTestType.IntrospectionEndpoint, $"Well known: {wellKnownJson}\nIntrospection: {introspectionUrl}", [new("unexpected_error", ex.Message)]);
        }
    }
}
