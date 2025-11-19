using MicroM.Configuration;
using MicroM.Core;
using MicroM.Diagnostics;
using MicroM.Extensions;
using MicroM.Web.Authentication.SSO;
using MicroM.Web.Extensions;
using Microsoft.IdentityModel.Tokens;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text.Json;

namespace MicroM.Web.Authentication.OIDCDiagnostics;

internal class ClientPARCheck() : IDiagnosticCheck<ClientDiagnosticsContext>
{
    public string DiagnosticId => "oidc_client_par_check";

    private static (Dictionary<string, string> form, string codeVerifier, string state, string nonce) BuildParForm(
        ApplicationOption app,
        IEnumerable<string> scopes,
        string redirectUri,
        CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        // PKCE
        var codeVerifier = CryptClass.GenerateBase64UrlRandomCode(32);
        string codeChallenge;
        codeChallenge = Base64UrlEncoder.Encode(SHA256.HashData(System.Text.Encoding.ASCII.GetBytes(codeVerifier)));

        // Ensure base64url for state/nonce (diagnostics will report format, not values)
        var state = CryptClass.GenerateBase64UrlRandomCode(32);
        var nonce = CryptClass.GenerateBase64UrlRandomCode(32);

        var form = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            [WellknownIdentityConstants.ResponseType] = WellknownIdentityConstants.Code,
            [WellknownIdentityConstants.ClientId] = app.ApplicationID,
            [WellknownIdentityConstants.RedirectUri] = redirectUri,
            [WellknownIdentityConstants.Scope] = string.Join(' ', scopes),
            [WellknownIdentityConstants.CodeChallenge] = codeChallenge,
            [WellknownIdentityConstants.CodeChallengeMethod] = nameof(OIDCCodeChallengeMethod.S256),
            [WellknownIdentityConstants.State] = state,
            [WellknownIdentityConstants.Nonce] = nonce
        };

        // Note: client_assertion (private_key_jwt) is intentionally NOT added here.
        // It will be selected and added in RunCheckAsync so we can report the chosen alg.

        return (form, codeVerifier, state, nonce);
    }

    public async Task<List<DiagnosticResult>> RunCheckAsync(ClientDiagnosticsContext ctx, CancellationToken ct)
    {
        var app = ctx.App;
        var httpClient = ctx.HttpClient;

        if (string.IsNullOrWhiteSpace(ctx.parURL))
        {
            return [new(DiagnosticId, IsSuccess: true, Result: "SKIPPED: PAR endpoint not advertised")];
        }

        string parEndpoint = ctx.parURL;
        string? authorizeEndpoint = ctx.authorizeURL;

        try
        {
            // Choose a registered redirect_uri
            var redirectUri = app.FrontendURLS?.FirstOrDefault(u => !string.IsNullOrWhiteSpace(u));
            if (string.IsNullOrWhiteSpace(redirectUri))
                return [new(DiagnosticId, Errors: [new("redirect_uri_missing", "No configured redirect_uri found to perform PAR")])];

            var (form, codeVerifier, state, nonce) = BuildParForm(
                app,
                scopes: [WellknownIdentityConstants.OpenID],
                redirectUri: redirectUri,
                ct);

            // Attempt private_key_jwt assertion if certificate present; use algs from context (populated from discovery)
            string clientAssertionStatus = "none";
            string? chosenAlg = null;

            if (app.OIDCCertificateBlob is { Length: > 0 } && !string.IsNullOrWhiteSpace(app.OIDCCertificatePassword))
            {
                using var cert = new X509Certificate2(app.OIDCCertificateBlob, app.OIDCCertificatePassword);

                var selectedAlg = PushedAuthorizationProvider.SelectAssertionAlg(cert, ctx.tokenEndpointAuthSigningAlgs);
                chosenAlg = selectedAlg?.ToString() ?? "default";
                var assertion = PushedAuthorizationProvider.BuildClientAssertion(cert, app.ApplicationID, parEndpoint, selectedAlg);

                form[WellknownIdentityConstants.ClientAssertionType] = WellknownIdentityConstants.ClientAssertionTypeJwtBearer;
                form[WellknownIdentityConstants.ClientAssertion] = assertion;
                clientAssertionStatus = "private_key_jwt";
            }

            // 1) Send PAR
            var parRes = await httpClient.PostPushedAuthorizationRequestAsync(parEndpoint, form, authorization: null, ct);
            var statusCode = (int)parRes.StatusCode;
            var body = (parRes.Body ?? string.Empty).ScrubForDiagnostics();

            if (!parRes.IsSuccessStatusCode)
            {
                return [new(DiagnosticId,
                    Result: $"PAR failed\nEndpoint: {parEndpoint}\nHTTP {statusCode}\nBody: {body.Truncate(2048)}\nError: {parRes.Error}\nclient_assertion: {clientAssertionStatus}\nclient_assertion_alg: {chosenAlg ?? "n/a"}",
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

            // 2) Verify the request_uri is accepted by the authorize endpoint (non-interactive check)
            string authorizeCheck = "skipped (authorize endpoint not advertised)";
            if (!string.IsNullOrWhiteSpace(authorizeEndpoint))
            {
                var authForm = new Dictionary<string, string>(StringComparer.Ordinal)
                {
                    [WellknownIdentityConstants.ClientId] = app.ApplicationID,
                    [WellknownIdentityConstants.RequestUri] = requestUriValue!
                };

                var authRes = await httpClient.PostFormUrlEncodedAsync(authorizeEndpoint, authForm, ct);
                var authStatus = (int)authRes.StatusCode;
                var authBody = (authRes.Body ?? string.Empty).ScrubForDiagnostics();

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
                        Result: $"Authorize check failed\nEndpoint: {authorizeEndpoint}\nHTTP {authStatus}\nBody: {authBody.Truncate(2048)}\nError: {authRes.Error}\nclient_assertion: {clientAssertionStatus}\nclient_assertion_alg: {chosenAlg ?? "n/a"}",
                        Errors: [new("par_authorize_failed", "Authorize endpoint rejected the request_uri (PAR not functional)")]
                    )];
                }

                authorizeCheck = $"OK (HTTP {authStatus})";
            }

            // Base64URL format reporting for state/nonce (no raw values logged)
            bool IsBase64Url(string s)
            {
                if (string.IsNullOrEmpty(s)) return false;
                foreach (var ch in s)
                {
                    if (!(char.IsLetterOrDigit(ch) || ch is '-' or '_')) return false;
                }
                // base64url should not include padding
                return !s.Contains('=');
            }

            var stateFmt = IsBase64Url(state) ? $"base64url(len={state.Length})" : $"invalid(len={state.Length})";
            var nonceFmt = IsBase64Url(nonce) ? $"base64url(len={nonce.Length})" : $"invalid(len={nonce.Length})";

            return [new(DiagnosticId, IsSuccess: true,
                Result:
$@"PAR working
PAR: {parEndpoint}
Status: {statusCode}
request_uri: {requestUriValue}
expires_in: {expiresInValue}
authorize check: {authorizeCheck}
client_assertion: {clientAssertionStatus}
client_assertion_alg: {chosenAlg ?? "n/a"}
state_format: {stateFmt}
nonce_format: {nonceFmt}
PAR response: {body.Truncate(2048)}"
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