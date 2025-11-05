using MicroM.Core;
using MicroM.Diagnostics;
using MicroM.Web.Extensions;

namespace MicroM.Web.Authentication.OIDCDiagnostics;

internal sealed class IdpFrontChannelLogoutConfigCheck : IDiagnosticCheck<IdPDiagnosticsContext>
{
    public string DiagnosticId => "oidc_idp_frontchannel_logout_config_check";

    public Task<List<DiagnosticResult>> RunCheckAsync(IdPDiagnosticsContext ctx, CancellationToken ct)
    {
        var app = ctx.App;
        List<DiagnosticResult> results = [];

        if (app.OIDCClientConfiguration == null || app.OIDCClientConfiguration.Count == 0)
        {
            results.Add(new(DiagnosticId, Result: "No OIDC clients configured for this IdP", Errors: [new("no_clients_configured", "OIDCClientConfiguration is empty")]));
            return Task.FromResult(results);
        }

        foreach (var client in app.OIDCClientConfiguration.Values)
        {
            List<ErrorResult> errs = [];

            var clientId = client.ClientAPPID ?? string.Empty;
            var frontUrl = client.URLFrontChannelLogout ?? string.Empty;
            var redirect = (client.URLAuthorizedRedirects != null && client.URLAuthorizedRedirects.Count > 0) ? client.URLAuthorizedRedirects[0] : null;

            if (string.IsNullOrWhiteSpace(clientId))
                errs.Add(new("client_id_missing", "Configured client is missing ClientAPPID"));

            if (string.IsNullOrWhiteSpace(frontUrl))
                errs.Add(new("frontchannel_url_missing", "Front-channel logout URL is missing"));
            else if (!frontUrl.isValidHTTPSUrl())
                errs.Add(new("frontchannel_url_invalid", $"Front-channel logout URL must be HTTPS: {frontUrl}"));

            if (string.IsNullOrWhiteSpace(redirect) || !redirect!.isValidHTTPSUrl())
                errs.Add(new("redirect_invalid", $"At least one HTTPS redirect_uri is required to validate end-session round-trip. Found: {redirect ?? "(null)"}"));

            var summary = $"Client: {clientId}\nFrontChannel: {frontUrl}\nSampleRedirect: {redirect ?? "(null)"}";
            results.Add(new DiagnosticResult(DiagnosticId, IsSuccess: errs.Count == 0, Result: summary, Errors: errs.Count == 0 ? null : errs));
        }

        return Task.FromResult(results);
    }
}