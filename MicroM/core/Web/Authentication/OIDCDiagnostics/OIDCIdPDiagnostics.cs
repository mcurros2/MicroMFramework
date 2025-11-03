using MicroM.Data;
using MicroM.DataDictionary.CategoriesDefinitions;
using MicroM.Web.Extensions;
using MicroM.Web.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Headers;
using Microsoft.Extensions.Primitives;
using Microsoft.IdentityModel.Tokens;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace MicroM.Web.Authentication.SSO;

/// <summary>
/// Provides diagnostic methods for testing OpenID Connect (OIDC) identity provider configuration for the IDPServer role.
/// </summary>
/// <remarks>
/// Use this class to test and validate the configuration of the IdPServer role in an OIDC setup. It includes methods to test signing material, PAR handling, and end-session fanout to clients.
/// It will test each client APP configured for the app_id IDPServer provided.
/// </remarks>
public class OIDCIdPDiagnostics : IOIDCIdPDiagnostics
{
    public async Task<List<OIDCDiagnosticsResult>> TestAllAsync(IEntityClient ec, string appId, IMicroMAppConfiguration appConfig, IIdentityProviderService idpService, IOIDCHttpClient httpClient, CancellationToken ct)
    {
        List<OIDCDiagnosticsResult> results = [];

        results.Add(await TestWellKnownAndJWKSAsync(appConfig, idpService, appId, ct));
        results.Add(await TestSigningMaterialAsync(appConfig, idpService, appId, ct));
        results.Add(await TestIssuerConsistencyAsync(appConfig, idpService, httpClient, appId, ct));

        var tokenGrantResults = await TestTokenGrantsAsync(appConfig, idpService, httpClient, appId, ct);
        results.AddRange(tokenGrantResults);

        var parResults = await TestPARAsync(appConfig, idpService, appId, ct);
        results.AddRange(parResults);

        var regResults = await TestClientRegistrationSanityAsync(ec, appConfig, idpService, appId, ct);
        results.AddRange(regResults);

        // New: front-channel logout config checks per client
        var fclResults = await TestFrontChannelLogoutAsync(appConfig, idpService, appId, ct);
        results.AddRange(fclResults);

        var fanoutResults = await TestEndSessionFanoutAsync(ec, appConfig, idpService, appId, ct);
        results.AddRange(fanoutResults);

        return results;
    }

    public async Task<List<OIDCDiagnosticsResult>> TestClientRegistrationSanityAsync(IEntityClient ec, IMicroMAppConfiguration appConfig, IIdentityProviderService idpService, string appId, CancellationToken ct)
    {
        var app = appConfig.GetAppConfiguration(appId);
        if (app == null)
        {
            return
            [
                new OIDCDiagnosticsResult(
                    OIDCDiagnosticsTestType.ClientRegistrationSanity,
                    null,
                    [new("app_not_found", "Application configuration not found")]
                )
            ];
        }

        if (app.IdentityProviderRoleType != nameof(IdentityProviderRole.IDPServer))
        {
            return
            [
                new OIDCDiagnosticsResult(
                    OIDCDiagnosticsTestType.ClientRegistrationSanity,
                    null,
                    [new("invalid_role", "Application is not configured as IDPServer")]
                )
            ];
        }

        if (app.OIDCClientConfiguration == null || app.OIDCClientConfiguration.Count == 0)
        {
            return
            [
                new OIDCDiagnosticsResult(
                    OIDCDiagnosticsTestType.ClientRegistrationSanity,
                    "No OIDC clients configured for this IdP",
                    [new("no_clients_configured", "OIDCClientConfiguration is empty")]
                )
            ];
        }

        List<OIDCDiagnosticsResult> results = [];

        foreach (var client in app.OIDCClientConfiguration.Values)
        {
            List<ErrorResult> errs = [];

            if (string.IsNullOrWhiteSpace(client.ClientAPPID))
            {
                errs.Add(new("client_id_missing", "Configured client is missing ClientAPPID"));
            }

            // HTTP is forbidden: must be HTTPS
            if (!client.URLBackchannelLogout.isValidHTTPSUrl())
            {
                errs.Add(new("backchannel_url_invalid", $"Backchannel logout URL must be HTTPS: {client.URLBackchannelLogout}"));
            }

            if (!string.IsNullOrWhiteSpace(client.URLClientJWKS) && !client.URLClientJWKS.isValidHTTPSUrl())
            {
                errs.Add(new("jwks_url_invalid", $"Client JWKS URL must be HTTPS: {client.URLClientJWKS}"));
            }

            if (!string.IsNullOrWhiteSpace(client.URLFrontChannelLogout) && !client.URLFrontChannelLogout.isValidHTTPSUrl())
            {
                errs.Add(new("frontchannel_url_invalid", $"Front-channel logout URL must be HTTPS: {client.URLFrontChannelLogout}"));
            }

            if (client.URLAuthorizedRedirects == null || client.URLAuthorizedRedirects.Count == 0)
            {
                errs.Add(new("redirects_missing", "No authorized redirect URIs configured"));
            }
            else
            {
                foreach (var r in client.URLAuthorizedRedirects)
                {
                    if (!r.isValidHTTPSUrl())
                    {
                        errs.Add(new("redirect_invalid", $"Redirect URI must be HTTPS: {r}"));
                    }
                }
            }

            string result = $"Client: {client.ClientAPPID}\nBackchannel: {client.URLBackchannelLogout}\nFrontChannel: {client.URLFrontChannelLogout}\nJWKS: {client.URLClientJWKS}\nRedirects: {(client.URLAuthorizedRedirects == null ? "(null)" : string.Join(",", client.URLAuthorizedRedirects))}";

            results.Add(new OIDCDiagnosticsResult(
                OIDCDiagnosticsTestType.ClientRegistrationSanity,
                result,
                errs.Count == 0 ? null : errs
            ));
        }

        return results;
    }

