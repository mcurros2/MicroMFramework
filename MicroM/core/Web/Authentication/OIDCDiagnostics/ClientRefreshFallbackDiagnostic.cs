using MicroM.Diagnostics;
using MicroM.Extensions;
using MicroM.Web.Extensions;
using System.Text.Json;

namespace MicroM.Web.Authentication.OIDCDiagnostics;

internal class ClientRefreshFallbackDiagnostic() : IDiagnosticCheck<ClientDiagnosticsContext>
{
    public string DiagnosticId => "oidc_client_refresh_fallback_diagnostic";

    public async Task<List<DiagnosticResult>> RunCheckAsync(ClientDiagnosticsContext ctx, CancellationToken ct)
    {
        var app = ctx.App;
        var http = ctx.HttpClient;

        try
        {
            if (string.IsNullOrWhiteSpace(ctx.tokenURL))
                return [new(DiagnosticId, IsSuccess: true, Result: "SKIPPED: token_endpoint not advertised")];

            // Simulate local refresh failure with an invalid refresh_token.
            // The expected behavior is a 400/401/403 from the IdP token endpoint.
            var form = new Dictionary<string, string>
            {
                [WellknownIdentityConstants.GrantType] = WellknownIdentityConstants.RefreshToken,
                [WellknownIdentityConstants.RefreshToken] = "invalid_refresh_token",
                [WellknownIdentityConstants.ClientId] = app.ApplicationID
            };

            var resp = await http.PostTokenAsync(ctx.tokenURL!, form, authorization: null, ct);
            var status = (int)resp.StatusCode;
            var body = resp.Body ?? string.Empty;

            // Try to capture structured error for the summary
            string? error = null;
            string? errorDescription = null;
            try
            {
                using var doc = JsonDocument.Parse(body);
                var root = doc.RootElement;
                error = root.ReadString("error");
                errorDescription = root.ReadString("error_description");
            }
            catch
            {
                // Non-JSON body; keep raw truncated body below
            }

            if (resp.IsSuccessStatusCode)
            {
                return [new(DiagnosticId,
                    Result:
$@"Refresh fallback check
Endpoint: {ctx.tokenURL}
HTTP: {status}
Body: {body.Truncate(1024)}
Error: {resp.Error}",
                    Errors: [new("refresh_fallback_unexpected_success", "Expected error for invalid refresh_token to validate fallback path")])];
            }

            if (status is 400 or 401 or 403)
            {
                // This indicates the IdP enforces proper validation on refresh_token, which is a prerequisite
                // for the client’s refresh-fallback path (when local refresh fails).
                var reason = string.IsNullOrWhiteSpace(error) ? "OK (expected error semantics)" : $"OK ({error})";
                return [new(DiagnosticId, IsSuccess: true,
                    Result:
$@"Refresh fallback check
Status: {reason}
Endpoint: {ctx.tokenURL}
HTTP: {status}
Error: {error ?? "n/a"}
Description: {errorDescription ?? "n/a"}
Body: {body.Truncate(512)}")];
            }

            return [new(DiagnosticId,
                Result:
$@"Refresh fallback check
Endpoint: {ctx.tokenURL}
HTTP: {status}
Body: {body.Truncate(1024)}
Error: {resp.Error}",
                Errors: [new("refresh_fallback_unexpected_status", "Token endpoint returned an unexpected status code for invalid refresh_token")])];
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