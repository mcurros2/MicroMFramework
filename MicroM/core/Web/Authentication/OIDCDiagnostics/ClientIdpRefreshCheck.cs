using MicroM.Diagnostics;
using MicroM.Extensions;
using System.Text.Json;

namespace MicroM.Web.Authentication.OIDCDiagnostics;

internal class ClientIdpRefreshCheck() : IDiagnosticCheck<ClientDiagnosticsContext>
{
    public string DiagnosticId => "oidc_client_token_refresh_check";

    public async Task<List<DiagnosticResult>> RunCheckAsync(ClientDiagnosticsContext ctx, CancellationToken ct)
    {
        var app = ctx.App;
        var httpClient = ctx.HttpClient;

        string? wellKnownJson = null;
        string? tokenEndpoint = null;

        try
        {

            if (string.IsNullOrWhiteSpace(ctx.tokenURL))
                return [new(DiagnosticId, Errors: [new("token_endpoint_missing", "Discovery is missing token_endpoint")])];

            tokenEndpoint = ctx.tokenURL;

            var form = new Dictionary<string, string>
            {
                ["grant_type"] = "refresh_token",
                ["refresh_token"] = "invalid_refresh_token",
                ["client_id"] = app.ApplicationID
            };

            var resp = await httpClient.PostTokenAsync(tokenEndpoint, form, authorization: null, ct);
            var status = resp.StatusCode;
            var body = resp.Body ?? string.Empty;

            if (resp.IsSuccessStatusCode)
            {
                return [new(DiagnosticId,
                    Result: $"Token endpoint responded without error for invalid refresh_token.\nEndpoint: {tokenEndpoint}\nHTTP {status}\nBody: {body.Truncate(2048)}",
                    Errors: [new("token_unexpected_success", "Expected an error for invalid refresh_token but received success")])];
            }

            if ((int)status is 400 or 401 or 403)
            {
                return [new(DiagnosticId,
                    IsSuccess: true,
                    Result: $"Status: OK (expected error semantics)\nEndpoint: {tokenEndpoint}\nHTTP {status}\nBody: {body.Truncate(2048)}\nError: {resp.Error}")];
            }

            return [new(DiagnosticId,
                Result: $"Endpoint: {tokenEndpoint}\nHTTP {status}\nBody: {body.Truncate(2048)}\nError: {resp.Error}",
                Errors: [new("token_unexpected_status", "Token endpoint returned an unexpected status code")])];
        }
        catch (OperationCanceledException)
        {
            return [new(DiagnosticId, Result: $"Well known: {wellKnownJson}\nToken: {tokenEndpoint}", Errors: [new("cancelled", "Operation canceled")])];
        }
        catch (JsonException ex)
        {
            return [new(DiagnosticId, Result: $"Well known: {wellKnownJson}", Errors: [new("json_error", ex.Message)])];
        }
        catch (Exception ex)
        {
            return [new(DiagnosticId, Result: $"Well known: {wellKnownJson}\nToken: {tokenEndpoint}", Errors: [new("unexpected_error", ex.Message)])];
        }
    }
}