using MicroM.Web.Authentication;
using MicroM.Web.Services;
using MicroM.Web.Services.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MicroM.Web.Controllers;

[ApiController]
public class IdentityProviderController : ControllerBase, IIdentityProviderController
{
    [AllowAnonymous]
    [HttpPost("{app_id}/oidc/.well-known/openid-configuration")]
    public Task<Dictionary<string, string>> WellKnown([FromServices] IAuthenticationProvider auth, [FromServices] IMicroMAppConfiguration app_config, string app_id, CancellationToken ct)
    {
        throw new NotImplementedException();
    }

    [AllowAnonymous]
    [HttpGet("{app_id}/oidc/jwks")]
    public Task<string> Jwks([FromServices] IAuthenticationProvider auth, [FromServices] IMicroMAppConfiguration app_config, string app_id, CancellationToken ct)
    {
        throw new NotImplementedException();
    }

    [Authorize(policy: nameof(MicroMPermissionsConstants.MicroMPermissionsPolicy))]
    [HttpGet("{app_id}/oauth2/authorize")]
    public Task<string> Authorize([FromServices] IAuthenticationProvider auth, [FromServices] IMicroMAppConfiguration app_config, string app_id, string userId, CancellationToken ct)
    {
        throw new NotImplementedException();
    }

    [Authorize(policy: nameof(MicroMPermissionsConstants.MicroMPermissionsPolicy))]
    [HttpPost("{app_id}/oauth2/token")]
    public Task<string> Token([FromServices] IAuthenticationProvider auth, [FromServices] IMicroMAppConfiguration app_config, string app_id, string userId, CancellationToken ct)
    {
        throw new NotImplementedException();
    }

    [Authorize(policy: nameof(MicroMPermissionsConstants.MicroMPermissionsPolicy))]
    [HttpPost("{app_id}/oauth2/userinfo")]
    public Task<Dictionary<string, object?>> UserInfo([FromServices] IAuthenticationProvider auth, [FromServices] IMicroMAppConfiguration app_config, string app_id, string userId, CancellationToken ct)
    {
        throw new NotImplementedException();
    }

    [Authorize(policy: nameof(MicroMPermissionsConstants.MicroMPermissionsPolicy))]
    [HttpPost("{app_id}/oauth2/par")]
    public Task<Dictionary<string, object?>> PAR([FromServices] IAuthenticationProvider auth, [FromServices] IMicroMAppConfiguration app_config, string app_id, string token, CancellationToken ct)
    {
        throw new NotImplementedException();
    }

    [Authorize(policy: nameof(MicroMPermissionsConstants.MicroMPermissionsPolicy))]
    [HttpPost("{app_id}/oauth2/endsession")]
    public Task<bool> EndSession([FromServices] IAuthenticationProvider auth, [FromServices] IMicroMAppConfiguration app_config, string app_id, string userId, CancellationToken ct)
    {
        throw new NotImplementedException();
    }
}
