using MicroM.Core;
using System.Net.Http.Headers;

namespace MicroM.Web.Authentication.SSO;

public interface IOIDCHttpClient
{
    ValueTask<ResultWithStatus<string, string>> GetWellKnownJsonAsync(string wellKnownUrl, CancellationToken ct);

    ValueTask<ResultWithStatus<string, string>> GetJwksJsonAsync(string jwksUri, CancellationToken ct);

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
}