    public async Task<List<OIDCDiagnosticsResult>> TestEndSessionFanoutAsync(IEntityClient ec, IMicroMAppConfiguration appConfig, IIdentityProviderService idpService, string appId, CancellationToken ct)
    {
        var app = appConfig.GetAppConfiguration(appId);
        if (app == null)
        {
            return
            [
                new OIDCDiagnosticsResult(
                    OIDCDiagnosticsTestType.EndSessionFanout,
                    null,
                    [new("app_not_found", "Application configuration not found")]
                )
            ];
        }

        if (app.IdentityProviderRoleType != nameof(IdentityProviderRole.IDPServer))
        {
            return
            [
                new OIDCDiagnosticsResult(
                    OIDCDiagnosticsTestType.EndSessionFanout,
                    null,
                    [new("invalid_role", "Application is not configured as IDPServer")]
                )
            ];
        }

        // issuer derived from configuration (preferred WellKnownURL, fallback JWTIssuer)
        string? requestBase = null;
        if (!string.IsNullOrWhiteSpace(app.OIDCWellKnownURL) && Uri.TryCreate(app.OIDCWellKnownURL, UriKind.Absolute, out var wkUri))
        {
            requestBase = wkUri.AbsoluteUri.Replace("/oidc/.well-known/openid-configuration", "", StringComparison.OrdinalIgnoreCase).TrimEnd('/');
        }
        else if (!string.IsNullOrWhiteSpace(app.JWTIssuer) && app.JWTIssuer.EndsWith("/oidc", StringComparison.Ordinal))
        {
            requestBase = app.JWTIssuer[..^"/oidc".Length];
        }

        if (string.IsNullOrWhiteSpace(requestBase))
        {
            return
            [
                new OIDCDiagnosticsResult(
                    OIDCDiagnosticsTestType.EndSessionFanout,
                    $"issuer: (unavailable)\nWellKnown: {app.OIDCWellKnownURL}\nJWTIssuer: {app.JWTIssuer}",
                    [new("request_base_unavailable", "Unable to derive request base from configuration (OIDCWellKnownURL/JWTIssuer)")]
                )
            ];
        }

        string issuer = $"{requestBase}/oidc";
        string subject = "diagnostics-subject";

        try
        {
            var ok = await idpService.HandleEndSession(app, issuer, subject, ct);

            if (!ok)
            {
                return
                [
                    new OIDCDiagnosticsResult(
                        OIDCDiagnosticsTestType.EndSessionFanout,
                        $"issuer: {issuer}\nsubject: {subject}",
                        [new("logout_failed", "EndSession fanout returned false")]
                    )
                ];
            }

            return
            [
                new OIDCDiagnosticsResult(
                    OIDCDiagnosticsTestType.EndSessionFanout,
                    $"Status: OK\nissuer: {issuer}\nsubject: {subject}",
                    null
                )
            ];
        }
        catch (OperationCanceledException)
        {
            return
            [
                new OIDCDiagnosticsResult(
                    OIDCDiagnosticsTestType.EndSessionFanout,
                    $"issuer: {issuer}\nsubject: {subject}",
                    [new("cancelled", "Operation canceled")]
                )
            ];
        }
        catch (Exception ex)
        {
            return
            [
                new OIDCDiagnosticsResult(
                    OIDCDiagnosticsTestType.EndSessionFanout,
                    $"issuer: {issuer}\nsubject: {subject}",
                    [new("unexpected_error", ex.Message)]
                )
            ];
        }
    }

