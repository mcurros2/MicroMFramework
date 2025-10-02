using MicroM.Configuration;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Headers;
using System.Security.Claims;

namespace MicroM.Web.Authentication.SSO;

public interface IIdentityProviderService
{
    EtagCacheServiceCacheCheckResult HandleWellKnown(ApplicationOption app_config, string request_base, RequestHeaders request_headers, IHeaderDictionary response_headers);

    EtagCacheServiceCacheCheckResult? HandleJwks(ApplicationOption app_config, RequestHeaders request_headers, IHeaderDictionary response_headers);

    (OIDCTokenResponse? response, object? error) HandleToken(ApplicationOption app, IFormCollection form, ClaimsPrincipal client);

    (OIDCPARResponse? response, object? error) HandlePAR(ApplicationOption app, IFormCollection form, ClaimsPrincipal client);

    Task<(string? redirectUrl, string? loginUrl, object? error)> HandleAuthorize(ApplicationOption app, IQueryCollection query, ClaimsPrincipal user, string request_base, CancellationToken ct);

    Task<Dictionary<string, object?>> HandleUserInfo(ApplicationOption app_config, string app_id, string user_id, CancellationToken ct);

    Task<bool> HandleEndSession(ApplicationOption app_config, string app_id, string user_id, CancellationToken ct);


}
