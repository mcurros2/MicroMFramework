using MicroM.Core;
using MicroM.Diagnostics;
using MicroM.Web.Extensions;

namespace MicroM.Web.Authentication.OIDCDiagnostics;

internal sealed class IdpClientRegistrationSanityCheck : IDiagnosticCheck<IdPDiagnosticsContext>
{
    public string DiagnosticId => "oidc_idp_client_registration_sanity";

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
            var backchannel = client.URLBackchannelLogout ?? string.Empty;
            var frontchannel = client.URLFrontChannelLogout ?? string.Empty;
            var jwks = client.URLClientJWKS ?? string.Empty;
            var redirects = client.URLAuthorizedRedirects;

            if (string.IsNullOrWhiteSpace(clientId))
                errs.Add(new("client_id_missing", "Configured client is missing ClientAPPID"));

            if (!backchannel.isValidHTTPSUrl())
                errs.Add(new("backchannel_url_invalid", $"Backchannel logout URL must be HTTPS: {backchannel}"));

            if (!string.IsNullOrWhiteSpace(jwks) && !jwks.isValidHTTPSUrl())
                errs.Add(new("jwks_url_invalid", $"Client JWKS URL must be HTTPS: {jwks}"));

            if (!string.IsNullOrWhiteSpace(frontchannel) && !frontchannel.isValidHTTPSUrl())
                errs.Add(new("frontchannel_url_invalid", $"Front-channel logout URL must be HTTPS: {frontchannel}"));

            if (redirects == null || redirects.Count == 0)
            {
                errs.Add(new("redirects_missing", "No authorized redirect URIs configured"));
            }
            else
            {
                foreach (var r in redirects)
                {
                    if (!r.isValidHTTPSUrl())
                        errs.Add(new("redirect_invalid", $"Redirect URI must be HTTPS: {r}"));
                }
            }

            var summary = $"Client: {clientId}\nBackchannel: {backchannel}\nFrontChannel: {frontchannel}\nJWKS: {jwks}\nRedirects: {(redirects == null ? "(null)" : string.Join(",", redirects))}";
            results.Add(new DiagnosticResult(DiagnosticId, IsSuccess: errs.Count == 0, Result: summary, Errors: errs.Count == 0 ? null : errs));
        }

        return Task.FromResult(results);
    }
}