    public async Task<OIDCDiagnosticsResult> TestIssuerConsistencyAsync(IMicroMAppConfiguration appConfig, IIdentityProviderService idpService, IOIDCHttpClient httpClient, string appId, CancellationToken ct)
    {
        var app = appConfig.GetAppConfiguration(appId);
        if (app == null)
        {
            return new OIDCDiagnosticsResult(
                OIDCDiagnosticsTestType.WellKnownAndJWKS,
                null,
                [new("app_not_found", "Application configuration not found")]
            );
        }

        if (app.IdentityProviderRoleType != nameof(IdentityProviderRole.IDPServer))
        {
            return new OIDCDiagnosticsResult(
                OIDCDiagnosticsTestType.WellKnownAndJWKS,
                null,
                [new("invalid_role", "Application is not configured as IDPServer")]
            );
        }

        string? requestBase = null;
        string? wellKnownFromConfig = app.OIDCWellKnownURL;

        if (!string.IsNullOrWhiteSpace(wellKnownFromConfig) && Uri.TryCreate(wellKnownFromConfig, UriKind.Absolute, out var wkUri))
        {
            var trimmed = wkUri.AbsoluteUri.Replace("/oidc/.well-known/openid-configuration", "", StringComparison.OrdinalIgnoreCase).TrimEnd('/');
            requestBase = trimmed;
        }
        else if (!string.IsNullOrWhiteSpace(app.JWTIssuer) && app.JWTIssuer.EndsWith("/oidc", StringComparison.Ordinal))
        {
            requestBase = app.JWTIssuer[..^"/oidc".Length];
        }

        if (string.IsNullOrWhiteSpace(requestBase))
        {
            return new OIDCDiagnosticsResult(
                OIDCDiagnosticsTestType.WellKnownAndJWKS,
                $"Well known: (not generated)\n\nExpected issuer: (unavailable)\n\nConfig WellKnown: {app.OIDCWellKnownURL}\nJWTIssuer: {app.JWTIssuer}",
                [new("request_base_unavailable", "Unable to derive request base from configuration (OIDCWellKnownURL/JWTIssuer)")]
            );
        }

        var reqHeaders = new RequestHeaders(new HeaderDictionary());
        var respHeaders = new HeaderDictionary();

        string? wellKnownJson = null;

        try
        {
            var wk = idpService.HandleWellKnown(app, requestBase, reqHeaders, respHeaders);
            wellKnownJson = wk.etag_content.Content;

            using var doc = JsonDocument.Parse(wellKnownJson);
            var root = doc.RootElement;

            var hasIssuer = root.TryGetProperty("issuer", out var issuerProp) && issuerProp.ValueKind == JsonValueKind.String && !string.IsNullOrWhiteSpace(issuerProp.GetString());
            var issuer = hasIssuer ? issuerProp.GetString() : null;
            var expectedIssuer = $"{requestBase}/oidc";

            if (!hasIssuer || !string.Equals(issuer, expectedIssuer, StringComparison.Ordinal))
            {
                return new OIDCDiagnosticsResult(
                    OIDCDiagnosticsTestType.WellKnownAndJWKS,
                    $"Well known: {wellKnownJson}\n\nExpected issuer: {expectedIssuer}",
                    [new("issuer_mismatch", $"Discovery issuer does not match expected: '{issuer}' vs '{expectedIssuer}'")]
                );
            }

            return new OIDCDiagnosticsResult(
                OIDCDiagnosticsTestType.WellKnownAndJWKS,
                $"Status: OK\n\nWell known: {wellKnownJson}\n\nExpected issuer: {expectedIssuer}",
                null
            );
        }
        catch (OperationCanceledException)
        {
            return new OIDCDiagnosticsResult(
                OIDCDiagnosticsTestType.WellKnownAndJWKS,
                $"Well known: {wellKnownJson}\n\nExpected issuer: {requestBase}/oidc",
                [new("cancelled", "Operation canceled")]
            );
        }
        catch (JsonException ex)
        {
            return new OIDCDiagnosticsResult(
                OIDCDiagnosticsTestType.WellKnownAndJWKS,
                $"Well known: {wellKnownJson}\n\nExpected issuer: {requestBase}/oidc",
                [new("json_error", ex.Message)]
            );
        }
        catch (Exception ex)
        {
            return new OIDCDiagnosticsResult(
                OIDCDiagnosticsTestType.WellKnownAndJWKS,
                $"Well known: {wellKnownJson}\n\nExpected issuer: {requestBase}/oidc",
                [new("unexpected_error", ex.Message)]
            );
        }
    }

