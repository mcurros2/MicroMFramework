using MicroM.Diagnostics;
using MicroM.Extensions;
using MicroM.Web.Extensions;

namespace MicroM.Web.Authentication.OIDCDiagnostics;

internal sealed class IdpClientFrontchannelEndpointCheck : IDiagnosticCheck<IdPDiagnosticsContext>
{
    public string DiagnosticId => "oidc_idp_client_frontchannel_endpoint_check";

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
            var url = client.URLFrontChannelLogout ?? string.Empty;

            if (string.IsNullOrWhiteSpace(url))
            {
                results.Add(new(DiagnosticId, Result: $"Client: {clientId}\nFrontchannel: (missing)", Errors: [new("frontchannel_url_missing", "Client front-channel logout URL not configured")]));
                continue;
            }

            if (!url.isValidHTTPSUrl())
            {
                results.Add(new(DiagnosticId, Result: $"Client: {clientId}\nFrontchannel: {url}", Errors: [new("frontchannel_url_invalid", "Front-channel logout URL must be HTTPS")]));
                continue;
            }

            try
            {
                var resp = await http.PostFormUrlEncodedAsync(url, Array.Empty<KeyValuePair<string, string>>(), ct);
                var status = (int)resp.StatusCode;
                var body = resp.Body ?? string.Empty;

                if (resp.IsSuccessStatusCode || status is 302 or 400 or 401 or 403 or 405)
                    results.Add(new(DiagnosticId, IsSuccess: true, Result: $"Status: OK (endpoint reachable)\nClient: {clientId}\nFrontchannel: {url}\nHTTP {status}\nBody: {body.Truncate(2048)}\nError: {resp.Error}"));
                else
                    results.Add(new(DiagnosticId, Result: $"Client: {clientId}\nFrontchannel: {url}\nHTTP {status}\nBody: {body.Truncate(2048)}\nError: {resp.Error}", Errors: [new("frontchannel_unexpected_status", "Front-channel endpoint returned an unexpected status code")]));
            }
            catch (OperationCanceledException)
            {
                results.Add(new(DiagnosticId, Result: $"Client: {clientId}\nFrontchannel: {url}", Errors: [new("cancelled", "Operation canceled")]));
            }
            catch (Exception ex)
            {
                results.Add(new(DiagnosticId, Result: $"Client: {clientId}\nFrontchannel: {url}", Errors: [new("unexpected_error", ex.Message)]));
            }
        }

        return results;
    }
}