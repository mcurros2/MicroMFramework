using System.Net.Http.Headers;

namespace MicroM.Web.Authentication.SSO;

public interface IOIDCHttpClient
{
    ValueTask<OIDCHttpClientPostResponse> GetWellKnownJsonAsync(string wellKnownUrl, CancellationToken ct);

    ValueTask<OIDCHttpClientPostResponse> GetJwksJsonAsync(string jwksUri, CancellationToken ct);

    // POST PAR (application/x-www-form-urlencoded)
    Task<OIDCHttpClientPostResponse> PostPushedAuthorizationRequestAsync(
        string parEndpoint,
        IEnumerable<KeyValuePair<string, string>> form,
        AuthenticationHeaderValue? authorization,
        CancellationToken ct);

    // POST Token (authorization_code or refresh_token, application/x-www-form-urlencoded)
    Task<OIDCHttpClientPostResponse> PostTokenAsync(
        string tokenEndpoint,
        IEnumerable<KeyValuePair<string, string>> form,
        AuthenticationHeaderValue? authorization,
        CancellationToken ct);

    // Generic form POST to arbitrary URL (application/x-www-form-urlencoded)
    Task<OIDCHttpClientPostResponse> PostFormUrlEncodedAsync(
        string url,
        IEnumerable<KeyValuePair<string, string>> form,
        CancellationToken ct);
}