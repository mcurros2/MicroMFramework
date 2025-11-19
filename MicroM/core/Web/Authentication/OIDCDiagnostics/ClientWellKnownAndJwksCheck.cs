using MicroM.Diagnostics;
using MicroM.Web.Authentication.SSO;
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
            // 1) Fetch well-known (initial)
            var wk1 = await httpClient.GetWellKnownJsonAsync(app.OIDCWellKnownURL!, ct);
            if (!wk1.IsSuccessStatusCode || string.IsNullOrWhiteSpace(wk1.Body))
                return [new(DiagnosticId, Errors: [new("wellknown_http_error", wk1.Error ?? "Failed to fetch discovery")])];

            wellKnownJson = wk1.Body;
            using var doc = JsonDocument.Parse(wellKnownJson);
            var root = doc.RootElement;

            var hasIssuer = root.TryGetProperty(WellknownIdentityConstants.Issuer, out var issuerProp) && issuerProp.ValueKind == JsonValueKind.String && !string.IsNullOrWhiteSpace(issuerProp.GetString());
            var hasJwksUri = root.TryGetProperty(WellknownIdentityConstants.JwksUri, out var jwksProp) && jwksProp.ValueKind == JsonValueKind.String && !string.IsNullOrWhiteSpace(jwksProp.GetString());
            if (!hasIssuer || !hasJwksUri)
                return [new(DiagnosticId, Result: wellKnownJson, Errors: [new("wellknown_invalid", "Discovery document missing required fields: issuer and/or jwks_uri")])];

            // Optional revalidation to exercise 304 path (kept out of main results to avoid breaking callers)
            var wk1Etag = wk1.ETag;
            _ = await httpClient.GetWellKnownJsonAsync(app.OIDCWellKnownURL!, ct, ifNoneMatch: wk1Etag);

            // 2) Fetch JWKS (initial)
            var jwksUri = jwksProp.GetString()!;
            var jw1 = await httpClient.GetJwksJsonAsync(jwksUri, ct);
            if (!jw1.IsSuccessStatusCode || string.IsNullOrWhiteSpace(jw1.Body))
                return [new(DiagnosticId, Result: wellKnownJson, Errors: [new("jwks_http_error", jw1.Error ?? "Failed to fetch JWKS")])];

            jwksJson = jw1.Body;
            using var jwksDoc = JsonDocument.Parse(jwksJson);
            if (!jwksDoc.RootElement.TryGetProperty(WellknownIdentityConstants.Keys, out var keysEl) || keysEl.ValueKind != JsonValueKind.Array || keysEl.GetArrayLength() <= 0)
                return [new(DiagnosticId, Result: "JWKS: <redacted>; discovery included for reference", Errors: [new("jwks_empty", "JWKS contains no keys")])];

            // Optional revalidation to exercise 304 path (kept out of main results to avoid breaking callers)
            var jw1Etag = jw1.ETag;
            _ = await httpClient.GetJwksJsonAsync(jwksUri, ct, ifNoneMatch: jw1Etag);

            // Optional: light cross-check between advertised sig algs and key types
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
                if (keyEl.TryGetProperty(WellknownIdentityConstants.Kty, out var ktyEl) && ktyEl.ValueKind == JsonValueKind.String)
                {
                    var kty = ktyEl.GetString();
                    if (string.Equals(kty, nameof(OIDCKeyType.RSA), StringComparison.OrdinalIgnoreCase))
                    {
                        hasRsaKey = true;
                    }
                    else if (string.Equals(kty, nameof(OIDCKeyType.EC), StringComparison.OrdinalIgnoreCase))
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
                    Result: "JWKS: <redacted>; discovery included for reference",
                    Errors: [new("jwks_ec_alg_missing", $"Advertised EC signing alg(s) [{string.Join(", ", advertisedEcAlgs)}] but none found in JWKS.")])];
            }

            // Validate: if RS* advertised, ensure at least one RSA key present
            var advertisedRsaAlgs = advertisedAlgs.Where(a => a.StartsWith("RS", StringComparison.OrdinalIgnoreCase)).ToList();
            if (advertisedRsaAlgs.Count > 0 && !hasRsaKey)
            {
                return [new(DiagnosticId,
                    Result: "JWKS: <redacted>; discovery included for reference",
                    Errors: [new("jwks_rsa_missing", $"Advertised RSA signing alg(s) [{string.Join(", ", advertisedRsaAlgs)}] but no RSA key found in JWKS.")])];
            }

            // IMPORTANT: preserve contract — first result = raw well-known JSON; second result = raw JWKS JSON (no truncation)
            List<DiagnosticResult> results =
            [
                new(DiagnosticId, IsSuccess: true, Result: wellKnownJson),
                new(DiagnosticId, IsSuccess: true, Result: jwksJson)
            ];

            // Optionally append a third human-readable summary (does not change the first two indices)
            if (wk1.ETag != null || jw1.ETag != null)
            {
                results.Add(new(DiagnosticId, IsSuccess: true,
                    Result:
$@"ETag summary (optional)
well-known ETag: {wk1.ETag ?? "n/a"}
jwks      ETag: {jw1.ETag ?? "n/a"}"));
            }

            return results;
        }
        catch (OperationCanceledException)
        {
            return [new(DiagnosticId, Result: "JWKS: <redacted>; discovery may be present", Errors: [new("cancelled", "Operation canceled")])];
        }
        catch (JsonException ex)
        {
            return [new(DiagnosticId, Result: "JWKS: <redacted>; discovery may be present", Errors: [new("json_error", ex.Message)])];
        }
        catch (Exception ex)
        {
            return [new(DiagnosticId, Result: "JWKS: <redacted>; discovery may be present", Errors: [new("unexpected_error", ex.Message)])];
        }
    }
}
