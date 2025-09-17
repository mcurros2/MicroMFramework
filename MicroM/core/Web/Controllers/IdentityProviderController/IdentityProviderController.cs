using MicroM.Configuration;
using MicroM.Web.Authentication;
using MicroM.Web.Authentication.SSO;
using MicroM.Web.Services;
using MicroM.Web.Services.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Microsoft.Net.Http.Headers;
using static MicroM.Web.Controllers.MicroMControllersMessages;

namespace MicroM.Web.Controllers;

[ApiController]
public class IdentityProviderController : ControllerBase, IIdentityProviderController
{
    private readonly MicroMOptions _microm_options;
    private readonly PathString _api_path;

    public IdentityProviderController(IOptions<MicroMOptions> microm_options)
    {
        _microm_options = microm_options.Value;
        _api_path = new PathString($"/{_microm_options.MicroMAPIBaseRootPath?.Trim('/')}");
    }


    [AllowAnonymous]
    [HttpGet("{app_id}/oidc/.well-known/openid-configuration")]
    public ActionResult WellKnown([FromServices] IMicroMAppConfiguration app_config, [FromServices] IIdentityProviderService idp, string app_id, CancellationToken ct)
    {
        try
        {
            var app = app_config.GetAppConfiguration(app_id);
            if (app == null) return NotFound(APPLICATION_NOT_FOUND);

            string requestBase = $"{Request.Scheme}://{Request.Host.Value}{_api_path}/{app_id}";

            var result = idp.HandleWellKnown(app, requestBase, ct);

            if (result == null)
            {
                return BadRequest("Failed to build OpenID configuration");
            }

            var etag = new EntityTagHeaderValue($"\"{result.Etag}\"");
            var respTyped = Response.GetTypedHeaders();
            respTyped.ETag = etag;
            respTyped.CacheControl = new CacheControlHeaderValue()
            {
                Public = true,
                MaxAge = TimeSpan.FromSeconds(300),
                SharedMaxAge = TimeSpan.FromSeconds(300),
                MustRevalidate = true
            };

            var reqTyped = Request.GetTypedHeaders();
            var none_match = reqTyped.IfNoneMatch;

            if (none_match.Count > 0 && none_match.Contains(etag))
            {
                return StatusCode(StatusCodes.Status304NotModified);
            }

            return Content(result.Content, "application/json");
        }
        catch (Exception ex) when (ex is TaskCanceledException || ex is OperationCanceledException || (ex.InnerException is TaskCanceledException || ex.InnerException is OperationCanceledException))
        {
            return Conflict(OPERATION_CANCELLED);
        }

    }

    [AllowAnonymous]
    [HttpGet("{app_id}/oidc/jwks")]
    public ActionResult Jwks([FromServices] IMicroMAppConfiguration app_config, [FromServices] IIdentityProviderService idp, string app_id, CancellationToken ct)
    {
        try
        {
            var app = app_config.GetAppConfiguration(app_id);
            if (app == null) return NotFound(APPLICATION_NOT_FOUND);

            var result = idp.HandleJwks(app, ct);

            if (result == null)
            {
                return BadRequest("Failed to build JWKS configuration");
            }

            var etag = new EntityTagHeaderValue($"\"{result.Etag}\"");
            var respTyped = Response.GetTypedHeaders();
            respTyped.ETag = etag;
            respTyped.CacheControl = new CacheControlHeaderValue()
            {
                Public = true,
                MaxAge = TimeSpan.FromSeconds(300),
                SharedMaxAge = TimeSpan.FromSeconds(300),
                MustRevalidate = true
            };
            var reqTyped = Request.GetTypedHeaders();
            var none_match = reqTyped.IfNoneMatch;
            if (none_match.Count > 0 && none_match.Contains(etag))
            {
                return StatusCode(StatusCodes.Status304NotModified);
            }
            return Content(result.Content, "application/json");
        }
        catch (Exception ex) when (ex is TaskCanceledException || ex is OperationCanceledException || (ex.InnerException is TaskCanceledException || ex.InnerException is OperationCanceledException))
        {
            return Conflict(OPERATION_CANCELLED);
        }
    }

    [Authorize(policy: nameof(MicroMPermissionsConstants.MicroMPermissionsPolicy))]
    [HttpGet("{app_id}/oauth2/authorize")]
    public Task<string> Authorize([FromServices] IAuthenticationProvider auth, [FromServices] IMicroMAppConfiguration app_config, string app_id, [FromBody] string userId, CancellationToken ct)
    {
        throw new NotImplementedException();
    }

    [Authorize(policy: nameof(MicroMPermissionsConstants.MicroMPermissionsPolicy))]
    [HttpPost("{app_id}/oauth2/token")]
    public Task<string> Token([FromServices] IAuthenticationProvider auth, [FromServices] IMicroMAppConfiguration app_config, string app_id, [FromBody] string userId, CancellationToken ct)
    {
        throw new NotImplementedException();
    }

    [Authorize(policy: nameof(MicroMPermissionsConstants.MicroMPermissionsPolicy))]
    [HttpPost("{app_id}/oauth2/userinfo")]
    public Task<Dictionary<string, object?>> UserInfo([FromServices] IAuthenticationProvider auth, [FromServices] IMicroMAppConfiguration app_config, string app_id, [FromBody] string userId, CancellationToken ct)
    {
        throw new NotImplementedException();
    }

    [Authorize(policy: nameof(MicroMPermissionsConstants.MicroMPermissionsPolicy))]
    [HttpPost("{app_id}/oauth2/par")]
    public Task<Dictionary<string, object?>> PAR([FromServices] IAuthenticationProvider auth, [FromServices] IMicroMAppConfiguration app_config, string app_id, [FromBody] string token, CancellationToken ct)
    {
        throw new NotImplementedException();
    }

    [Authorize(policy: nameof(MicroMPermissionsConstants.MicroMPermissionsPolicy))]
    [HttpPost("{app_id}/oauth2/endsession")]
    public Task<bool> EndSession([FromServices] IAuthenticationProvider auth, [FromServices] IMicroMAppConfiguration app_config, string app_id, [FromBody] string userId, CancellationToken ct)
    {
        throw new NotImplementedException();
    }

    [Authorize(policy: nameof(MicroMPermissionsConstants.MicroMPermissionsPolicy))]
    [HttpPost("{app_id}/oauth2/revoke")]
    public Task<bool> Revoke([FromServices] IAuthenticationProvider auth, [FromServices] IMicroMAppConfiguration app_config, string app_id, [FromBody] string token, CancellationToken ct)
    {
        throw new NotImplementedException();
    }

    [Authorize(policy: nameof(MicroMPermissionsConstants.MicroMPermissionsPolicy))]
    [HttpPost("{app_id}/oauth2/introspect")]

    public Task<Dictionary<string, object?>> Introspect([FromServices] IAuthenticationProvider auth, [FromServices] IMicroMAppConfiguration app_config, string app_id, [FromBody] string token, CancellationToken ct)
    {
        throw new NotImplementedException();
    }
}
