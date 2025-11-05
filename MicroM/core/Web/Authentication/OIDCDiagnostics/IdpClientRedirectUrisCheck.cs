using MicroM.Diagnostics;
using MicroM.Extensions;
using MicroM.Web.Extensions;

namespace MicroM.Web.Authentication.OIDCDiagnostics;

internal sealed class IdpClientRedirectUrisCheck : IDiagnosticCheck<IdPDiagnosticsContext>
{
    public string DiagnosticId => "oidc_idp_client_redirect_uris_check";

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
            var redirects = client.URLAuthorizedRedirects;

            if (redirects == null || redirects.Count == 0)
            {
                results.Add(new(DiagnosticId, Result: $"Client: {clientId}\nRedirects: (none)", Errors: [new("redirects_missing", "No authorized redirect URIs configured")]));
                continue;
            }

            foreach (var redirect in redirects)
            {
                if (string.IsNullOrWhiteSpace(redirect) || !redirect.isValidHTTPSUrl())
                {
                    results.Add(new(DiagnosticId, Result: $"Client: {clientId}\nRedirect: {redirect ?? "(null)"}", Errors: [new("redirect_invalid", "Redirect URI must be HTTPS")]));
                    continue;
                }

                try
                {
                    // Probe with POST empty form; many redirect endpoints are GET-only, so 405 is acceptable.
                    var resp = await http.PostFormUrlEncodedAsync(redirect, Array.Empty<KeyValuePair<string, string>>(), ct);
                    var status = (int)resp.StatusCode;
                    var body = resp.Body ?? string.Empty;

                    if (resp.IsSuccessStatusCode || status is 302 or 400 or 401 or 403 or 405)
                        results.Add(new(DiagnosticId, IsSuccess: true, Result: $"Status: OK (redirect reachable)\nClient: {clientId}\nRedirect: {redirect}\nHTTP {status}\nBody: {body.Truncate(2048)}\nError: {resp.Error}"));
                    else
                        results.Add(new(DiagnosticId, Result: $"Client: {clientId}\nRedirect: {redirect}\nHTTP {status}\nBody: {body.Truncate(2048)}\nError: {resp.Error}", Errors: [new("redirect_unexpected_status", "Redirect URI returned an unexpected status code")]));
                }
                catch (OperationCanceledException)
                {
                    results.Add(new(DiagnosticId, Result: $"Client: {clientId}\nRedirect: {redirect}", Errors: [new("cancelled", "Operation canceled")]));
                }
                catch (Exception ex)
                {
                    results.Add(new(DiagnosticId, Result: $"Client: {clientId}\nRedirect: {redirect}", Errors: [new("unexpected_error", ex.Message)]));
                }
            }
        }

        return results;
    }
}