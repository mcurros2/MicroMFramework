using MicroM.Configuration;
using Microsoft.Extensions.Logging;
using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;

namespace MicroM.Web.Authentication.SSO;

public class OIDCHttpClient(IHttpClientFactory httpClientFactory, ILogger<OIDCHttpClient> log) : IOIDCHttpClient
{
    private static (Uri? uri, (string? error, string? error_description)) ValidateURL(string url, string title)
    {
        if (string.IsNullOrWhiteSpace(url)) return (null, (error: "invalid_request", error_description: $"{title} is empty"));
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri)) return (null, (error: "invalid_request", error_description: $"{title} is not an absolute URI"));
        if (!uri.Scheme.Equals(Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase) && !uri.IsLoopback)
            return (null, (error: "invalid_request", error_description: $"{title} must be HTTPS (non-loopback)"));

        return (uri, (null, null));
    }

    public async ValueTask<OIDCHttpClientPostResponse> GetWellKnownJsonAsync(string wellKnownUrl, CancellationToken ct, string? ifNoneMatch = null)
    {
        var (uri, url_validate) = ValidateURL(wellKnownUrl, "wellKnownUrl");
        if (url_validate.error != null) return new((int)HttpStatusCode.BadRequest, Error: JsonSerializer.Serialize(url_validate));

        try
        {
            using var client = httpClientFactory.CreateClient(ConfigurationDefaults.HTTPClientOidcName);
            using var req = new HttpRequestMessage(HttpMethod.Get, uri);
            req.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            req.Headers.AcceptCharset.Add(new StringWithQualityHeaderValue("utf-8"));
            if (!string.IsNullOrWhiteSpace(ifNoneMatch))
            {
                req.Headers.IfNoneMatch.Add(new EntityTagHeaderValue($"\"{ifNoneMatch}\""));
            }

            using var resp = await client.SendAsync(req, HttpCompletionOption.ResponseHeadersRead, ct);

            // 304 Not Modified → return metadata with NotModified = true
            if (resp.StatusCode == HttpStatusCode.NotModified)
            {
                var etag = resp.Headers.ETag?.Tag?.Trim('"');
                return new((int)resp.StatusCode, IsSuccessStatusCode: false, ContentType: "application/json", Body: "", Error: null, ETag: etag, NotModified: true);
            }

            var content = await resp.Content.ReadAsStringAsync(ct);

            // check content length doesn't exceed 32KB
            const int MAX_CONTENT_LENGTH = 32 * 1024;
            if (content.Length > MAX_CONTENT_LENGTH)
            {
                return new((int)HttpStatusCode.RequestEntityTooLarge, Error: JsonSerializer.Serialize(new { error = "server_error", error_description = $"well-known content is too large. Max size allowed {MAX_CONTENT_LENGTH}" }));
            }

            if (!resp.IsSuccessStatusCode)
            {
                return new((int)resp.StatusCode, Error: JsonSerializer.Serialize(new { error = "server_error", error_description = $"GET well-known failed: {(int)resp.StatusCode} {resp.ReasonPhrase}\n{content}" }));
            }

            string content_type = resp.Content.Headers.ContentType?.ToString() ?? "application/json";
            var responseEtag = resp.Headers.ETag?.Tag?.Trim('"');
            return new((int)resp.StatusCode, true, content_type, content, ETag: responseEtag);
        }
        catch (Exception ex)
        {
            log.LogWarning(ex, "Error fetching well-known at {url}", wellKnownUrl);
            return new((int)HttpStatusCode.InternalServerError, Error: JsonSerializer.Serialize(new { error = "server_error", error_description = $"Error fetching well-known: {ex.Message}" }));
        }
    }

    public async ValueTask<OIDCHttpClientPostResponse> GetJwksJsonAsync(string jwksUri, CancellationToken ct, string? ifNoneMatch = null)
    {
        var (uri, url_validate) = ValidateURL(jwksUri, "jwksUri");
        if (url_validate.error != null) return new((int)HttpStatusCode.BadRequest, Error: JsonSerializer.Serialize(url_validate));

        try
        {
            using var client = httpClientFactory.CreateClient(ConfigurationDefaults.HTTPClientJwksName);
            using var req = new HttpRequestMessage(HttpMethod.Get, uri);
            req.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            req.Headers.AcceptCharset.Add(new StringWithQualityHeaderValue("utf-8"));
            if (!string.IsNullOrWhiteSpace(ifNoneMatch))
            {
                req.Headers.IfNoneMatch.Add(new EntityTagHeaderValue($"\"{ifNoneMatch}\""));
            }

            using var resp = await client.SendAsync(req, HttpCompletionOption.ResponseHeadersRead, ct);


            // 304 Not Modified → return metadata with NotModified = true
            if (resp.StatusCode == HttpStatusCode.NotModified)
            {
                var etag = resp.Headers.ETag?.Tag?.Trim('"');
                return new((int)resp.StatusCode, ContentType: "application/json", ETag: etag, NotModified: true);
            }

            var content = await resp.Content.ReadAsStringAsync(ct);
            // check content length doesn't exceed 64KB
            const int MAX_CONTENT_LENGTH = 64 * 1024;
            if (content.Length > MAX_CONTENT_LENGTH)
            {
                return new((int)HttpStatusCode.RequestEntityTooLarge, Error: JsonSerializer.Serialize(new { error = "server_error", error_description = $"JWKS content is too large. Max size allowed {MAX_CONTENT_LENGTH}" }));
            }

            if (!resp.IsSuccessStatusCode)
            {
                return new((int)resp.StatusCode, Error: JsonSerializer.Serialize(new { error = "server_error", error_description = $"GET JWKS failed: {(int)resp.StatusCode} {resp.ReasonPhrase}\n{content}" }));
            }

            string content_type = resp.Content.Headers.ContentType?.ToString() ?? "application/json";
            var responseEtag = resp.Headers.ETag?.Tag?.Trim('"');
            return new((int)resp.StatusCode, true, content_type, content, ETag: responseEtag);
        }
        catch (Exception ex)
        {
            log.LogWarning(ex, "Error fetching JWKS at {url}", jwksUri);
            return new((int)HttpStatusCode.InternalServerError, Error: JsonSerializer.Serialize(new { error = "server_error", error_description = $"Error fetching JWKS: {ex.Message}" }));
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

    public Task<OIDCHttpClientPostResponse> PostFormUrlEncodedAsync(
    string url,
    IEnumerable<KeyValuePair<string, string>> form,
    CancellationToken ct)
    => PostFormAsync(ConfigurationDefaults.HTTPClientOidcName, url, form, authorization: null, ct);

    private async Task<OIDCHttpClientPostResponse> PostFormAsync(
        string namedClient,
        string url,
        IEnumerable<KeyValuePair<string, string>> form,
        AuthenticationHeaderValue? authorization,
        CancellationToken ct)
    {
        var (uri, url_validate) = ValidateURL(url, "url");
        if (url_validate.error != null) return new((int)HttpStatusCode.BadRequest, Error: JsonSerializer.Serialize(url_validate));

        try
        {
            using var client = httpClientFactory.CreateClient(namedClient);
            using var req = new HttpRequestMessage(HttpMethod.Post, uri)
            {
                Content = new FormUrlEncodedContent(form ?? [])
            };

            if (authorization != null) req.Headers.Authorization = authorization;
            req.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            req.Headers.AcceptCharset.Add(new StringWithQualityHeaderValue("utf-8"));

            using var resp = await client.SendAsync(req, ct);
            var body = await resp.Content.ReadAsStringAsync(ct);
            var ctype = resp.Content.Headers.ContentType?.ToString() ?? "application/json";
            return new((int)resp.StatusCode, resp.IsSuccessStatusCode, ctype, body);
        }
        catch (Exception ex)
        {
            log.LogWarning(ex, "POST form failed to {url}", url);
            var msg = new { error = "server_error", error_description = $"HTTP error: {ex.Message}" };
            return new(502, false, "application/json", JsonSerializer.Serialize(msg));
        }
    }
}