    public async Task<List<OIDCDiagnosticsResult>> TestPARAsync(IMicroMAppConfiguration appConfig, IIdentityProviderService idpService, string appId, CancellationToken ct)
    {
        var app = appConfig.GetAppConfiguration(appId);
        if (app == null)
        {
            return
            [
                new OIDCDiagnosticsResult(
                    OIDCDiagnosticsTestType.PAR_IdPServer,
                    null,
                    [new("app_not_found", "Application configuration not found")]
                )
            ];
        }

        if (app.IdentityProviderRoleType != nameof(IdentityProviderRole.IDPServer))
        {
            return
            [
                new OIDCDiagnosticsResult(
                    OIDCDiagnosticsTestType.PAR_IdPServer,
                    null,
                    [new("invalid_role", "Application is not configured as IDPServer")]
                )
            ];
        }

        if (app.OIDCClientConfiguration == null || app.OIDCClientConfiguration.Count == 0)
        {
            return
            [
                new OIDCDiagnosticsResult(
                    OIDCDiagnosticsTestType.PAR_IdPServer,
                    "No OIDC clients configured for this IdP",
                    [new("no_clients_configured", "OIDCClientConfiguration is empty")]
                )
            ];
        }

        List<OIDCDiagnosticsResult> results = [];

        foreach (var client in app.OIDCClientConfiguration.Values)
        {
            var clientId = client.ClientAPPID;
            var redirect = (client.URLAuthorizedRedirects != null && client.URLAuthorizedRedirects.Count > 0) ? client.URLAuthorizedRedirects[0] : null;

            if (string.IsNullOrWhiteSpace(clientId))
            {
                results.Add(new OIDCDiagnosticsResult(
                    OIDCDiagnosticsTestType.PAR_IdPServer,
                    "Client selected from configuration has no ClientAPPID",
                    [new("client_id_missing", "Configured client is missing ClientAPPID")]
                ));
                continue;
            }

            if (string.IsNullOrWhiteSpace(redirect))
            {
                results.Add(new OIDCDiagnosticsResult(
                    OIDCDiagnosticsTestType.PAR_IdPServer,
                    $"Client: {clientId} has no authorized redirect URIs",
                    [new("client_redirects_missing", "Configured client has no URLAuthorizedRedirects")]
                ));
                continue;
            }

            // PKCE: use built-in Base64Url encoder (no custom helper)
            var verifierBytes = RandomNumberGenerator.GetBytes(32);
            var codeVerifier = Base64UrlEncoder.Encode(verifierBytes);
            var challenge = Base64UrlEncoder.Encode(SHA256.HashData(Encoding.ASCII.GetBytes(codeVerifier)));

            // state/nonce: built-in base64url encoding over RNG bytes
            var state = Base64UrlEncoder.Encode(RandomNumberGenerator.GetBytes(32));
            var nonce = Base64UrlEncoder.Encode(RandomNumberGenerator.GetBytes(32));

            var formData = new Dictionary<string, StringValues>
            {
                [WellknownIdentityConstants.ResponseType] = WellknownIdentityConstants.Code,
                [WellknownIdentityConstants.ClientId] = clientId,
                [WellknownIdentityConstants.RedirectUri] = redirect,
                [WellknownIdentityConstants.Scope] = $"{WellknownIdentityConstants.OpenID} profile email",
                [WellknownIdentityConstants.State] = state,
                [WellknownIdentityConstants.Nonce] = nonce,
                [WellknownIdentityConstants.CodeChallenge] = challenge,
                [WellknownIdentityConstants.CodeChallengeMethod] = "S256"
            };
            var form = new FormCollection(formData);
            string formSummary = $"client_id={clientId}, redirect_uri={redirect}, scope={WellknownIdentityConstants.OpenID} profile email";

            try
            {
                var id = new ClaimsIdentity("oidc_client");
                id.AddClaim(new Claim("client_id", clientId));
                id.AddClaim(new Claim(ClaimTypes.Name, clientId));
                var principal = new ClaimsPrincipal(id);

                var (response, error) = idpService.HandlePAR(app, form, principal);

                if (error != null || response == null)
                {
                    var errorJson = JsonSerializer.Serialize(error);
                    results.Add(new OIDCDiagnosticsResult(
                        OIDCDiagnosticsTestType.PAR_IdPServer,
                        $"Client: {clientId}\nForm: {formSummary}\nError: {errorJson}",
                        [new("par_failed", "PAR did not return a request_uri")]
                    ));
                }
                else
                {
                    var responseJson = JsonSerializer.Serialize(response);
                    results.Add(new OIDCDiagnosticsResult(
                        OIDCDiagnosticsTestType.PAR_IdPServer,
                        $"Status: OK\nClient: {clientId}\nForm: {formSummary}\nResponse: {responseJson}",
                        null
                    ));
                }
            }
            catch (OperationCanceledException)
            {
                results.Add(new OIDCDiagnosticsResult(
                    OIDCDiagnosticsTestType.PAR_IdPServer,
                    $"Client: {clientId}\nForm: {formSummary}",
                    [new("cancelled", "Operation canceled")]
                ));
            }
            catch (Exception ex)
            {
                results.Add(new OIDCDiagnosticsResult(
                    OIDCDiagnosticsTestType.PAR_IdPServer,
                    $"Client: {clientId}\nForm: {formSummary}",
                    [new("unexpected_error", ex.Message)]
                ));
            }
        }

        return results;
    }

