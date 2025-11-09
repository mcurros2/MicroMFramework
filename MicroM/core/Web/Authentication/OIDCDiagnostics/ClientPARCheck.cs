using MicroM.Configuration;
using MicroM.Core;
using MicroM.Diagnostics;
using MicroM.Extensions;
using MicroM.Web.Authentication.SSO;
using Microsoft.IdentityModel.Tokens;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text.Json;

namespace MicroM.Web.Authentication.OIDCDiagnostics;

internal class ClientPARCheck() : IDiagnosticCheck<ClientDiagnosticsContext>
{
    public string DiagnosticId => "oidc_client_par_check";

    private static (Dictionary<string, string> form, string codeVerifier) BuildParForm(
        ApplicationOption app,
        IEnumerable<string> scopes,
        string redirectUri,
        string parEndpoint,
        CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        // PKCE
        var codeVerifier = CryptClass.GenerateBase64UrlRandomCode(32);
        string codeChallenge;
        using (var sha = SHA256.Create())
        {
            codeChallenge = Base64UrlEncoder.Encode(sha.ComputeHash(System.Text.Encoding.ASCII.GetBytes(codeVerifier)));
        }

        var state = CryptClass.GenerateBase64UrlRandomCode(32);
        var nonce = CryptClass.GenerateBase64UrlRandomCode(32);

        var form = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            [WellknownIdentityConstants.ResponseType] = WellknownIdentityConstants.Code,
            [WellknownIdentityConstants.ClientId] = app.ApplicationID,
            [WellknownIdentityConstants.RedirectUri] = redirectUri,
            [WellknownIdentityConstants.Scope] = string.Join(' ', scopes),
            [WellknownIdentityConstants.CodeChallenge] = codeChallenge,
            [WellknownIdentityConstants.CodeChallengeMethod] = "S256",
            [WellknownIdentityConstants.State] = state,
            [WellknownIdentityConstants.Nonce] = nonce
        };

        // Optional client authentication: private_key_jwt if client has a certificate configured
        if (app.OIDCCertificateBlob is { Length: > 0 } && !string.IsNullOrWhiteSpace(app.OIDCCertificatePassword))
        {
            using var cert = new System.Security.Cryptography.X509Certificates.X509Certificate2(app.OIDCCertificateBlob, app.OIDCCertificatePassword);

            // Build assertion using provider (audience = PAR endpoint). Pass null alg to use sensible default (ES256/RS256).
            var clientAssertion = PushedAuthorizationProvider.BuildClientAssertion(cert, app.ApplicationID, parEndpoint, null);

            form[WellknownIdentityConstants.ClientAssertionType] = WellknownIdentityConstants.ClientAssertionTypeJwtBearer;
            form[WellknownIdentityConstants.ClientAssertion] = clientAssertion;
        }

