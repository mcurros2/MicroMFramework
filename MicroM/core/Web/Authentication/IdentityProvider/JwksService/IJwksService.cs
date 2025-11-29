using MicroM.Configuration;
using MicroM.Web.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Headers;

namespace MicroM.Web.Authentication.SSO;

public interface IJwksService
{
    public EtagCacheServiceCacheCheckResult<OIDCJwksResponse>? HandleJwks(ApplicationOption app, string request_base, RequestHeaders request_headers, IHeaderDictionary reponse_headers);
}
