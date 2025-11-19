using MicroM.Diagnostics;
using MicroM.Extensions;
using MicroM.Web.Extensions;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using System.Text.Json;

namespace MicroM.Web.Authentication.OIDCDiagnostics;

internal class ClientIdpRefreshCheck() : IDiagnosticCheck<ClientDiagnosticsContext>
{
    public string DiagnosticId => "oidc_client_token_refresh_check";

    public async Task<List<DiagnosticResult>> RunCheckAsync(ClientDiagnosticsContext ctx, CancellationToken ct)
    {
        var app = ctx.App;
        var httpClient = ctx.HttpClient;

        try
        {
            if (string.IsNullOrWhiteSpace(ctx.tokenURL))
                return [new(DiagnosticId, Errors: [new("token_endpoint_missing", "Discovery is missing token_endpoint")])];

            var tokenEndpoint = ctx.tokenURL;

            // Attempt a refresh grant to inspect expected error semantics and, if present, id_token shape.
            var form = new Dictionary<string, string>
            {
                [WellknownIdentityConstants.GrantType] = WellknownIdentityConstants.RefreshToken,
                [WellknownIdentityConstants.RefreshToken] = "invalid_refresh_token",
                [WellknownIdentityConstants.ClientId] = app.ApplicationID
            };

            var resp = await httpClient.PostTokenAsync(tokenEndpoint, form, authorization: null, ct);
            var status = (int)resp.StatusCode;
            var body = resp.Body ?? string.Empty;

            // Try to parse structured JSON (error or success)
            string? idToken = null;
            string? error = null;
            string? errorDescription = null;

            try
            {
                using var doc = JsonDocument.Parse(body);
                var root = doc.RootElement;
                idToken = root.ReadString(WellknownIdentityConstants.IdToken);
                error = root.ReadString("error");
                errorDescription = root.ReadString("error_description");
            }
            catch
            {
                // Non-JSON body or truncated; keep raw (truncated) body in result.
            }

            // If id_token was unexpectedly returned, analyze whether it's JWS/JWE and report alg/enc
            if (!string.IsNullOrWhiteSpace(idToken))
            {
                var analysis = AnalyzeTokenHeader(idToken!);
                var mode = analysis.isJwe ? "JWE (signed+encrypted)" : "JWS (signed only)";
                var alg = analysis.alg ?? "unknown";
                var enc = analysis.enc ?? (analysis.isJwe ? "unknown" : "n/a");

                return [new(DiagnosticId, IsSuccess: true,
                    Result:
$@"Refresh flow responded with id_token (unexpected in diagnostics request)
Endpoint: {tokenEndpoint}
HTTP: {status}
Mode: {mode}
alg: {alg}
enc: {enc}
Body: {body.Truncate(1024)}"
                )];
            }

            // No id_token: check expected error semantics (400/401/403) and report
            if (resp.IsSuccessStatusCode)
            {
                return [new(DiagnosticId,
                    Result: $"Token endpoint responded without error for invalid refresh_token.\nEndpoint: {tokenEndpoint}\nHTTP {status}\nBody: {body.Truncate(2048)}",
                    Errors: [new("token_unexpected_success", "Expected an error for invalid refresh_token but received success")])];
            }

            if (status is 400 or 401 or 403)
            {
                var reason = string.IsNullOrWhiteSpace(error) ? "OK (expected error semantics)" : $"OK ({error})";
                return [new(DiagnosticId,
                    IsSuccess: true,
                    Result:
$@"Status: {reason}
Endpoint: {tokenEndpoint}
HTTP: {status}
Error: {error ?? "n/a"}
Description: {errorDescription ?? "n/a"}
Body: {body.Truncate(1024)}")];
            }

            return [new(DiagnosticId,
                Result:
$@"Endpoint: {tokenEndpoint}
HTTP: {status}
Body: {body.Truncate(2048)}
Error: {resp.Error}",
                Errors: [new("token_unexpected_status", "Token endpoint returned an unexpected status code")])];
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

    private static (bool isJwe, string? alg, string? enc) AnalyzeTokenHeader(string jwt)
    {
        // Compact serialization:
        // JWS: header.payload.signature (3 parts)
        // JWE: header.encrypted_key.iv.ciphertext.tag (5 parts)
        var parts = jwt.Split('.');
        if (parts.Length < 1) return (false, null, null);

        try
        {
            var headerB64 = parts[0];
            var headerJson = Encoding.UTF8.GetString(Base64UrlEncoder.DecodeBytes(headerB64));
            using var doc = JsonDocument.Parse(headerJson);
            var root = doc.RootElement;

            string? alg = null;
            string? enc = null;

            if (root.TryGetProperty(JwtHeaderParameterNames.Alg, out var algEl) && algEl.ValueKind == JsonValueKind.String)
                alg = algEl.GetString();

            if (root.TryGetProperty(JwtHeaderParameterNames.Enc, out var encEl) && encEl.ValueKind == JsonValueKind.String)
                enc = encEl.GetString();

            var isJwe = parts.Length == 5 || !string.IsNullOrWhiteSpace(enc);
            return (isJwe, alg, enc);
        }
        catch
        {
            return (false, null, null);
        }
    }
}