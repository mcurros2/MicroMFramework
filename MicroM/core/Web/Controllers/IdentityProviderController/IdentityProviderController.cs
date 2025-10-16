using MicroM.Configuration;
using MicroM.DataDictionary.CategoriesDefinitions;
using MicroM.Web.Authentication;
using MicroM.Web.Authentication.SSO;
using MicroM.Web.Services;
using MicroM.Web.Services.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using static MicroM.Web.Controllers.MicroMControllersMessages;

namespace MicroM.Web.Controllers;

[ApiController]
public class IdentityProviderController : ControllerBase, IIdentityProviderController
{
    private readonly MicroMOptions _microm_options;
    private readonly PathString _api_path;
    private readonly ILogger<IdentityProviderController> _log;

    public IdentityProviderController(IOptions<MicroMOptions> microm_options, ILogger<IdentityProviderController> log)
    {
        _microm_options = microm_options.Value;
        _api_path = new PathString($"/{_microm_options.MicroMAPIBaseRootPath?.Trim('/')}");
        _log = log;
    }

    [AllowAnonymous]
    [HttpGet("{app_id}/oidc/.well-known/openid-configuration")]
    public ActionResult WellKnown([FromServices] IMicroMAppConfiguration app_config, [FromServices] IIdentityProviderService idp, string app_id, CancellationToken ct)
    {
        try
        {
            var app = app_config.GetAppConfiguration(app_id);
            if (app == null) return NotFound(APPLICATION_NOT_FOUND);

            if (app.IdentityProviderRoleType != nameof(IdentityProviderRole.IDPServer))
            {
                _log.LogWarning("Application {app_id} is not configured as an Identity Provider", app_id);
                return BadRequest("Application is not configured as an Identity Provider");
            }

            var request_headers = Request.GetTypedHeaders();
            var response_headers = Response.GetTypedHeaders();

            string requestBase = $"{Request.Scheme}://{Request.Host.Value}{_api_path}/{app_id}";

            var result = idp.HandleWellKnown(app, requestBase, request_headers, response_headers.Headers);

            if (result.is_cached)
            {
                return StatusCode(StatusCodes.Status304NotModified);
            }

            return Content(result.etag_content.Content, "application/json");
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

            if (app.IdentityProviderRoleType != nameof(IdentityProviderRole.IDPServer))
            {
                _log.LogWarning("Application {app_id} is not configured as an Identity Provider", app_id);
                return BadRequest("Application is not configured as an Identity Provider");
            }

            var request_headers = Request.GetTypedHeaders();
            var response_headers = Response.GetTypedHeaders();

            var result = idp.HandleJwks(app, request_headers, response_headers.Headers);

            if (result == null)
            {
                return NotFound();
            }

            if (result.is_cached)
            {
                return StatusCode(StatusCodes.Status304NotModified);
            }

            return Content(result.etag_content.Content, "application/json");

        }
        catch (Exception ex) when (ex is TaskCanceledException || ex is OperationCanceledException || (ex.InnerException is TaskCanceledException || ex.InnerException is OperationCanceledException))
        {
            return Conflict(OPERATION_CANCELLED);
        }
    }

    [Authorize(Policy = nameof(MicroMPermissionsConstants.IdPClientPolicy))]
    [HttpPost("{app_id}/oauth2/token")]
    public async Task<ActionResult> Token([FromServices] IMicroMAppConfiguration app_config, [FromServices] IIdentityProviderService idp, string app_id, CancellationToken ct)
    {
        try
        {
            var app = app_config.GetAppConfiguration(app_id);
            if (app == null) return NotFound(APPLICATION_NOT_FOUND);

            if (app.IdentityProviderRoleType != nameof(IdentityProviderRole.IDPServer))
            {
                _log.LogWarning("Application {app_id} is not configured as an Identity Provider", app_id);
                return BadRequest("Application is not configured as an Identity Provider");
            }

            if (!Request.HasFormContentType)
            {
                return BadRequest(new { error = "invalid_request", error_description = "Request must be application/x-www-form-urlencoded" });
            }

            var form = await Request.ReadFormAsync(ct);

            var (response, error) = await idp.HandleToken(app, form, User, ct);

            if (error != null || response == null)
            {
                return BadRequest(error);
            }

            return Ok(response);
        }
        catch (Exception ex) when (ex is TaskCanceledException || ex is OperationCanceledException)
        {
            return Conflict(OPERATION_CANCELLED);
        }
    }

