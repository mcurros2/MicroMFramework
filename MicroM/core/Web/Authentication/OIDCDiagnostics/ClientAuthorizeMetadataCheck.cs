using MicroM.Diagnostics;
using System.Text.Json;

namespace MicroM.Web.Authentication.OIDCDiagnostics;

internal class ClientAuthorizeMetadataCheck() : IDiagnosticCheck<ClientDiagnosticsContext>
{
    public string DiagnosticId => "oidc_client_authorize_metadata_check";

    public async Task<List<DiagnosticResult>> RunCheckAsync(ClientDiagnosticsContext ctx, CancellationToken ct)
    {
        var app = ctx.App;
        var httpClient = ctx.HttpClient;

        try
        {

            if (string.IsNullOrWhiteSpace(ctx.authorizeURL))
                return [new(DiagnosticId, Errors: [new("authorize_missing", "Discovery is missing authorization_endpoint")])];

            var root = ctx.wellKnownDoc!.RootElement;

            bool supportsCode = true;
            if (root.TryGetProperty("response_types_supported", out var rts) && rts.ValueKind == JsonValueKind.Array)
                supportsCode = rts.EnumerateArray().Any(x => x.ValueKind == JsonValueKind.String && string.Equals(x.GetString(), WellknownIdentityConstants.Code, StringComparison.OrdinalIgnoreCase));
            if (!supportsCode)
                return [new(DiagnosticId, Result: $"authorization_endpoint: {ctx.authorizeURL}", Errors: [new("authorize_code_missing", "IdP does not advertise response_types_supported=code")])];

            bool supportsS256 = true;
            if (root.TryGetProperty("code_challenge_methods_supported", out var ccms) && ccms.ValueKind == JsonValueKind.Array)
                supportsS256 = ccms.EnumerateArray().Any(x => x.ValueKind == JsonValueKind.String && string.Equals(x.GetString(), "S256", StringComparison.OrdinalIgnoreCase));
            if (!supportsS256)
                return [new(DiagnosticId, Result: $"authorization_endpoint: {ctx.authorizeURL}", Errors: [new("pkce_s256_missing", "IdP does not advertise PKCE S256 support")])];

            bool scopesOk = true;
            if (root.TryGetProperty("scopes_supported", out var scopes) && scopes.ValueKind == JsonValueKind.Array)
                scopesOk = scopes.EnumerateArray().Any(x => x.ValueKind == JsonValueKind.String && string.Equals(x.GetString(), WellknownIdentityConstants.OpenID, StringComparison.OrdinalIgnoreCase));

            var summary = $"authorization_endpoint: {ctx.authorizeURL}\nPKCE_S256: {supportsS256}\nScopesHasOpenId: {scopesOk}";
            return [new(DiagnosticId, IsSuccess: true, Result: summary)];
        }
        catch (OperationCanceledException)
        {
            return [new(DiagnosticId, Errors: [new("cancelled", "Operation canceled")])];
        }
        catch (JsonException ex)
        {
            return [new(DiagnosticId, Errors: [new("json_error", ex.Message)])];
        }
        catch (Exception ex)
        {
            return [new(DiagnosticId, Errors: [new("unexpected_error", ex.Message)])];
        }
    }
}