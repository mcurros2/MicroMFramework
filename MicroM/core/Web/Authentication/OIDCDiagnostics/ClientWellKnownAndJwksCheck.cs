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
                return [new(DiagnosticId, Result: $"{{Well known: {wellKnownJson}\n\n JWKS: {jwksJson}}}", Errors: [new("jwks_empty", "JWKS contains no keys")])];

            // Cross-check JWKS keys vs advertised signing algs (optional robustness)
            HashSet<string> advertisedAlgs = new();
            if (root.TryGetProperty(WellknownIdentityConstants.IdTokenSigningAlgValuesSupported, out var algsEl) && algsEl.ValueKind == JsonValueKind.Array)
            {
                foreach (var algEl in algsEl.EnumerateArray())
                {
                    if (algEl.ValueKind == JsonValueKind.String && !string.IsNullOrWhiteSpace(algEl.GetString()))
                        advertisedAlgs.Add(algEl.GetString()!);
                }
            }

            bool hasRsaKey = false;
            HashSet<string> ecAlgsFound = new();
            foreach (var keyEl in keysEl.EnumerateArray())
            {
                if (keyEl.ValueKind != JsonValueKind.Object) continue;
                if (keyEl.TryGetProperty("kty", out var ktyEl) && ktyEl.ValueKind == JsonValueKind.String)
                {
                    var kty = ktyEl.GetString();
                    if (string.Equals(kty, "RSA", StringComparison.OrdinalIgnoreCase))
                    {
                        hasRsaKey = true;
                    }
                    else if (string.Equals(kty, "EC", StringComparison.OrdinalIgnoreCase))
                    {
                        if (keyEl.TryGetProperty("alg", out var algEl) && algEl.ValueKind == JsonValueKind.String)
                        {
                            var alg = algEl.GetString();
                            if (!string.IsNullOrWhiteSpace(alg))
                                ecAlgsFound.Add(alg);
                        }
                    }
                }
            }

            // Validate: if any ES* alg advertised, corresponding EC key must be present
            var advertisedEcAlgs = advertisedAlgs.Where(a => a.StartsWith("ES", StringComparison.OrdinalIgnoreCase)).ToList();
            if (advertisedEcAlgs.Count > 0 && !advertisedEcAlgs.Any(a => ecAlgsFound.Contains(a)))
            {
                return [new(DiagnosticId,
                    Result: $"{{Well known: {wellKnownJson}\n\n JWKS: {jwksJson}}}",
                    Errors: [new("jwks_ec_alg_missing", $"Advertised EC signing alg(s) [{string.Join(", ", advertisedEcAlgs)}] but none found in JWKS.")])];
            }

            // Validate: if RS256/RS512 advertised, ensure at least one RSA key (alg omitted intentionally in RSA JWKs)
            var advertisedRsaAlgs = advertisedAlgs.Where(a => a.StartsWith("RS", StringComparison.OrdinalIgnoreCase)).ToList();
            if (advertisedRsaAlgs.Count > 0 && !hasRsaKey)
            {
                return [new(DiagnosticId,
                    Result: $"{{Well known: {wellKnownJson}\n\n JWKS: {jwksJson}}}",
                    Errors: [new("jwks_rsa_missing", $"Advertised RSA signing alg(s) [{string.Join(", ", advertisedRsaAlgs)}] but no RSA key found in JWKS.")])];
            }

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