    [Authorize(Policy = nameof(MicroMPermissionsConstants.IdPClientPolicy))]
    [HttpPost("{app_id}/oauth2/par")]
    public async Task<ActionResult> PAR([FromServices] IMicroMAppConfiguration app_config, [FromServices] IIdentityProviderService idp, string app_id, CancellationToken ct)
    {
        try
        {
            var app = app_config.GetAppConfiguration(app_id);
            if (app == null) return NotFound(APPLICATION_NOT_FOUND);

            if (!Request.HasFormContentType)
            {
                return BadRequest(new { error = "invalid_request", error_description = "Request must be application/x-www-form-urlencoded" });
            }

            var form = await Request.ReadFormAsync(ct);

            var (response, error) = idp.HandlePAR(app, form, User);

            if (error != null || response == null)
            {
                return BadRequest(error);
            }

            return Ok(response);
        }
        catch (Exception ex) when (ex is TaskCanceledException || ex is OperationCanceledException)
        {
            return Conflict(OPERATION_CANCELLED);
        }
    }

    [Authorize(Policy = nameof(MicroMPermissionsConstants.IdPClientPolicy))]
    [HttpGet("{app_id}/oauth2/authorize")]
    public async Task<ActionResult> Authorize([FromServices] IMicroMAppConfiguration app_config, [FromServices] IIdentityProviderService idp, string app_id, CancellationToken ct)
    {
        try
        {
            var app = app_config.GetAppConfiguration(app_id);
            if (app == null) return NotFound(APPLICATION_NOT_FOUND);

            string requestBase = $"{Request.Scheme}://{Request.Host.Value}{_api_path}/{app_id}";

            var (result, error) = await idp.HandleAuthorize(app, Request.Query, User, requestBase, ct);


            if (error != null)
            {
                return BadRequest(error);
            }

            if (result == null)
            {
                return BadRequest(new { error = "server_error", error_description = "No result produced by authorize flow" });
            }

            var (redirectUrl, loginUrl) = result;

            if (!string.IsNullOrEmpty(loginUrl))
            {
                return Redirect(loginUrl);
            }

            if (!string.IsNullOrEmpty(redirectUrl))
            {
                return Redirect(redirectUrl);
            }

            return BadRequest(new { error = "server_error", error_description = "No action produced by authorize flow" });
        }
        catch (Exception ex) when (ex is TaskCanceledException || ex is OperationCanceledException)
        {
            return Conflict(OPERATION_CANCELLED);
        }
    }

    [Authorize(Policy = nameof(MicroMPermissionsConstants.IdPClientPolicy))]
    [HttpPost("{app_id}/oauth2/userinfo")]
    public Task<Dictionary<string, object?>> UserInfo([FromServices] IAuthenticationProvider auth, [FromServices] IMicroMAppConfiguration app_config, string app_id, [FromBody] string userId, CancellationToken ct)
    {
        throw new NotImplementedException();
    }

    [Authorize(Policy = nameof(MicroMPermissionsConstants.IdPClientPolicy))]
    [HttpPost("{app_id}/oauth2/endsession")]
    public Task<bool> EndSession([FromServices] IAuthenticationProvider auth, [FromServices] IMicroMAppConfiguration app_config, string app_id, [FromBody] string userId, CancellationToken ct)
    {
        throw new NotImplementedException();
    }

    [Authorize(Policy = nameof(MicroMPermissionsConstants.IdPClientPolicy))]
    [HttpPost("{app_id}/oauth2/revoke")]
    public Task<bool> Revoke([FromServices] IAuthenticationProvider auth, [FromServices] IMicroMAppConfiguration app_config, string app_id, [FromBody] string token, CancellationToken ct)
    {
        throw new NotImplementedException();
    }

    [Authorize(Policy = nameof(MicroMPermissionsConstants.IdPClientPolicy))]
    [HttpPost("{app_id}/oauth2/introspect")]
    public Task<Dictionary<string, object?>> Introspect([FromServices] IAuthenticationProvider auth, [FromServices] IMicroMAppConfiguration app_config, string app_id, [FromBody] string token, CancellationToken ct)
    {
        throw new NotImplementedException();
    }
}
