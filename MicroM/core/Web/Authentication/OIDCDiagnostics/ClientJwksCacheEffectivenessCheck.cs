using MicroM.Diagnostics;
using MicroM.Extensions;
using System.Text.Json;

namespace MicroM.Web.Authentication.OIDCDiagnostics;

internal class ClientJwksCacheEffectivenessCheck : IDiagnosticCheck<ClientDiagnosticsContext>
{
    public string DiagnosticId => "oidc_client_jwks_cache_effectiveness_check";

    public async Task<List<DiagnosticResult>> RunCheckAsync(ClientDiagnosticsContext ctx, CancellationToken ct)
    {
        var app = ctx.App;
        var http = ctx.HttpClient;

        if (string.IsNullOrWhiteSpace(app.OIDCWellKnownURL))
        {
            return [new(DiagnosticId, IsSuccess: true, Result: "SKIPPED: well-known URL not configured/advertised")];
        }

        // Fetch discovery
        var wk = await http.GetWellKnownJsonAsync(app.OIDCWellKnownURL!, ct);
        if (!wk.IsSuccessStatusCode || string.IsNullOrWhiteSpace(wk.Body))
        {
            return [new(DiagnosticId, Errors: [new("wellknown_http_error", wk.Error ?? "Failed to fetch discovery")])];
        }

        string? jwksUri;
        try
        {
            using var doc = JsonDocument.Parse(wk.Body);
            var root = doc.RootElement;
            jwksUri = root.TryGetProperty(WellknownIdentityConstants.JwksUri, out var jwksProp) ? jwksProp.GetString() : null;
        }
        catch (JsonException ex)
        {
            return [new(DiagnosticId, Result: wk.Body, Errors: [new("wellknown_json_error", ex.Message)])];
        }

        if (string.IsNullOrWhiteSpace(jwksUri))
        {
            return [new(DiagnosticId, Result: wk.Body, Errors: [new("jwks_uri_missing", "Discovery does not contain jwks_uri")])];
        }

        // First JWKS fetch
        var jwks1 = await http.GetJwksJsonAsync(jwksUri, ct);
        if (!jwks1.IsSuccessStatusCode || string.IsNullOrWhiteSpace(jwks1.Body))
        {
            return [new(DiagnosticId, Result: "JWKS first fetch failed", Errors: [new("jwks_http_error", jwks1.Error ?? "Failed to fetch JWKS")])];
        }

        // Parse JWKS for keys metadata
        int keysCount = 0;
        List<string> kids = [];
        try
        {
            using var jwksDoc = JsonDocument.Parse(jwks1.Body);
            if (jwksDoc.RootElement.TryGetProperty(WellknownIdentityConstants.Keys, out var keysEl) && keysEl.ValueKind == JsonValueKind.Array)
            {
                keysCount = keysEl.GetArrayLength();
                foreach (var k in keysEl.EnumerateArray())
                {
                    if (k.ValueKind == JsonValueKind.Object &&
                        k.TryGetProperty("kid", out var kidEl) &&
                        kidEl.ValueKind == JsonValueKind.String)
                    {
                        if (kids.Count < 5) kids.Add(kidEl.GetString()!);
                    }
                }
            }
            if (keysCount <= 0)
            {
                return [new(DiagnosticId, Result: jwks1.Body, Errors: [new("jwks_empty", "JWKS contains no keys")])];
            }
        }
        catch (JsonException ex)
        {
            return [new(DiagnosticId, Result: jwks1.Body.Truncate(1024), Errors: [new("jwks_json_error", ex.Message)])];
        }

        var etag1 = jwks1.ETag ?? "<none>";

        // Second JWKS fetch with If-None-Match
        var jwks2 = await http.GetJwksJsonAsync(jwksUri, ct, ifNoneMatch: jwks1.ETag);
        var jwks2Status = jwks2.StatusCode;
        var notModified = jwks2.NotModified;

        // Build user-readable summary (no JWKS body included)
        var lines = new List<string>
        {
            $"JWKS URL: {jwksUri}",
            $"First fetch: HTTP {jwks1.StatusCode}, ETag received: {etag1}, Keys: {keysCount}, Sample kids: [{string.Join(", ", kids)}]"
        };

        if (string.IsNullOrWhiteSpace(jwks1.ETag))
        {
            lines.Add("Revalidate: Server did not provide ETag; 304 path cannot be tested (acceptable).");
            return [new(DiagnosticId, IsSuccess: true, Result: string.Join('\n', lines))];
        }

        lines.Add($"Revalidate: If-None-Match={etag1} -> HTTP {jwks2Status}, NotModified={notModified}");

        if (notModified)
        {
            lines.Add("Result: Cache effectiveness OK (304 observed).");
            return [new(DiagnosticId, IsSuccess: true, Result: string.Join('\n', lines))];
        }

        if (!jwks2.IsSuccessStatusCode || string.IsNullOrWhiteSpace(jwks2.Body))
        {
            return [new(DiagnosticId, Result: string.Join('\n', lines), Errors: [new("jwks_http_error_second", jwks2.Error ?? "Second JWKS fetch failed")])];
        }

        // If no 304, at least verify content is unchanged
        var unchanged = string.Equals(jwks1.Body, jwks2.Body, StringComparison.Ordinal);
        lines.Add(unchanged
            ? "Result: No 304, but content unchanged; server may not support ETag fully."
            : "Result: JWKS content changed without 304; this could indicate rotation or missing ETag support.");

        return [new(DiagnosticId, IsSuccess: unchanged, Result: string.Join('\n', lines),
                    Errors: unchanged ? null : [new("jwks_changed_no_304", "Content changed without 304 Not Modified")])];
    }
}