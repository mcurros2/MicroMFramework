using MicroM.Diagnostics;
using MicroM.Web.Extensions;
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
            return [new(DiagnosticId, IsSuccess: true, Result: "SKIPPED: well-known URL not configured/advertised")];

        // 1) Fetch discovery
        var wk = await http.GetWellKnownJsonAsync(app.OIDCWellKnownURL!, ct);
        if (!wk.IsSuccessStatusCode || string.IsNullOrWhiteSpace(wk.Body))
            return [new(DiagnosticId, Errors: [new("wellknown_http_error", wk.Error ?? "Failed to fetch discovery")])];

        string? jwksUri;
        try
        {
            using var doc = JsonDocument.Parse(wk.Body);
            jwksUri = doc.RootElement.ReadString(WellknownIdentityConstants.JwksUri);
        }
        catch (JsonException ex)
        {
            // Do not include raw body here to avoid noise
            return [new(DiagnosticId, Errors: [new("wellknown_json_error", ex.Message)])];
        }

        if (string.IsNullOrWhiteSpace(jwksUri))
            return [new(DiagnosticId, Errors: [new("jwks_uri_missing", "Discovery does not contain jwks_uri")])];

        // 2) First JWKS fetch
        var jwks1 = await http.GetJwksJsonAsync(jwksUri, ct);
        if (!jwks1.IsSuccessStatusCode || string.IsNullOrWhiteSpace(jwks1.Body))
            return [new(DiagnosticId, Result: $"JWKS URL: {jwksUri}\nFirst fetch HTTP: {jwks1.StatusCode}", Errors: [new("jwks_http_error", jwks1.Error ?? "Failed to fetch JWKS")])];

        // Parse JWKS for keys metadata (no JWKS body leaked in results)
        int keysCount = 0;
        List<string> kids = [];
        try
        {
            using var jwksDoc = JsonDocument.Parse(jwks1.Body);
            var keysEl = jwksDoc.RootElement.ReadArray(WellknownIdentityConstants.Keys);
            if (keysEl is null || keysEl.Value.GetArrayLength() <= 0)
                return [new(DiagnosticId, Result: $"JWKS URL: {jwksUri}", Errors: [new("jwks_empty", "JWKS contains no keys")])];

            keysCount = keysEl.Value.GetArrayLength();
            foreach (var k in keysEl.Value.EnumerateArray())
            {
                var kid = k.ReadString("kid");
                if (!string.IsNullOrWhiteSpace(kid) && kids.Count < 5)
                    kids.Add(kid);
            }
        }
        catch (JsonException ex)
        {
            return [new(DiagnosticId, Result: $"JWKS URL: {jwksUri}", Errors: [new("jwks_json_error", ex.Message)])];
        }

        var etag1 = jwks1.ETag ?? "<none>";

        // 3) Second JWKS fetch with If-None-Match to exercise 304 path
        var jwks2 = await http.GetJwksJsonAsync(jwksUri, ct, ifNoneMatch: jwks1.ETag);
        var notModified = jwks2.NotModified;

        // Structured summary (no JWKS body included)
        var lines = new List<string>
        {
            $"JWKS URL: {jwksUri}",
            $"first_http_status: {jwks1.StatusCode}",
            $"first_etag: {etag1}",
            $"keys_count: {keysCount}",
            $"kids_sample: [{string.Join(", ", kids)}]"
        };

        if (string.IsNullOrWhiteSpace(jwks1.ETag))
        {
            lines.Add("revalidate_http_status: n/a");
            lines.Add("not_modified: false");
            lines.Add("result: Server did not provide ETag; 304 path cannot be tested (acceptable).");
            return [new(DiagnosticId, IsSuccess: true, Result: string.Join('\n', lines))];
        }

        lines.Add($"revalidate_http_status: {jwks2.StatusCode}");
        lines.Add($"not_modified: {notModified.ToString().ToLowerInvariant()}");

        if (notModified)
        {
            lines.Add("result: Cache effectiveness OK (304 observed).");
            return [new(DiagnosticId, IsSuccess: true, Result: string.Join('\n', lines))];
        }

        if (!jwks2.IsSuccessStatusCode || string.IsNullOrWhiteSpace(jwks2.Body))
        {
            return [new(DiagnosticId, Result: string.Join('\n', lines), Errors: [new("jwks_http_error_second", jwks2.Error ?? "Second JWKS fetch failed")])];
        }

        // If server did not return 304, check if content is unchanged
        var unchanged = jwks1.Body == jwks2.Body;
        lines.Add(unchanged
            ? "result: No 304, but content unchanged; server may not support ETag fully."
            : "result: JWKS content changed without 304; this could indicate rotation or missing ETag support.");

        return [new(DiagnosticId, IsSuccess: unchanged, Result: string.Join('\n', lines),
                    Errors: unchanged ? null : [new("jwks_changed_no_304", "Content changed without 304 Not Modified")])];
    }
}