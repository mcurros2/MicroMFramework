using MicroM.Configuration;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Headers;

namespace MicroM.Web.Authentication.SSO;

public interface IJwksService
{
    public EtagCacheServiceCacheCheckResult? HandleJwks(ApplicationOption app, RequestHeaders request_headers, IHeaderDictionary reponse_headers);
}
