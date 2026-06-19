using MicroM.Diagnostics;
using MicroM.Web.Extensions;
using System.Diagnostics;
using System.Text.Json;

namespace MicroM.Web.Authentication.OIDCDiagnostics;

internal sealed class IdpClientJwksCheck : IDiagnosticCheck<IdPDiagnosticsContext>
{
    public string DiagnosticId => "oidc_idp_client_jwks_check";

    public async Task<List<DiagnosticResult>> RunCheckAsync(IdPDiagnosticsContext ctx, CancellationToken ct)
    {
        var app = ctx.App;
        var http = ctx.HttpClient;

        List<DiagnosticResult> results = [];

        if (app.OIDCClientConfiguration == null || app.OIDCClientConfiguration.Count == 0)
        {
            results.Add(new(DiagnosticId, Result: "No OIDC clients configured for this IdP", Errors: [new("no_clients_configured", "OIDCClientConfiguration is empty")]));
            return results;
        }

        foreach (var client in app.OIDCClientConfiguration.Values)
        {
            var clientId = client.ClientAPPID ?? "(unknown)";
            var jwksUrl = client.URLClientJWKS ?? string.Empty;

            if (string.IsNullOrWhiteSpace(jwksUrl))
            {
                results.Add(new(DiagnosticId, Result:
$@"client_id: {clientId}
jwks_url: (missing)", Errors: [new("client_jwks_missing", "Client JWKS URL not configured")]));
                continue;
            }

            if (!jwksUrl.isValidHTTPSUrl())
            {
                results.Add(new(DiagnosticId, Result:
$@"client_id: {clientId}
jwks_url: {jwksUrl}", Errors: [new("jwks_url_invalid", "Client JWKS URL must be HTTPS")]));
                continue;
            }

            try
            {
                var sw1 = Stopwatch.StartNew();
                var resp = await http.GetJwksJsonAsync(jwksUrl, ct);
                sw1.Stop();

                var httpStatus1 = resp.StatusCode;
                var etag1 = resp.ETag ?? "n/a";

                if (!resp.IsSuccessStatusCode || string.IsNullOrWhiteSpace(resp.Body))
                {
                    results.Add(new(DiagnosticId, Result:
$@"client_id: {clientId}
jwks_url: {jwksUrl}
http_status: {httpStatus1}
duration_ms: {sw1.ElapsedMilliseconds}
etag: {etag1}", Errors: [new("jwks_http_error", resp.Error ?? "Failed to fetch client JWKS")]));
                    continue;
                }

                int keysCount = 0;
                List<string> kidsSample = [];
                string kidMatch = "n/a";
                string? configuredKid = client.CertificateUniqueID;

                using (var jwks = JsonDocument.Parse(resp.Body))
                {
                    var keysElOpt = jwks.RootElement.ReadArray(WellknownIdentityConstants.Keys);
                    if (keysElOpt is null || keysElOpt.Value.GetArrayLength() == 0)
                    {
                        results.Add(new(DiagnosticId, Result:
$@"client_id: {clientId}
jwks_url: {jwksUrl}
http_status: {httpStatus1}
duration_ms: {sw1.ElapsedMilliseconds}
etag: {etag1}
keys_count: 0", Errors: [new("jwks_empty", "Client JWKS contains no keys")]));
                        continue;
                    }

                    var keysEl = keysElOpt.Value;
                    keysCount = keysEl.GetArrayLength();

                    foreach (var keyEl in keysEl.EnumerateArray())
                    {
                        if (kidsSample.Count >= 5) break;
                        var kid = keyEl.ReadString("kid");
                        if (!string.IsNullOrWhiteSpace(kid))
                            kidsSample.Add(kid!);
                    }

                    if (!string.IsNullOrWhiteSpace(configuredKid))
                    {
                        bool found = false;
                        foreach (var keyEl in keysEl.EnumerateArray())
                        {
                            var kid = keyEl.ReadString("kid");
                            if (!string.IsNullOrWhiteSpace(kid) && kid == configuredKid)
                            {
                                found = true;
                                break;
                            }
                        }
                        kidMatch = found ? "true" : "false";
                        if (!found)
                        {
                            results.Add(new(DiagnosticId, Result:
$@"client_id: {clientId}
jwks_url: {jwksUrl}
http_status: {httpStatus1}
duration_ms: {sw1.ElapsedMilliseconds}
etag: {etag1}
keys_count: {keysCount}
kids_sample: [{string.Join(", ", kidsSample)}]
kid_match: {kidMatch}
configured_kid: {configuredKid}", Errors: [new("jwks_kid_missing", $"Configured key id not found in client JWKS: kid='{configuredKid}'")]));
                            continue;
                        }
                    }
                    else
                    {
                        kidMatch = "n/a";
                        if (keysCount > 1)
                        {
                            results.Add(new(DiagnosticId, IsSuccess: true, Result:
$@"Status: OK (client JWKS reachable)
client_id: {clientId}
jwks_url: {jwksUrl}
http_status: {httpStatus1}
duration_ms: {sw1.ElapsedMilliseconds}
etag: {etag1}
keys_count: {keysCount}
kids_sample: [{string.Join(", ", kidsSample)}]
kid_match: {kidMatch}
warning: Multiple keys present but no configured key id"));
                            continue;
                        }
                    }
                }

                // Optional revalidation using ETag to surface 304 behavior
                var sw2 = Stopwatch.StartNew();
                var resp2 = await http.GetJwksJsonAsync(jwksUrl, ct, ifNoneMatch: resp.ETag);
                sw2.Stop();

                var httpStatus2 = resp2.StatusCode;
                var notModified = resp2.NotModified;

                results.Add(new(DiagnosticId, IsSuccess: true, Result:
$@"Status: OK (client JWKS reachable)
client_id: {clientId}
jwks_url: {jwksUrl}
http_status: {httpStatus1}
duration_ms: {sw1.ElapsedMilliseconds}
etag: {etag1}
revalidate_status: {httpStatus2}
revalidate_duration_ms: {sw2.ElapsedMilliseconds}
not_modified: {notModified.ToString().ToLowerInvariant()}
keys_count: {keysCount}
kids_sample: [{string.Join(", ", kidsSample)}]
kid_match: {kidMatch}{(string.IsNullOrWhiteSpace(configuredKid) ? "" : $"\nconfigured_kid: {configuredKid}")}"));
            }
            catch (OperationCanceledException)
            {
                results.Add(new(DiagnosticId, Result:
$@"client_id: {clientId}
jwks_url: {jwksUrl}", Errors: [new("cancelled", "Operation canceled")]));
            }
            catch (JsonException ex)
            {
                results.Add(new(DiagnosticId, Result:
$@"client_id: {clientId}
jwks_url: {jwksUrl}", Errors: [new("json_error", ex.Message)]));
            }
            catch (Exception ex)
            {
                results.Add(new(DiagnosticId, Result:
$@"client_id: {clientId}
jwks_url: {jwksUrl}", Errors: [new("unexpected_error", ex.Message)]));
            }
        }

        return results;
    }
}