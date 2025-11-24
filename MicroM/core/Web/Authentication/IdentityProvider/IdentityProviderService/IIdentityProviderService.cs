using MicroM.Configuration;
using MicroM.Core;
using MicroM.Web.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Headers;
using System.Security.Claims;

namespace MicroM.Web.Authentication.SSO;

public interface IIdentityProviderService
{
    EtagCacheServiceCacheCheckResult HandleWellKnown(ApplicationOption app_config, string request_base, RequestHeaders request_headers, IHeaderDictionary response_headers);

    EtagCacheServiceCacheCheckResult? HandleJwks(ApplicationOption app_config, RequestHeaders request_headers, IHeaderDictionary response_headers);

    Task<ResultWithStatus<OIDCTokenResponse, ErrorResult>> HandleToken(ApplicationOption app, IFormCollection form, ClaimsPrincipal client, CancellationToken ct);

    ResultWithStatus<OIDCPARResponse, ErrorResult> HandlePAR(ApplicationOption app, IFormCollection form, ClaimsPrincipal client);

    Task<ResultWithStatus<OIDCAuthorizeRecord, ErrorResult>> HandleAuthorize(ApplicationOption app, IQueryCollection query, ClaimsPrincipal user, string request_base, CancellationToken ct);

    Task<bool> HandleEndSession(ApplicationOption app_config, string issuer, string user_id, CancellationToken ct);

    // Not implemented
    //Task<Dictionary<string, object?>> HandleUserInfo(ApplicationOption app_config, string app_id, string user_id, CancellationToken ct);

}