    public async Task<OIDCDiagnosticsResult> TestSigningMaterialAsync(IMicroMAppConfiguration appConfig, IIdentityProviderService idpService, string appId, CancellationToken ct)
    {
        var app = appConfig.GetAppConfiguration(appId);
        if (app == null)
        {
            return new OIDCDiagnosticsResult(
                OIDCDiagnosticsTestType.SigningMaterial,
                null,
                [new("app_not_found", "Application configuration not found")]
            );
        }

        if (app.IdentityProviderRoleType != nameof(IdentityProviderRole.IDPServer))
        {
            return new OIDCDiagnosticsResult(
                OIDCDiagnosticsTestType.SigningMaterial,
                null,
                [new("invalid_role", "Application is not configured as IDPServer")]
            );
        }

        var reqHeaders = new RequestHeaders(new HeaderDictionary());
        var respHeaders = new HeaderDictionary();

        string? jwksJson = null;

        try
        {
            var jwks = idpService.HandleJwks(app, reqHeaders, respHeaders);
            if (jwks == null)
            {
                return new OIDCDiagnosticsResult(
                    OIDCDiagnosticsTestType.SigningMaterial,
                    $"JWKS: {jwksJson}",
                    [new("jwks_null", "JWKS content not available")]
                );
            }

            jwksJson = jwks.etag_content.Content;

            using var jwksDoc = JsonDocument.Parse(jwksJson);
            if (!jwksDoc.RootElement.TryGetProperty("keys", out var keysEl) || keysEl.ValueKind != JsonValueKind.Array || keysEl.GetArrayLength() <= 0)
            {
                return new OIDCDiagnosticsResult(
                    OIDCDiagnosticsTestType.SigningMaterial,
                    $"JWKS: {jwksJson}",
                    [new("jwks_empty", "JWKS contains no keys")]
                );
            }

            var keyErrors = new List<ErrorResult>();
            foreach (var key in keysEl.EnumerateArray())
            {
                if (!key.TryGetProperty("kid", out var kid) || kid.ValueKind != JsonValueKind.String || string.IsNullOrWhiteSpace(kid.GetString()))
                {
                    keyErrors.Add(new ErrorResult("jwks_key_missing_kid", "A JWKS key is missing 'kid'"));
                }
                if (!key.TryGetProperty("kty", out var kty) || kty.ValueKind != JsonValueKind.String || string.IsNullOrWhiteSpace(kty.GetString()))
                {
                    keyErrors.Add(new ErrorResult("jwks_key_missing_kty", "A JWKS key is missing 'kty'"));
                }
            }

            if (keyErrors.Count > 0)
            {
                return new OIDCDiagnosticsResult(
                    OIDCDiagnosticsTestType.SigningMaterial,
                    $"JWKS: {jwksJson}",
                    keyErrors
                );
            }

            return new OIDCDiagnosticsResult(
                OIDCDiagnosticsTestType.SigningMaterial,
                $"Status: OK\n\nJWKS: {jwksJson}",
                null
            );
        }
        catch (OperationCanceledException)
        {
            return new OIDCDiagnosticsResult(
                OIDCDiagnosticsTestType.SigningMaterial,
                $"JWKS: {jwksJson}",
                [new("cancelled", "Operation canceled")]
            );
        }
        catch (JsonException ex)
        {
            return new OIDCDiagnosticsResult(
                OIDCDiagnosticsTestType.SigningMaterial,
                $"JWKS: {jwksJson}",
                [new("json_error", ex.Message)]
            );
        }
        catch (Exception ex)
        {
            return new OIDCDiagnosticsResult(
                OIDCDiagnosticsTestType.SigningMaterial,
                $"JWKS: {jwksJson}",
                [new("unexpected_error", ex.Message)]
            );
        }
    }

