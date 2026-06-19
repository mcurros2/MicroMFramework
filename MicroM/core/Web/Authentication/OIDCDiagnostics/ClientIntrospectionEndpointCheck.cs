using MicroM.Diagnostics;
using MicroM.Extensions;
using MicroM.Web.Extensions;
using System.Text.Json;

namespace MicroM.Web.Authentication.OIDCDiagnostics;

internal class ClientIntrospectionEndpointCheck : IDiagnosticCheck<ClientDiagnosticsContext>
{
    public string DiagnosticId => "oidc_client_introspection_endpoint_check";

    public async Task<List<DiagnosticResult>> RunCheckAsync(ClientDiagnosticsContext ctx, CancellationToken ct)
    {
        var app = ctx.App;
        var httpClient = ctx.HttpClient;

        string? wellKnownJson = null;
        string? introspectionUrl = null;

        try
        {
            // Unified SKIPPED behavior
            if (string.IsNullOrWhiteSpace(ctx.introspectionURL))
                return [new(DiagnosticId, IsSuccess: true, Result: "SKIPPED: introspection_endpoint not advertised")];

            introspectionUrl = ctx.introspectionURL;

            var form = new Dictionary<string, string>
            {
                ["token"] = "invalid_token",
                ["client_id"] = app.ApplicationID
            };

            var resp = await httpClient.PostFormUrlEncodedAsync(introspectionUrl, form, ct);
            var status = resp.StatusCode;
            var body = (resp.Body ?? string.Empty).ScrubForDiagnostics();

            if (resp.IsSuccessStatusCode || (int)status is 400 or 401 or 403)
                return [new(DiagnosticId, Result: $"Status: OK (endpoint reachable)\nEndpoint: {introspectionUrl}\nHTTP {status}\nBody: {body.Truncate(2048)}\nError: {resp.Error}")];

            return [new(DiagnosticId, Result: $"Endpoint: {introspectionUrl}\nHTTP {status}\nBody: {body.Truncate(2048)}\nError: {resp.Error}", Errors: [new("introspection_unexpected_status", "Introspection endpoint returned an unexpected status code")])];
        }
        catch (OperationCanceledException)
        {
            return [new(DiagnosticId, Result: $"Well known: {wellKnownJson}\nIntrospection: {introspectionUrl}", Errors: [new("cancelled", "Operation canceled")])];
        }
        catch (JsonException ex)
        {
            return [new(DiagnosticId, Result: $"Well known: {wellKnownJson}", Errors: [new("json_error", ex.Message)])];
        }
        catch (Exception ex)
        {
            return [new(DiagnosticId, Result: $"Well known: {wellKnownJson}\nIntrospection: {introspectionUrl}", Errors: [new("unexpected_error", ex.Message)])];
        }
    }
}