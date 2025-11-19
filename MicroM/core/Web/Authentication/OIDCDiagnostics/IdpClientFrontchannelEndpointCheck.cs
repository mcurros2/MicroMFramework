using MicroM.Diagnostics;
using MicroM.Extensions;
using MicroM.Web.Extensions;
using System.Diagnostics;

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
                results.Add(new(DiagnosticId, Result:
$@"client_id: {clientId}
frontchannel_url: (missing)", Errors: [new("frontchannel_url_missing", "Client front-channel logout URL not configured")]));
                continue;
            }

            if (!url.isValidHTTPSUrl())
            {
                results.Add(new(DiagnosticId, Result:
$@"client_id: {clientId}
frontchannel_url: {url}", Errors: [new("frontchannel_url_invalid", "Front-channel logout URL must be HTTPS")]));
                continue;
            }

            try
            {
                var sw = Stopwatch.StartNew();
                var resp = await http.PostFormUrlEncodedAsync(url, Array.Empty<KeyValuePair<string, string>>(), ct);
                sw.Stop();

                var status = (int)resp.StatusCode;
                var body = (resp.Body ?? string.Empty).ScrubForDiagnostics();
                var reachable = resp.IsSuccessStatusCode || status is 302 or 400 or 401 or 403 or 405;

                var summary =
$@"{(reachable ? "Status: OK (endpoint reachable)" : "Status: NOT OK")}
client_id: {clientId}
frontchannel_url: {url}
http_status: {status}
duration_ms: {sw.ElapsedMilliseconds}
error: {(resp.Error ?? "n/a")}
body_len: {body.Length}";

                if (reachable)
                {
                    results.Add(new(DiagnosticId, IsSuccess: true, Result: summary + $"\nBody: {body.Truncate(2048)}"));
                }
                else
                {
                    results.Add(new(DiagnosticId, Result: summary + $"\nBody: {body.Truncate(2048)}", Errors: [new("frontchannel_unexpected_status", "Front-channel endpoint returned an unexpected status code")]));
                }
            }
            catch (OperationCanceledException)
            {
                results.Add(new(DiagnosticId, Result:
$@"client_id: {clientId}
frontchannel_url: {url}", Errors: [new("cancelled", "Operation canceled")]));
            }
            catch (Exception ex)
            {
                results.Add(new(DiagnosticId, Result:
$@"client_id: {clientId}
frontchannel_url: {url}", Errors: [new("unexpected_error", ex.Message)]));
            }
        }

        return results;
    }
}