    public async Task<OIDCDiagnosticsResult> TestWellKnownAndJWKSAsync(
        IMicroMAppConfiguration appConfig,
        IIdentityProviderService idpService,
        string appId,
        CancellationToken ct)
    {
        var app = appConfig.GetAppConfiguration(appId);
        if (app == null)
        {
            return new OIDCDiagnosticsResult(
                OIDCDiagnosticsTestType.WellKnownAndJWKS,
                null,
                [new("app_not_found", "Application configuration not found")]
            );
        }

        if (app.IdentityProviderRoleType != nameof(IdentityProviderRole.IDPServer))
        {
            return new OIDCDiagnosticsResult(
                OIDCDiagnosticsTestType.WellKnownAndJWKS,
                null,
                [new("invalid_role", "Application is not configured as IDPServer")]
            );
        }

        // Derive requestBase from configuration (prefer WellKnown URL, fallback JWTIssuer)
        string? requestBase = null;
        if (!string.IsNullOrWhiteSpace(app.OIDCWellKnownURL) && Uri.TryCreate(app.OIDCWellKnownURL, UriKind.Absolute, out var wkUri))
        {
            requestBase = wkUri.AbsoluteUri.Replace("/oidc/.well-known/openid-configuration", "", StringComparison.OrdinalIgnoreCase).TrimEnd('/');
        }
        else if (!string.IsNullOrWhiteSpace(app.JWTIssuer) && app.JWTIssuer.EndsWith("/oidc", StringComparison.Ordinal))
        {
            requestBase = app.JWTIssuer[..^"/oidc".Length];
        }

        if (string.IsNullOrWhiteSpace(requestBase))
        {
            return new OIDCDiagnosticsResult(
                OIDCDiagnosticsTestType.WellKnownAndJWKS,
                $"Well known: (not generated)\n\nJWKS: (not requested)\n\nConfig WellKnown: {app.OIDCWellKnownURL}\nJWTIssuer: {app.JWTIssuer}",
                [new("request_base_unavailable", "Unable to derive request base from configuration (OIDCWellKnownURL/JWTIssuer)")]
            );
        }

        var reqHeaders = new RequestHeaders(new HeaderDictionary());
        var respHeaders = new HeaderDictionary();

        string? wellKnownJson = null;
        string? jwksJson = null;

        try
        {
            var wk = idpService.HandleWellKnown(app, requestBase, reqHeaders, respHeaders);
            wellKnownJson = wk.etag_content.Content;

            using var doc = JsonDocument.Parse(wellKnownJson);
            var root = doc.RootElement;

            var hasIssuer = root.TryGetProperty("issuer", out var issuerProp) && issuerProp.ValueKind == JsonValueKind.String && !string.IsNullOrWhiteSpace(issuerProp.GetString());
            var hasJwksUri = root.TryGetProperty("jwks_uri", out var jwksProp) && jwksProp.ValueKind == JsonValueKind.String && !string.IsNullOrWhiteSpace(jwksProp.GetString());
            if (!hasIssuer || !hasJwksUri)
            {
                return new OIDCDiagnosticsResult(
                    OIDCDiagnosticsTestType.WellKnownAndJWKS,
                    $"Well known: {wellKnownJson}\n\nJWKS: {jwksJson}",
                    [new("wellknown_invalid", "Discovery document missing required fields: issuer and/or jwks_uri")]
                );
            }

            var jwks = idpService.HandleJwks(app, reqHeaders, respHeaders);
            if (jwks == null)
            {
                return new OIDCDiagnosticsResult(
                    OIDCDiagnosticsTestType.WellKnownAndJWKS,
                    $"Well known: {wellKnownJson}\n\nJWKS: {jwksJson}",
                    [new("jwks_null", "JWKS content not available")]
                );
            }

            jwksJson = jwks.etag_content.Content;

            using var jwksDoc = JsonDocument.Parse(jwksJson);
            if (!jwksDoc.RootElement.TryGetProperty("keys", out var keysEl) || keysEl.ValueKind != JsonValueKind.Array || keysEl.GetArrayLength() <= 0)
            {
                return new OIDCDiagnosticsResult(
                    OIDCDiagnosticsTestType.WellKnownAndJWKS,
                    $"Well known: {wellKnownJson}\n\nJWKS: {jwksJson}",
                    [new("jwks_empty", "JWKS contains no keys")]
                );
            }

            return new OIDCDiagnosticsResult(
                OIDCDiagnosticsTestType.WellKnownAndJWKS,
                $"Status: OK\n\nWell known: {wellKnownJson}\n\nJWKS: {jwksJson}",
                null
            );
        }
        catch (OperationCanceledException)
        {
            return new OIDCDiagnosticsResult(
                OIDCDiagnosticsTestType.WellKnownAndJWKS,
                $"Well known: {wellKnownJson}\n\nJWKS: {jwksJson}",
                [new("cancelled", "Operation canceled")]
            );
        }
        catch (JsonException ex)
        {
            return new OIDCDiagnosticsResult(
                OIDCDiagnosticsTestType.WellKnownAndJWKS,
                $"Well known: {wellKnownJson}\n\nJWKS: {jwksJson}",
                [new("json_error", ex.Message)]
            );
        }
        catch (Exception ex)
        {
            return new OIDCDiagnosticsResult(
                OIDCDiagnosticsTestType.WellKnownAndJWKS,
                $"Well known: {wellKnownJson}\n\nJWKS: {jwksJson}",
                [new("unexpected_error", ex.Message)]
            );
        }
    }

