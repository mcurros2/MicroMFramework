using MicroM.Diagnostics;
using MicroM.Extensions;
using System.Net.Http.Headers;
using System.Text.Json;

namespace MicroM.Web.Authentication.OIDCDiagnostics;

internal class ClientPARCheck() : IDiagnosticCheck<ClientDiagnosticsContext>
{
    public string DiagnosticId => "oidc_client_par_check";

    public async Task<List<DiagnosticResult>> RunCheckAsync(ClientDiagnosticsContext ctx, CancellationToken ct)
    {

        var app = ctx.App;
        var httpClient = ctx.HttpClient;

        string? wellKnownJson = null;
        string? parEndpoint = null;

        try
        {

            if (string.IsNullOrWhiteSpace(ctx.parURL))
                return [new(DiagnosticId, Errors: [new("par_url_empty", "PAR endpoint not advertised in discovery; skipping test.")])];

            parEndpoint = ctx.parURL;

            var form = new Dictionary<string, string>
            {
                ["response_type"] = "code",
                ["client_id"] = app.ApplicationID,
                ["scope"] = "openid"
            };

            var parRes = await httpClient.PostPushedAuthorizationRequestAsync(parEndpoint, form, authorization: (AuthenticationHeaderValue?)null, ct);
            var statusCode = (int)parRes.StatusCode;
            var body = parRes.Body ?? string.Empty;

            if (parRes.IsSuccessStatusCode)
                return [new(DiagnosticId, IsSuccess: true, Result: $"Status: OK (PAR reachable)\nEndpoint: {parEndpoint}\nResponse: {body.Truncate(2048)}")];

            if (statusCode is 400 or 401 or 403)
                return [new(DiagnosticId, IsSuccess: true, Result: $"Status: OK (expected error semantics)\nEndpoint: {parEndpoint}\nHTTP {statusCode}\nBody: {body.Truncate(2048)}\nError: {parRes.Error}")];

            return [new(DiagnosticId, Result: $"Endpoint: {parEndpoint}\nHTTP {statusCode}\nBody: {body.Truncate(2048)}\nError: {parRes.Error}", Errors: [new("par_unexpected_status", "PAR endpoint returned an unexpected status code")])];
        }
        catch (OperationCanceledException)
        {
            return [new(DiagnosticId, Result: $"Well known: {wellKnownJson}\nPAR: {parEndpoint}", Errors: [new("cancelled", "Operation canceled")])];
        }
        catch (JsonException ex)
        {
            return [new(DiagnosticId, Result: $"Well known: {wellKnownJson}", Errors: [new("json_error", ex.Message)])];
        }
        catch (Exception ex)
        {
            return [new(DiagnosticId, Result: $"Well known: {wellKnownJson}\nPAR: {parEndpoint}", Errors: [new("unexpected_error", ex.Message)])];
        }
    }
}