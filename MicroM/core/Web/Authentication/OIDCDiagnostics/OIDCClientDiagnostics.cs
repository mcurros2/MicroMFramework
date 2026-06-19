using MicroM.Configuration.CategoriesDefinitions;
using MicroM.Diagnostics;
using MicroM.Web.Authentication.OIDCDiagnostics;
using MicroM.Web.Extensions;
using MicroM.Web.Services;
using System.Text.Json;

namespace MicroM.Web.Authentication.SSO;

/// <summary>
/// Provides methods to test and diagnose OpenID Connect (OIDC) IDPClient role configuration.
/// </summary>
/// <remarks>
/// Validates the configured IdP for the given app_id by reading its discovery document and testing relevant IdP endpoints.
/// Does not assume co-location of client and IdP; all network calls go through IOIDCHttpClient. No client-initiated SLO here.
/// </remarks>
public class OIDCClientDiagnostics(
        IMicroMAppConfiguration appConfig,
        IOIDCHttpClient oidcHttpClient
    ) : IDiagnostics<string>
{

    private readonly ClientWellKnownAndJwksCheck WellKnownTest = new();
    private readonly ClientPARCheck PARTest = new();
    private readonly ClientAuthorizeMetadataCheck AuthorizeMetadataTest = new();
    private readonly ClientIdpRefreshCheck IdpRefreshTest = new();
    private readonly ClientEndSessionEndpointCheck EndSessionTest = new();
    private readonly ClientUserInfoEndpointCheck userInfoEndpointCheck = new();
    private readonly ClientRevocationEndpointCheck revocationEndpointCheck = new();
    private readonly ClientIntrospectionEndpointCheck introspectionEndpointCheck = new();
    private readonly ClientJwksCacheEffectivenessCheck jwksCacheEffectivenessCheck = new();
    private readonly ClientEncryptionMetadataCheck EncMetadataCheck = new();
    private readonly ClientRefreshFallbackCheck RefreshFallbackCheck = new();

    public async Task<Dictionary<string, List<DiagnosticResult>>> RunAllDiagnosticsAsync(string app_id, CancellationToken ct)
    {
        Dictionary<string, List<DiagnosticResult>> results = [];
        var app = appConfig.GetAppConfiguration(app_id);

        const string config_check = "oidc_client_configuration_check";

        if (app == null)
        {
            results[config_check] = [new(config_check, Errors: [new("app_not_found", "Application configuration not found")])];
            return results;
        }
        if (app.IdentityProviderRoleType != nameof(IdentityProviderRole.IDPClient))
        {
            results[config_check] = [new(config_check, Errors: [new("invalid_role", "Application is not configured as IDPClient")])];
            return results;

        }
        if (string.IsNullOrWhiteSpace(app.OIDCWellKnownURL))
        {
            results[config_check] = [new(config_check, Errors: [new("url_missing", "OIDC Well Known URL is not configured")])];
            return results;
        }

        var ctx = new ClientDiagnosticsContext(oidcHttpClient, app);

        List<DiagnosticResult> wk_result = await WellKnownTest.RunCheckAsync(ctx, ct);

        if (wk_result.Count == 0)
        {
            results[WellKnownTest.DiagnosticId] = [new(WellKnownTest.DiagnosticId, Errors: [new("missing_results", "OIDC Well Known URL diagnostic has returned an empty result")])];
        }
        else
        {
            results[WellKnownTest.DiagnosticId] = wk_result;
        }

        if (wk_result.isSuccess())
        {
            using var wellKnown = JsonDocument.Parse(wk_result[0].Result!);

            ctx.wellKnownDoc = wellKnown;

            ctx.parURL = wellKnown.RootElement.ReadString(WellknownIdentityConstants.PushedAuthorizationRequestEndpoint);
            ctx.authorizeURL = wellKnown.RootElement.ReadString(WellknownIdentityConstants.AuthorizationEndpoint);
            ctx.tokenURL = wellKnown.RootElement.ReadString(WellknownIdentityConstants.TokenEndpoint);
            ctx.idpRefreshURL = wellKnown.RootElement.ReadString(WellknownIdentityConstants.DeviceAuthorizationEndpoint);
            ctx.endSessionURL = wellKnown.RootElement.ReadString(WellknownIdentityConstants.EndSessionEndpoint);
            ctx.userInfoURL = wellKnown.RootElement.ReadString(WellknownIdentityConstants.UserinfoEndpoint);
            ctx.revocationURL = wellKnown.RootElement.ReadString(WellknownIdentityConstants.RevocationEndpoint);
            ctx.introspectionURL = wellKnown.RootElement.ReadString(WellknownIdentityConstants.IntrospectionEndpoint);
            ctx.jwksUri = wellKnown.RootElement.ReadString(WellknownIdentityConstants.JwksUri);

            // Populate token_endpoint_auth_signing_alg_values_supported into the context for reuse
            if (wellKnown.RootElement.TryGetProperty(WellknownIdentityConstants.TokenEndpointAuthSigningAlgValuesSupported, out var algArr)
                && algArr.ValueKind == JsonValueKind.Array)
            {
                var list = new List<OIDCSigningAlg>();
                foreach (var a in algArr.EnumerateArray())
                {
                    if (a.ValueKind == JsonValueKind.String &&
                        Enum.TryParse<OIDCSigningAlg>(a.GetString(), out var parsed))
                    {
                        list.Add(parsed);
                    }
                }
                if (list.Count > 0)
                    ctx.tokenEndpointAuthSigningAlgs = list;
            }

            var enc_result = await EncMetadataCheck.RunCheckAsync(ctx, ct);
            results[EncMetadataCheck.DiagnosticId] = enc_result;

            var par_result = await PARTest.RunCheckAsync(ctx, ct);
            results[PARTest.DiagnosticId] = par_result;

            var authorize_result = await AuthorizeMetadataTest.RunCheckAsync(ctx, ct);
            results[AuthorizeMetadataTest.DiagnosticId] = authorize_result;

            var idp_refresh_result = await IdpRefreshTest.RunCheckAsync(ctx, ct);
            results[IdpRefreshTest.DiagnosticId] = idp_refresh_result;

            var refresh_fallback_result = await RefreshFallbackCheck.RunCheckAsync(ctx, ct);
            results[RefreshFallbackCheck.DiagnosticId] = refresh_fallback_result;

            var end_session_result = await EndSessionTest.RunCheckAsync(ctx, ct);
            results[EndSessionTest.DiagnosticId] = end_session_result;

            var userinfo_result = await userInfoEndpointCheck.RunCheckAsync(ctx, ct);
            results[userInfoEndpointCheck.DiagnosticId] = userinfo_result;

            var revocation_result = await revocationEndpointCheck.RunCheckAsync(ctx, ct);
            results[revocationEndpointCheck.DiagnosticId] = revocation_result;

            var introspection_result = await introspectionEndpointCheck.RunCheckAsync(ctx, ct);
            results[introspectionEndpointCheck.DiagnosticId] = introspection_result;

            var jwks_cache_result = await jwksCacheEffectivenessCheck.RunCheckAsync(ctx, ct);
            results[jwksCacheEffectivenessCheck.DiagnosticId] = jwks_cache_result;

        }

        return results;
    }

}
