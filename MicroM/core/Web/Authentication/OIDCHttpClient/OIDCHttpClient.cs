using MicroM.Configuration;
using MicroM.Core;
using Microsoft.Extensions.Logging;
using System.Net.Http.Headers;

namespace MicroM.Web.Authentication.SSO;

public class OIDCHttpClient(IHttpClientFactory httpClientFactory, ILogger<OIDCHttpClient> log) : IOIDCHttpClient
{
    public async ValueTask<ResultWithStatus<string, string>> GetWellKnownJsonAsync(string wellKnownUrl, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(wellKnownUrl)) return new(null, "wellKnownUrl is empty");
        if (!Uri.TryCreate(wellKnownUrl, UriKind.Absolute, out var uri)) return new(null, "wellKnownUrl is not an absolute URI");
        if (!uri.Scheme.Equals(Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase) && !uri.IsLoopback)
            return new(null, "wellKnownUrl must be HTTPS (non-loopback)");

        try
        {
            using var client = httpClientFactory.CreateClient(ConfigurationDefaults.HTTPClientOidcName);
            using var req = new HttpRequestMessage(HttpMethod.Get, uri);
            req.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            req.Headers.AcceptCharset.Add(new StringWithQualityHeaderValue("utf-8"));

            using var resp = await client.SendAsync(req, HttpCompletionOption.ResponseHeadersRead, ct);
            var content = await resp.Content.ReadAsStringAsync(ct);

            // check content length doesn't exceed 32KB
            const int MAX_CONTENT_LENGTH = 32 * 1024;
            if (content.Length > MAX_CONTENT_LENGTH)
            {
                return new(null, $"well-known content is too large. Max size allowed {MAX_CONTENT_LENGTH}");
            }

            if (!resp.IsSuccessStatusCode)
            {
                return new(null, $"GET well-known failed: {(int)resp.StatusCode} {resp.ReasonPhrase}\n{content}");
            }
            return new(content, null);
        }
        catch (Exception ex)
        {
            log.LogWarning(ex, "Error fetching well-known at {url}", wellKnownUrl);
            return new(null, $"Error fetching well-known: {ex.Message}");
        }
    }

    public async ValueTask<ResultWithStatus<string, string>> GetJwksJsonAsync(string jwksUri, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(jwksUri)) return new(null, "jwksUri is empty");
        if (!Uri.TryCreate(jwksUri, UriKind.Absolute, out var uri)) return new(null, "jwksUri is not an absolute URI");
        if (!uri.Scheme.Equals(Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase) && !uri.IsLoopback)
            return new(null, "jwksUri must be HTTPS (non-loopback)");

        try
        {
            using var client = httpClientFactory.CreateClient(ConfigurationDefaults.HTTPClientJwksName);
            using var req = new HttpRequestMessage(HttpMethod.Get, uri);
            req.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            req.Headers.AcceptCharset.Add(new StringWithQualityHeaderValue("utf-8"));

            using var resp = await client.SendAsync(req, HttpCompletionOption.ResponseHeadersRead, ct);
            var content = await resp.Content.ReadAsStringAsync(ct);

            if (!resp.IsSuccessStatusCode)
            {
                return new(null, $"GET JWKS failed: {(int)resp.StatusCode} {resp.ReasonPhrase}\n{content}");
            }
            return new(content, null);
        }
        catch (Exception ex)
        {
            log.LogWarning(ex, "Error fetching JWKS at {url}", jwksUri);
            return new(null, $"Error fetching JWKS: {ex.Message}");
        }
    }

    public Task<OIDCHttpClientPostResponse> PostPushedAuthorizationRequestAsync(
        string parEndpoint,
        IEnumerable<KeyValuePair<string, string>> form,
        AuthenticationHeaderValue? authorization,
        CancellationToken ct)
        => PostFormAsync(ConfigurationDefaults.HTTPClientOidcName, parEndpoint, form, authorization, ct);

    public Task<OIDCHttpClientPostResponse> PostTokenAsync(
        string tokenEndpoint,
        IEnumerable<KeyValuePair<string, string>> form,
        AuthenticationHeaderValue? authorization,
        CancellationToken ct)
        => PostFormAsync(ConfigurationDefaults.HTTPClientOidcName, tokenEndpoint, form, authorization, ct);

    private async Task<OIDCHttpClientPostResponse> PostFormAsync(
        string namedClient,
        string url,
        IEnumerable<KeyValuePair<string, string>> form,
        AuthenticationHeaderValue? authorization,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(url)) return new(400, "application/json", "{\"error\":\"invalid_request\",\"error_description\":\"URL is empty\"}");
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri)) return new(400, "application/json", "{\"error\":\"invalid_request\",\"error_description\":\"URL is not an absolute URI\"}");
        if (!uri.Scheme.Equals(Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase) && !uri.IsLoopback)
            return new(400, "application/json", "{\"error\":\"invalid_request\",\"error_description\":\"URL must be HTTPS (non-loopback)\"}");

        try
        {
            using var client = httpClientFactory.CreateClient(namedClient);
            using var req = new HttpRequestMessage(HttpMethod.Post, uri)
            {
                Content = new FormUrlEncodedContent(form ?? Array.Empty<KeyValuePair<string, string>>())
            };

            if (authorization != null) req.Headers.Authorization = authorization;
            req.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            req.Headers.AcceptCharset.Add(new StringWithQualityHeaderValue("utf-8"));

            using var resp = await client.SendAsync(req, ct);
            var body = await resp.Content.ReadAsStringAsync(ct);
            var ctype = resp.Content.Headers.ContentType?.ToString() ?? "application/json";
            return new((int)resp.StatusCode, ctype, body);
        }
        catch (Exception ex)
        {
            log.LogWarning(ex, "POST form failed to {url}", url);
            var msg = new { error = "server_error", error_description = $"HTTP error: {ex.Message}" };
            return new(502, "application/json", System.Text.Json.JsonSerializer.Serialize(msg));
        }
    }
}