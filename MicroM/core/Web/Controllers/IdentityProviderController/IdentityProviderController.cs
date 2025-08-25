using MicroM.Web.Authentication;
using MicroM.Web.Services;
using MicroM.Web.Services.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MicroM.Web.Controllers;

/// <summary>
/// Provides endpoints for OpenID Connect and OAuth2 operations.
/// </summary>
[ApiController]
public class IdentityProviderController : ControllerBase, IIdentityProviderController
{
    /// <summary>
    /// Returns the OpenID Connect discovery document.
    /// </summary>
    [AllowAnonymous]
    [HttpPost("{app_id}/oidc/.well-known/openid-configuration")]
    public Task<Dictionary<string, string>> WellKnown([FromServices] IAuthenticationProvider auth, [FromServices] IMicroMAppConfiguration app_config, string app_id, CancellationToken ct)
    {
        throw new NotImplementedException();
    }

    /// <summary>
    /// Returns the JSON Web Key Set used to validate tokens.
    /// </summary>
    [AllowAnonymous]
    [HttpGet("{app_id}/oidc/jwks")]
    public Task<string> Jwks([FromServices] IAuthenticationProvider auth, [FromServices] IMicroMAppConfiguration app_config, string app_id, CancellationToken ct)
    {
        throw new NotImplementedException();
    }

    /// <summary>
    /// Handles an authorization request.
    /// </summary>
    [Authorize(policy: nameof(MicroMPermissionsConstants.MicroMPermissionsPolicy))]
    [HttpGet("{app_id}/oauth2/authorize")]
    public Task<string> Authorize([FromServices] IAuthenticationProvider auth, [FromServices] IMicroMAppConfiguration app_config, string app_id, string userId, CancellationToken ct)
    {
        throw new NotImplementedException();
    }

    /// <summary>
    /// Issues tokens for an authorization request.
    /// </summary>
    [Authorize(policy: nameof(MicroMPermissionsConstants.MicroMPermissionsPolicy))]
    [HttpPost("{app_id}/oauth2/token")]
    public Task<string> Token([FromServices] IAuthenticationProvider auth, [FromServices] IMicroMAppConfiguration app_config, string app_id, string userId, CancellationToken ct)
    {
        throw new NotImplementedException();
    }

    /// <summary>
    /// Returns claims about the authenticated user.
    /// </summary>
    [Authorize(policy: nameof(MicroMPermissionsConstants.MicroMPermissionsPolicy))]
    [HttpPost("{app_id}/oauth2/userinfo")]
    public Task<Dictionary<string, object?>> UserInfo([FromServices] IAuthenticationProvider auth, [FromServices] IMicroMAppConfiguration app_config, string app_id, string userId, CancellationToken ct)
    {
        throw new NotImplementedException();
    }

    /// <summary>
    /// Processes a pushed authorization request.
    /// </summary>
    [Authorize(policy: nameof(MicroMPermissionsConstants.MicroMPermissionsPolicy))]
    [HttpPost("{app_id}/oauth2/par")]
    public Task<Dictionary<string, object?>> PAR([FromServices] IAuthenticationProvider auth, [FromServices] IMicroMAppConfiguration app_config, string app_id, string token, CancellationToken ct)
    {
        throw new NotImplementedException();
    }

    /// <summary>
    /// Terminates a user's session and revokes tokens.
    /// </summary>
    [Authorize(policy: nameof(MicroMPermissionsConstants.MicroMPermissionsPolicy))]
    [HttpPost("{app_id}/oauth2/endsession")]
    public Task<bool> EndSession([FromServices] IAuthenticationProvider auth, [FromServices] IMicroMAppConfiguration app_config, string app_id, string userId, CancellationToken ct)
    {
        throw new NotImplementedException();
    }
}
