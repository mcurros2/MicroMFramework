using MicroM.Configuration;
using Microsoft.AspNetCore.Http;

namespace MicroM.Web.Authentication.SSO;

public interface IOauthTokenService
{
    (OIDCTokenResponse? response, object? error) HandleTokenRequest(ApplicationOption app, IFormCollection form, string authenticated_client_id);
}