        return (form, codeVerifier);
    }

    public async Task<List<DiagnosticResult>> RunCheckAsync(ClientDiagnosticsContext ctx, CancellationToken ct)
    {
        var app = ctx.App;
        var httpClient = ctx.HttpClient;

        string? parEndpoint = null;
        string? authorizeEndpoint = ctx.authorizeURL;

        try
        {
            if (string.IsNullOrWhiteSpace(ctx.parURL))
                return [new(DiagnosticId, Errors: [new("par_url_empty", "PAR endpoint not advertised in discovery; skipping test.")])];

            // Choose a registered redirect_uri
            var redirectUri = app.FrontendURLS?.FirstOrDefault(u => !string.IsNullOrWhiteSpace(u));
            if (string.IsNullOrWhiteSpace(redirectUri))
                return [new(DiagnosticId, Errors: [new("redirect_uri_missing", "No configured redirect_uri found to perform PAR")])];

            parEndpoint = ctx.parURL;

            var (form, codeVerifier) = BuildParForm(
                app,
                scopes: [WellknownIdentityConstants.OpenID],
                redirectUri: redirectUri!,
                parEndpoint: parEndpoint!,
                ct: ct);

            // 1) Send PAR
            var parRes = await httpClient.PostPushedAuthorizationRequestAsync(parEndpoint!, form, authorization: (AuthenticationHeaderValue?)null, ct);
            var statusCode = (int)parRes.StatusCode;
            var body = parRes.Body ?? string.Empty;

            if (!parRes.IsSuccessStatusCode)
            {
                return [new(DiagnosticId,
                    Result: $"PAR failed\nEndpoint: {parEndpoint}\nHTTP {statusCode}\nBody: {body.Truncate(2048)}\nError: {parRes.Error}",
                    Errors: [new("par_failed", "PAR request failed (expected success)")]
                )];
            }

            // Parse PAR response: must contain request_uri & expires_in
            string? requestUriValue;
            int? expiresInValue;
            try
            {
                using var parDoc = JsonDocument.Parse(body);
                var root = parDoc.RootElement;

                if (!root.TryGetProperty(WellknownIdentityConstants.RequestUri, out var reqUriProp) || reqUriProp.ValueKind != JsonValueKind.String)
                {
                    return [new(DiagnosticId,
                        Result: body.Truncate(2048),
                        Errors: [new("par_response_invalid", "PAR success response missing request_uri")]
                    )];
                }
                requestUriValue = reqUriProp.GetString();

                if (!root.TryGetProperty(WellknownIdentityConstants.ExpiresIn, out var expProp) ||
                    expProp.ValueKind != JsonValueKind.Number ||
                    !expProp.TryGetInt32(out var expInt))
                {
                    return [new(DiagnosticId,
                        Result: body.Truncate(2048),
                        Errors: [new("par_response_invalid", "PAR success response missing expires_in")]
                    )];
                }
                expiresInValue = expInt;
            }
            catch (JsonException ex)
            {
                return [new(DiagnosticId,
                    Result: body.Truncate(2048),
                    Errors: [new("par_response_json_error", $"Invalid PAR response JSON: {ex.Message}")]
                )];
            }

            // 2) Verify the request_uri is accepted by the authorize endpoint (non-interactive check).
            // RFC allows POST to authorization endpoint; we'll POST client_id + request_uri and accept:
            // - 2xx (OK), or 3xx (redirect to login), or 4xx that is NOT invalid_request_uri.
            string authorizeCheck = "skipped (authorize endpoint not advertised)";
            if (!string.IsNullOrWhiteSpace(authorizeEndpoint))
            {
                var authForm = new Dictionary<string, string>(StringComparer.Ordinal)
                {
                    [WellknownIdentityConstants.ClientId] = app.ApplicationID,
                    [WellknownIdentityConstants.RequestUri] = requestUriValue!
                };

                var authRes = await httpClient.PostFormUrlEncodedAsync(authorizeEndpoint!, authForm, ct);
                var authStatus = (int)authRes.StatusCode;
                var authBody = authRes.Body ?? string.Empty;

                // Try to detect a JSON error payload
                string? authError = null;
                try
                {
                    using var authDoc = JsonDocument.Parse(authBody);
                    var root = authDoc.RootElement;
                    if (root.TryGetProperty("error", out var errEl) && errEl.ValueKind == JsonValueKind.String)
                    {
                        authError = errEl.GetString();
                    }
                }
                catch { /* non-JSON body (likely HTML login page) */ }

                bool authorizeOk =
                    authRes.IsSuccessStatusCode ||
                    (authStatus >= 300 && authStatus < 400) ||
                    (authStatus == 400 && !string.Equals(authError, "invalid_request_uri", StringComparison.OrdinalIgnoreCase));

                if (!authorizeOk)
                {
                    return [new(DiagnosticId,
                        Result: $"Authorize check failed\nEndpoint: {authorizeEndpoint}\nHTTP {authStatus}\nBody: {authBody.Truncate(2048)}\nError: {authRes.Error}",
                        Errors: [new("par_authorize_failed", "Authorize endpoint rejected the request_uri (PAR not functional)")]
                    )];
                }

                authorizeCheck = $"OK (HTTP {authStatus})";
            }

            return [new(DiagnosticId, IsSuccess: true,
                Result: $"PAR working\nPAR: {parEndpoint}\nStatus: {statusCode}\nrequest_uri: {requestUriValue}\nexpires_in: {expiresInValue}\nauthorize check: {authorizeCheck}\ncode_verifier: {codeVerifier}\nPAR response: {body.Truncate(2048)}"
            )];
        }
        catch (OperationCanceledException)
        {
            return [new(DiagnosticId, Errors: [new("cancelled", "Operation canceled")])];
        }
        catch (Exception ex)
        {
            return [new(DiagnosticId, Errors: [new("unexpected_error", ex.Message)])];
        }
    }
}