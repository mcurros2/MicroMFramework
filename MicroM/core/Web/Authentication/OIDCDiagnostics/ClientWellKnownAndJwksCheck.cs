using MicroM.Diagnostics;
using System.Text.Json;

namespace MicroM.Web.Authentication.OIDCDiagnostics;

internal class ClientWellKnownAndJwksCheck() : IDiagnosticCheck<ClientDiagnosticsContext>
{
    public string DiagnosticId => "oidc_client_wellknown_jwks_check";

    public async Task<List<DiagnosticResult>> RunCheckAsync(ClientDiagnosticsContext ctx, CancellationToken ct)
    {
        var app = ctx.App;
        var httpClient = ctx.HttpClient;

        string? wellKnownJson = null;
        string? jwksJson = null;
        try
        {
            var wellKnownResponse = await httpClient.GetWellKnownJsonAsync(app.OIDCWellKnownURL!, ct);
            if (!wellKnownResponse.IsSuccessStatusCode || string.IsNullOrWhiteSpace(wellKnownResponse.Body))
                return [new(DiagnosticId, Errors: [new("wellknown_http_error", wellKnownResponse.Error ?? "Failed to fetch discovery")])];

            wellKnownJson = wellKnownResponse.Body;
            using var doc = JsonDocument.Parse(wellKnownJson);
            var root = doc.RootElement;

            var hasIssuer = root.TryGetProperty(WellknownIdentityConstants.Issuer, out var issuerProp) && issuerProp.ValueKind == JsonValueKind.String && !string.IsNullOrWhiteSpace(issuerProp.GetString());
            var hasJwksUri = root.TryGetProperty(WellknownIdentityConstants.JwksUri, out var jwksProp) && jwksProp.ValueKind == JsonValueKind.String && !string.IsNullOrWhiteSpace(jwksProp.GetString());
            if (!hasIssuer || !hasJwksUri)
                return [new(DiagnosticId, Result: wellKnownJson, Errors: [new("wellknown_invalid", "Discovery document missing required fields: issuer and/or jwks_uri")])];

            var jwksUri = jwksProp.GetString()!;
            var jwksResponse = await httpClient.GetJwksJsonAsync(jwksUri, ct);
            if (!jwksResponse.IsSuccessStatusCode || string.IsNullOrWhiteSpace(jwksResponse.Body))
                return [new(DiagnosticId, Result: wellKnownJson, Errors: [new("jwks_http_error", jwksResponse.Error ?? "Failed to fetch JWKS")])];

            jwksJson = jwksResponse.Body;
            using var jwksDoc = JsonDocument.Parse(jwksJson);
            if (!jwksDoc.RootElement.TryGetProperty(WellknownIdentityConstants.Keys, out var keysEl) || keysEl.ValueKind != JsonValueKind.Array || keysEl.GetArrayLength() <= 0)
                return [new(DiagnosticId, Result: $"Well known: {wellKnownJson}\n\n JWKS: {jwksJson}", Errors: [new("jwks_empty", "JWKS contains no keys")])];

            return [
                new(DiagnosticId, IsSuccess: true, Result: wellKnownJson),
                new(DiagnosticId, IsSuccess: true, Result: jwksJson),
                ];
        }
        catch (OperationCanceledException)
        {
            return [new(DiagnosticId, Result: $"Well known: {wellKnownJson}\n\n JWKS: {jwksJson}", Errors: [new("cancelled", "Operation canceled")])];
        }
        catch (JsonException ex)
        {
            return [new(DiagnosticId, Result: $"Well known: {wellKnownJson}\n\n JWKS: {jwksJson}", Errors: [new("json_error", ex.Message)])];
        }
        catch (Exception ex)
        {
            return [new(DiagnosticId, Result: $"Well known: {wellKnownJson}\n\n JWKS: {jwksJson}", Errors: [new("unexpected_error", ex.Message)])];
        }
    }
}
