using MicroM.Diagnostics;
using MicroM.Extensions;
using MicroM.Web.Extensions;
using System.Text.Json;

namespace MicroM.Web.Authentication.OIDCDiagnostics;

internal class ClientEndSessionEndpointCheck : IDiagnosticCheck<ClientDiagnosticsContext>
{
    public string DiagnosticId => "oidc_client_end_session_endpoint_check";

    public async Task<List<DiagnosticResult>> RunCheckAsync(ClientDiagnosticsContext ctx, CancellationToken ct)
    {
        var app = ctx.App;
        var httpClient = ctx.HttpClient;

        string? wellKnownJson = null;
        string? endSessionUrl = null;

        try
        {
            // Unify skip behavior: if not advertised, return SKIPPED (not an error)
            if (string.IsNullOrWhiteSpace(ctx.endSessionURL))
                return [new(DiagnosticId, IsSuccess: true, Result: "SKIPPED: end_session_endpoint not advertised")];

            endSessionUrl = ctx.endSessionURL;

            var resp = await httpClient.PostFormUrlEncodedAsync(endSessionUrl, Array.Empty<KeyValuePair<string, string>>(), ct);
            var status = resp.StatusCode;
            var body = (resp.Body ?? string.Empty).ScrubForDiagnostics();

            if (resp.IsSuccessStatusCode || (int)status is 302 or 400 or 401 or 403 or 405)
                return [new(DiagnosticId, Result: $"Status: OK (endpoint reachable)\nEndpoint: {endSessionUrl}\nHTTP {status}\nBody: {body.Truncate(2048)}\nError: {resp.Error}")];

            return [new(DiagnosticId, Result: $"Endpoint: {endSessionUrl}\nHTTP {status}\nBody: {body.Truncate(2048)}\nError: {resp.Error}", Errors: [new("end_session_unexpected_status", "End session endpoint returned an unexpected status code")])];
        }
        catch (OperationCanceledException)
        {
            return [new(DiagnosticId, Result: $"Well known: {wellKnownJson}\nEndSession: {endSessionUrl}", Errors: [new("cancelled", "Operation canceled")])];
        }
        catch (JsonException ex)
        {
            return [new(DiagnosticId, Result: $"Well known: {wellKnownJson}", Errors: [new("json_error", ex.Message)])];
        }
        catch (Exception ex)
        {
            return [new(DiagnosticId, Result: $"Well known: {wellKnownJson}\nEndSession: {endSessionUrl}", Errors: [new("unexpected_error", ex.Message)])];
        }
    }
}