using MicroM.Diagnostics;
using MicroM.Extensions;
using MicroM.Web.Extensions;
using System.Text.Json;

namespace MicroM.Web.Authentication.OIDCDiagnostics;

internal class ClientUserInfoEndpointCheck() : IDiagnosticCheck<ClientDiagnosticsContext>
{
    public string DiagnosticId => "oidc_client_userinfo_endpoint_check";

    public async Task<List<DiagnosticResult>> RunCheckAsync(ClientDiagnosticsContext ctx, CancellationToken ct)
    {
        var app = ctx.App;
        var httpClient = ctx.HttpClient;

        string? wellKnownJson = null;
        string? userInfoUrl = null;

        try
        {
            // Unified SKIPPED behavior
            if (string.IsNullOrWhiteSpace(ctx.userInfoURL))
                return [new(DiagnosticId, IsSuccess: true, Result: "SKIPPED: userinfo_endpoint not advertised")];

            userInfoUrl = ctx.userInfoURL;

            var resp = await httpClient.PostFormUrlEncodedAsync(userInfoUrl, Array.Empty<KeyValuePair<string, string>>(), ct);
            var status = resp.StatusCode;
            var body = (resp.Body ?? string.Empty).ScrubForDiagnostics();

            if (resp.IsSuccessStatusCode || (int)status is 400 or 401 or 403 or 405)
                return [new(DiagnosticId, IsSuccess: true, Result: $"Status: OK (endpoint reachable)\nEndpoint: {userInfoUrl}\nHTTP {status}\nBody: {body.Truncate(2048)}\nError: {resp.Error}")];

            return [new(DiagnosticId, Result: $"Endpoint: {userInfoUrl}\nHTTP {status}\nBody: {body.Truncate(2048)}\nError: {resp.Error}", Errors: [new("userinfo_unexpected_status", "UserInfo endpoint returned an unexpected status code")])];
        }
        catch (OperationCanceledException)
        {
            return [new(DiagnosticId, Result: $"Well known: {wellKnownJson}\nUserInfo: {userInfoUrl}", Errors: [new("cancelled", "Operation canceled")])];
        }
        catch (JsonException ex)
        {
            return [new(DiagnosticId, Result: $"Well known: {wellKnownJson}", Errors: [new("json_error", ex.Message)])];
        }
        catch (Exception ex)
        {
            return [new(DiagnosticId, Result: $"Well known: {wellKnownJson}\nUserInfo: {userInfoUrl}", Errors: [new("unexpected_error", ex.Message)])];
        }
    }
}