    public async Task<List<OIDCDiagnosticsResult>> TestTokenGrantsAsync(IMicroMAppConfiguration appConfig, IIdentityProviderService idpService, IOIDCHttpClient httpClient, string appId, CancellationToken ct)
    {
        var app = appConfig.GetAppConfiguration(appId);
        if (app == null)
        {
            return
            [
                new OIDCDiagnosticsResult(
                    OIDCDiagnosticsTestType.PAR_IdPServer,
                    null,
                    [new("app_not_found", "Application configuration not found")]
                )
            ];
        }

        if (app.IdentityProviderRoleType != nameof(IdentityProviderRole.IDPServer))
        {
            return
            [
                new OIDCDiagnosticsResult(
                    OIDCDiagnosticsTestType.PAR_IdPServer,
                    null,
                    [new("invalid_role", "Application is not configured as IDPServer")]
                )
            ];
        }

        if (app.OIDCClientConfiguration == null || app.OIDCClientConfiguration.Count == 0)
        {
            return
            [
                new OIDCDiagnosticsResult(
                    OIDCDiagnosticsTestType.PAR_IdPServer,
                    "No OIDC clients configured for this IdP",
                    [new("no_clients_configured", "OIDCClientConfiguration is empty")]
                )
            ];
        }

        List<OIDCDiagnosticsResult> results = [];

        foreach (var client in app.OIDCClientConfiguration.Values)
        {
            var clientId = client.ClientAPPID;
            var redirect = (client.URLAuthorizedRedirects != null && client.URLAuthorizedRedirects.Count > 0) ? client.URLAuthorizedRedirects[0] : null;

            if (string.IsNullOrWhiteSpace(clientId))
            {
                results.Add(new OIDCDiagnosticsResult(
                    OIDCDiagnosticsTestType.PAR_IdPServer,
                    "Client selected from configuration has no ClientAPPID",
                    [new("client_id_missing", "Configured client is missing ClientAPPID")]
                ));
                continue;
            }

            if (string.IsNullOrWhiteSpace(redirect))
            {
                results.Add(new OIDCDiagnosticsResult(
                    OIDCDiagnosticsTestType.PAR_IdPServer,
                    $"Client: {clientId} has no authorized redirect URIs",
                    [new("client_redirects_missing", "Configured client has no URLAuthorizedRedirects")]
                ));
                continue;
            }

            var formData = new Dictionary<string, StringValues>
            {
                [WellknownIdentityConstants.GrantType] = WellknownIdentityConstants.AuthorizationCode,
                [WellknownIdentityConstants.Code] = "invalid_code",
                [WellknownIdentityConstants.RedirectUri] = redirect,
                [WellknownIdentityConstants.ClientId] = clientId,
                [WellknownIdentityConstants.CodeVerifier] = "invalid_verifier"
            };

            var form = new FormCollection(formData);
            string formSummary = $"grant_type={WellknownIdentityConstants.AuthorizationCode}, code=invalid_code, redirect_uri={redirect}, client_id={clientId}";

            try
            {
                var id = new ClaimsIdentity("oidc_client");
                id.AddClaim(new Claim("client_id", clientId));
                id.AddClaim(new Claim(ClaimTypes.Name, clientId));
                var principal = new ClaimsPrincipal(id);

                var (response, error) = await idpService.HandleToken(app, form, principal, ct);

                if (error == null)
                {
                    var responseJson = response != null ? JsonSerializer.Serialize(response) : "null";
                    results.Add(new OIDCDiagnosticsResult(
                        OIDCDiagnosticsTestType.PAR_IdPServer,
                        $"Token endpoint responded without error for invalid request.\n\nClient: {clientId}\nForm: {formSummary}\n\nResponse: {responseJson}",
                        [new("token_grant_unexpected_success", "Expected an error for invalid authorization_code but none was returned")]
                    ));
                }
                else
                {
                    var errorJson = JsonSerializer.Serialize(error);
                    results.Add(new OIDCDiagnosticsResult(
                        OIDCDiagnosticsTestType.PAR_IdPServer,
                        $"Status: OK (error semantics observed)\n\nClient: {clientId}\nForm: {formSummary}\n\nError: {errorJson}",
                        null
                    ));
                }
            }
            catch (OperationCanceledException)
            {
                results.Add(new OIDCDiagnosticsResult(
                    OIDCDiagnosticsTestType.PAR_IdPServer,
                    $"Client: {clientId}\nForm: {formSummary}",
                    [new("cancelled", "Operation canceled")]
                ));
            }
            catch (Exception ex)
            {
                results.Add(new OIDCDiagnosticsResult(
                    OIDCDiagnosticsTestType.PAR_IdPServer,
                    $"Client: {clientId}\nForm: {formSummary}",
                    [new("unexpected_error", ex.Message)]
                ));
            }
        }

        return results;
    }

