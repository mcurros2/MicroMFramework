using MicroM.Diagnostics;
using MicroM.Web.Authentication.OIDCDiagnostics;
using MicroM.Web.Services;

namespace MicroM.Web.Authentication.SSO;

/// <summary>
/// Provides diagnostic methods for testing OpenID Connect (OIDC) identity provider configuration for the IDPServer role.
/// </summary>
/// <remarks>
/// Use this class to test and validate the configuration of the IdPServer role in an OIDC setup.
/// It will test no IdP configuration is missing.
/// It will test each client APP configured endpoint for the app_id IDPServer provided.
/// </remarks>
public class OIDCIdPDiagnostics(
    IMicroMAppConfiguration appConfig,
    IOIDCHttpClient httpClient
) : IDiagnostics<string>
{
    private readonly IdpConfigurationCheck ConfigCheck = new();
    private readonly IdpClientRegistrationSanityCheck ClientRegSanity = new();

    // New endpoint reachability checks per configured client
    private readonly IdpClientJwksCheck ClientJwksCheck = new();
    private readonly IdpClientBackchannelEndpointCheck ClientBackchannelCheck = new();
    private readonly IdpClientFrontchannelEndpointCheck ClientFrontchannelCheck = new();
    private readonly IdpClientRedirectUrisCheck ClientRedirectsCheck = new();

    public async Task<Dictionary<string, List<DiagnosticResult>>> RunAllDiagnosticsAsync(string app_id, CancellationToken ct)
    {
        Dictionary<string, List<DiagnosticResult>> results = [];
        var app = appConfig.GetAppConfiguration(app_id);

        const string config_check = "oidc_idp_configuration_check";

        if (app == null)
        {
            results[config_check] = [new(config_check, Errors: [new("app_not_found", "Application configuration not found")])];
            return results;
        }

        var ctx = new IdPDiagnosticsContext(app, httpClient);

        // 1) IdP configuration validation
        var cfg = await ConfigCheck.RunCheckAsync(ctx, ct);
        results[ConfigCheck.DiagnosticId] = cfg;

        if (!cfg.isSuccess()) return results;

        // 2) Client registration sanity (HTTPS, required fields, redirects format)
        var clientReg = await ClientRegSanity.RunCheckAsync(ctx, ct);
        results[ClientRegSanity.DiagnosticId] = clientReg;

        if (!clientReg.isSuccess()) return results;

        // 3) Client endpoints reachability and expected behavior
        results[ClientJwksCheck.DiagnosticId] = await ClientJwksCheck.RunCheckAsync(ctx, ct);
        results[ClientBackchannelCheck.DiagnosticId] = await ClientBackchannelCheck.RunCheckAsync(ctx, ct);
        results[ClientFrontchannelCheck.DiagnosticId] = await ClientFrontchannelCheck.RunCheckAsync(ctx, ct);
        results[ClientRedirectsCheck.DiagnosticId] = await ClientRedirectsCheck.RunCheckAsync(ctx, ct);

        return results;
    }
}

