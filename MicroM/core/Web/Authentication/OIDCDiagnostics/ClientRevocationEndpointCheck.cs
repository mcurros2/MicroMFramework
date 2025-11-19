using MicroM.Diagnostics;
using MicroM.Extensions;
using MicroM.Web.Extensions;
using System.Text.Json;

namespace MicroM.Web.Authentication.OIDCDiagnostics;

internal class ClientRevocationEndpointCheck() : IDiagnosticCheck<ClientDiagnosticsContext>
{
    public string DiagnosticId => "oidc_client_revocation_endpoint_check";

    public async Task<List<DiagnosticResult>> RunCheckAsync(ClientDiagnosticsContext ctx, CancellationToken ct)
    {
        var app = ctx.App;
        var httpClient = ctx.HttpClient;

        string? wellKnownJson = null;
        string? revocationUrl = null;

        try
        {
            // Unified SKIPPED behavior
            if (string.IsNullOrWhiteSpace(ctx.revocationURL))
                return [new(DiagnosticId, IsSuccess: true, Result: "SKIPPED: revocation_endpoint not advertised")];

            revocationUrl = ctx.revocationURL;

            var form = new Dictionary<string, string>
            {
                ["token"] = "invalid_token",
                ["token_type_hint"] = "refresh_token",
                ["client_id"] = app.ApplicationID
            };

            var resp = await httpClient.PostFormUrlEncodedAsync(revocationUrl, form, ct);
            var status = resp.StatusCode;
            var body = (resp.Body ?? string.Empty).ScrubForDiagnostics();

            if (resp.IsSuccessStatusCode || (int)status is 400 or 401 or 403)
                return [new(DiagnosticId, IsSuccess: true, Result: $"Status: OK (endpoint reachable)\nEndpoint: {revocationUrl}\nHTTP {status}\nBody: {body.Truncate(2048)}\nError: {resp.Error}")];

            return [new(DiagnosticId, Result: $"Endpoint: {revocationUrl}\nHTTP {status}\nBody: {body.Truncate(2048)}\nError: {resp.Error}", Errors: [new("revocation_unexpected_status", "Revocation endpoint returned an unexpected status code")])];
        }
        catch (OperationCanceledException)
        {
            return [new(DiagnosticId, Result: $"Well known: {wellKnownJson}\nRevocation: {revocationUrl}", Errors: [new("cancelled", "Operation canceled")])];
        }
        catch (JsonException ex)
        {
            return [new(DiagnosticId, Result: $"Well known: {wellKnownJson}", Errors: [new("json_error", ex.Message)])];
        }
        catch (Exception ex)
        {
            return [new(DiagnosticId, Result: $"Well known: {wellKnownJson}\nRevocation: {revocationUrl}", Errors: [new("unexpected_error", ex.Message)])];
        }
    }
}