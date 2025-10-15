using MicroM.Configuration;
using MicroM.Core;
using MicroM.Web.Services;
using Microsoft.AspNetCore.Http;

namespace MicroM.Web.Authentication.SSO;

public interface IOauthTokenService
{
    Task<ResultWithStatus<OIDCTokenResponse, ErrorResult>> HandleTokenRequest(ApplicationOption app, IFormCollection form, string authenticated_client_id);
}