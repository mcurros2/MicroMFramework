using MicroM.Diagnostics;
using MicroM.Extensions;
using MicroM.Web.Extensions;

namespace MicroM.Web.Authentication.OIDCDiagnostics;

internal sealed class IdpClientBackchannelEndpointCheck : IDiagnosticCheck<IdPDiagnosticsContext>
{
    public string DiagnosticId => "oidc_idp_client_backchannel_endpoint_check";

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
            var url = client.URLBackchannelLogout ?? string.Empty;

            if (string.IsNullOrWhiteSpace(url))
            {
                results.Add(new(DiagnosticId, Result: $"Client: {clientId}\nBackchannel: (missing)", Errors: [new("backchannel_url_missing", "Client backchannel logout URL not configured")]));
                continue;
            }

            if (!url.isValidHTTPSUrl())
            {
                results.Add(new(DiagnosticId, Result: $"Client: {clientId}\nBackchannel: {url}", Errors: [new("backchannel_url_invalid", "Backchannel logout URL must be HTTPS")]));
                continue;
            }

            try
            {
                var resp = await http.PostFormUrlEncodedAsync(url, Array.Empty<KeyValuePair<string, string>>(), ct);
                var status = (int)resp.StatusCode;
                var body = resp.Body ?? string.Empty;

                if (resp.IsSuccessStatusCode || status is 400 or 401 or 403 or 405)
                    results.Add(new(DiagnosticId, IsSuccess: true, Result: $"Status: OK (endpoint reachable)\nClient: {clientId}\nBackchannel: {url}\nHTTP {status}\nBody: {body.Truncate(2048)}\nError: {resp.Error}"));
                else
                    results.Add(new(DiagnosticId, Result: $"Client: {clientId}\nBackchannel: {url}\nHTTP {status}\nBody: {body.Truncate(2048)}\nError: {resp.Error}", Errors: [new("backchannel_unexpected_status", "Backchannel endpoint returned an unexpected status code")]));
            }
            catch (OperationCanceledException)
            {
                results.Add(new(DiagnosticId, Result: $"Client: {clientId}\nBackchannel: {url}", Errors: [new("cancelled", "Operation canceled")]));
            }
            catch (Exception ex)
            {
                results.Add(new(DiagnosticId, Result: $"Client: {clientId}\nBackchannel: {url}", Errors: [new("unexpected_error", ex.Message)]));
            }
        }

        return results;
    }
}