using MicroM.Diagnostics;
using MicroM.Web.Extensions;

namespace MicroM.Web.Authentication.OIDCDiagnostics;

internal sealed class IdpClientJwksCheck : IDiagnosticCheck<IdPDiagnosticsContext>
{
    public string DiagnosticId => "oidc_idp_client_jwks_check";

    public async Task<List<DiagnosticResult>> RunCheckAsync(IdPDiagnosticsContext ctx, CancellationToken ct)
    {
        var app = ctx.App;
        var http = ctx.HttpClient;

        List<DiagnosticResult> results = [];

        if (app.OIDCClientConfiguration == null || app.OIDCClientConfiguration.Count == 0)
        {
            results.Add(new(DiagnosticId, Result: "No OIDC clients configured for this IdP", Errors: [new("no_clients_configured", "OIDCClientConfiguration is empty")]));
            return results;
        }

        foreach (var client in app.OIDCClientConfiguration.Values)
        {
            var clientId = client.ClientAPPID ?? "(unknown)";
            var jwksUrl = client.URLClientJWKS ?? string.Empty;

            if (string.IsNullOrWhiteSpace(jwksUrl))
            {
                results.Add(new(DiagnosticId, Result: $"Client: {clientId}\nJWKS: (missing)", Errors: [new("client_jwks_missing", "Client JWKS URL not configured")]));
                continue;
            }

            if (!jwksUrl.isValidHTTPSUrl())
            {
                results.Add(new(DiagnosticId, Result: $"Client: {clientId}\nJWKS: {jwksUrl}", Errors: [new("jwks_url_invalid", "Client JWKS URL must be HTTPS")]));
                continue;
            }

            try
            {
                var resp = await http.GetJwksJsonAsync(jwksUrl, ct);
                if (!resp.IsSuccessStatusCode || string.IsNullOrWhiteSpace(resp.Body))
                {
                    results.Add(new(DiagnosticId, Result: $"Client: {clientId}\nJWKS: {jwksUrl}", Errors: [new("jwks_http_error", resp.Error ?? "Failed to fetch client JWKS")]));
                    continue;
                }

                using var jwks = System.Text.Json.JsonDocument.Parse(resp.Body);
                var hasKeys = jwks.RootElement.TryGetProperty(WellknownIdentityConstants.Keys, out var keysEl) &&
                              keysEl.ValueKind == System.Text.Json.JsonValueKind.Array &&
                              keysEl.GetArrayLength() > 0;

                if (!hasKeys)
                {
                    results.Add(new(DiagnosticId, Result: $"Client: {clientId}\nJWKS: {jwksUrl}\nBody: {resp.Body}", Errors: [new("jwks_empty", "Client JWKS contains no keys")]));
                    continue;
                }

                // If client has a configured key id, ensure JWKS contains a matching kid
                var configuredKid = client.CertificateUniqueID; // expected in client configuration
                if (!string.IsNullOrWhiteSpace(configuredKid))
                {
                    bool kidFound = false;
                    foreach (var keyEl in keysEl.EnumerateArray())
                    {
                        if (keyEl.ValueKind != System.Text.Json.JsonValueKind.Object) continue;
                        if (keyEl.TryGetProperty("kid", out var kidEl) && kidEl.ValueKind == System.Text.Json.JsonValueKind.String)
                        {
                            var kid = kidEl.GetString();
                            if (!string.IsNullOrWhiteSpace(kid) && string.Equals(kid, configuredKid, StringComparison.Ordinal))
                            {
                                kidFound = true;
                                break;
                            }
                        }
                    }

                    if (!kidFound)
                    {
                        results.Add(new(DiagnosticId, Result: $"Client: {clientId}\nJWKS: {jwksUrl}", Errors: [new("jwks_kid_missing", $"Configured key id not found in client JWKS: kid='{configuredKid}'")]));
                        continue;
                    }
                }
                else
                {
                    // No configured kid, warn if multiple keys are present
                    if (keysEl.GetArrayLength() > 1)
                    {
                        results.Add(new(DiagnosticId, IsSuccess: true, Result: $"Status: OK (client JWKS reachable)\nWarning: Multiple keys present in JWKS but no configured key id to select\nClient: {clientId}\nJWKS: {jwksUrl}"));
                        continue;
                    }
                }

                results.Add(new(DiagnosticId, IsSuccess: true, Result: $"Status: OK (client JWKS reachable)\nClient: {clientId}\nJWKS: {jwksUrl}"));
            }
            catch (OperationCanceledException)
            {
                results.Add(new(DiagnosticId, Result: $"Client: {clientId}\nJWKS: {jwksUrl}", Errors: [new("cancelled", "Operation canceled")]));
            }
            catch (System.Text.Json.JsonException ex)
            {
                results.Add(new(DiagnosticId, Result: $"Client: {clientId}\nJWKS: {jwksUrl}", Errors: [new("json_error", ex.Message)]));
            }
            catch (Exception ex)
            {
                results.Add(new(DiagnosticId, Result: $"Client: {clientId}\nJWKS: {jwksUrl}", Errors: [new("unexpected_error", ex.Message)]));
            }
        }

        return results;
    }
}