using MicroM.Web.Services;
using MicroM.Web.Services.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MicroM.Web.Controllers;

[ApiController]
public class IdentityProviderClientController : IIdentityProviderClientController
{
    /// <summary>
    /// Expose clients certificates for OIDC PAR
    /// </summary>
    /// <param name="app_config"></param>
    /// <param name="app_id"></param>
    /// <param name="ct"></param>
    /// <returns></returns>
    /// <exception cref="NotImplementedException"></exception>
    [AllowAnonymous]
    [HttpGet("{app_id}/oidc-client/jwks")]
    public Task<string> Jwks(IMicroMAppConfiguration app_config, string app_id, CancellationToken ct)
    {
        throw new NotImplementedException();
    }

    /// <summary>
    /// OIDC client Backchannel login
    /// </summary>
    /// <param name="app_id"></param>
    /// <param name="returnUrl"></param>
    /// <param name="ct"></param>
    /// <returns></returns>
    /// <exception cref="NotImplementedException"></exception>
    [Authorize(policy: nameof(MicroMPermissionsConstants.MicroMPermissionsPolicy))]
    [HttpPost("{app_id}/oidc-client/login")]
    public Task SignInOidc(string app_id, string? returnUrl, CancellationToken ct)
    {
        throw new NotImplementedException();
    }

    /// <summary>
    /// OIDC client Backchannel logout
    /// </summary>
    /// <param name="app_id"></param>
    /// <param name="returnUrl"></param>
    /// <param name="ct"></param>
    /// <returns></returns>
    /// <exception cref="NotImplementedException"></exception>
    [Authorize(policy: nameof(MicroMPermissionsConstants.MicroMPermissionsPolicy))]
    [HttpPost("{app_id}/oidc-client/logout")]

    public Task SignOutOidc(string app_id, string? returnUrl, CancellationToken ct)
    {
        throw new NotImplementedException();
    }
}