    public async Task<List<OIDCDiagnosticsResult>> TestFrontChannelLogoutAsync(IMicroMAppConfiguration appConfig, IIdentityProviderService idpService, string appId, CancellationToken ct)
    {
        var app = appConfig.GetAppConfiguration(appId);
        if (app == null)
        {
            return
            [
                new OIDCDiagnosticsResult(
                    OIDCDiagnosticsTestType.ClientRegistrationSanity,
                    null,
                    [new("app_not_found", "Application configuration not found")]
                )
            ];
        }

        if (app.IdentityProviderRoleType != nameof(IdentityProviderRole.IDPServer))
        {
            return
            [
                new OIDCDiagnosticsResult(
                    OIDCDiagnosticsTestType.ClientRegistrationSanity,
                    null,
                    [new("invalid_role", "Application is not configured as IDPServer")]
                )
            ];
        }

        if (app.OIDCClientConfiguration == null || app.OIDCClientConfiguration.Count == 0)
        {
            return
            [
                new OIDCDiagnosticsResult(
                    OIDCDiagnosticsTestType.ClientRegistrationSanity,
                    "No OIDC clients configured for this IdP",
                    [new("no_clients_configured", "OIDCClientConfiguration is empty")]
                )
            ];
        }

        List<OIDCDiagnosticsResult> results = [];
        foreach (var client in app.OIDCClientConfiguration.Values)
        {
            List<ErrorResult> errs = [];

            var clientId = client.ClientAPPID ?? string.Empty;
            var frontUrl = client.URLFrontChannelLogout ?? string.Empty;
            var redirect = (client.URLAuthorizedRedirects != null && client.URLAuthorizedRedirects.Count > 0) ? client.URLAuthorizedRedirects[0] : null;

            if (string.IsNullOrWhiteSpace(clientId))
            {
                errs.Add(new("client_id_missing", "Configured client is missing ClientAPPID"));
            }

            if (string.IsNullOrWhiteSpace(frontUrl))
            {
                errs.Add(new("frontchannel_url_missing", "Front-channel logout URL is missing"));
            }
            else if (!frontUrl.isValidHTTPSUrl())
            {
                errs.Add(new("frontchannel_url_invalid", $"Front-channel logout URL must be HTTPS: {frontUrl}"));
            }

            if (string.IsNullOrWhiteSpace(redirect) || !redirect.isValidHTTPSUrl())
            {
                errs.Add(new("redirect_invalid", $"At least one HTTPS redirect_uri is required to validate end-session round-trip. Found: {redirect ?? "(null)"}"));
            }

            var summary = $"Client: {clientId}\nFrontChannel: {frontUrl}\nSampleRedirect: {redirect ?? "(null)"}";

            results.Add(new OIDCDiagnosticsResult(
                OIDCDiagnosticsTestType.ClientRegistrationSanity,
                summary,
                errs.Count == 0 ? null : errs
            ));
        }

        return results;
    }
}

