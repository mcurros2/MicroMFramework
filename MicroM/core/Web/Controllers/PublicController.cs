using MicroM.Data;
using MicroM.Web.Authentication;
using MicroM.Web.Services;
using MicroM.Web.Services.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MicroM.Web.Controllers;

[ApiController]
public class PublicController : ControllerBase, IPublicController
{
    [AllowAnonymous]
    [HttpGet("public-api-status")]
    public string GetStatus()
    {
        return "OK";
    }

    [AllowAnonymous]
    [PublicEndpoint]
    [HttpPost("{app_id}/public/{entityName}/action/{actionName}")]
    public async Task<ObjectResult> PublicAction([FromServices] IAuthenticationProvider auth, [FromServices] IEntitiesService ents, string app_id, string entityName, string actionName, [FromBody] DataWebAPIRequest parms, CancellationToken ct)
    {
        try
        {
            parms.ServerClaims = User.Claims.ToClaimsDictionary();
            parms.ServerClaims[MicroMServerClaimTypes.MicroMUsername] = "public";
            using var ec = await ents.CreateDbConnection(app_id, parms.ServerClaims, auth, ct);

            var result = await ents.HandleExecuteAction(auth, app_id, entityName, actionName, parms, ec, ct);

            if (result != null)
            {
                return Ok(result);
            }
            return BadRequest("");
        }
        catch (Exception ex) when (ex is TaskCanceledException || ex is OperationCanceledException)
        {
            return Ok(null);
        }
    }


    [AllowAnonymous]
    [PublicEndpoint]
    [HttpPost("{app_id}/public/{entityName}/delete")]
    public async Task<ObjectResult> PublicDelete([FromServices] IAuthenticationProvider auth, [FromServices] IEntitiesService ents, string app_id, string entityName, [FromBody] DataWebAPIRequest parms, CancellationToken ct)
    {
        try
        {
            parms.ServerClaims = User.Claims.ToClaimsDictionary();
            parms.ServerClaims[MicroMServerClaimTypes.MicroMUsername] = "public";
            using var ec = await ents.CreateDbConnection(app_id, parms.ServerClaims, auth, ct);
            var result = await ents.HandleDeleteEntity(auth, app_id, entityName, parms, ec, ct);
            if (result != null)
            {
                return Ok(result);
            }
            return BadRequest("");
        }
        catch (Exception ex) when (ex is TaskCanceledException || ex is OperationCanceledException)
        {
            return Ok(null);
        }
    }

    [AllowAnonymous]
    [PublicEndpoint]
    [HttpPost("{app_id}/public/{entityName}/get")]
    public async Task<ObjectResult> PublicGet([FromServices] IAuthenticationProvider auth, [FromServices] IEntitiesService ents, string app_id, string entityName, [FromBody] DataWebAPIRequest parms, CancellationToken ct)
    {
        try
        {
            parms.ServerClaims = User.Claims.ToClaimsDictionary();
            parms.ServerClaims[MicroMServerClaimTypes.MicroMUsername] = "public";

            using var ec = await ents.CreateDbConnection(app_id, parms.ServerClaims, auth, ct);
            var result = await ents.HandleGetEntity(auth, app_id, entityName, parms, ec, ct);

            if (result != null)
            {
                return Ok(result);
            }

            return BadRequest("");
        }
        catch (Exception ex) when (ex is TaskCanceledException || ex is OperationCanceledException)
        {
            return Ok(null);
        }
    }

    [AllowAnonymous]
    [PublicEndpoint]
    [HttpPost("{app_id}/public/{entityName}/insert")]
    public async Task<ObjectResult> PublicInsert(
        [FromServices] IAuthenticationProvider auth,
        [FromServices] IEntitiesService ents,
        [FromBody] DataWebAPIRequest parms,
        string app_id, string entityName,
        CancellationToken ct)
    {
        try
        {
            parms.ServerClaims = User.Claims.ToClaimsDictionary();
            parms.ServerClaims[MicroMServerClaimTypes.MicroMUsername] = "public";
            using var ec = await ents.CreateDbConnection(app_id, parms.ServerClaims, auth, ct);
            var result = await ents.HandleInsertEntity(auth, app_id, entityName, parms, ec, ct);

            if (result != null)
            {
                return Ok(result);
            }

            return BadRequest("");
        }
        catch (Exception ex) when (ex is TaskCanceledException || ex is OperationCanceledException)
        {
            return Ok(null);
        }
    }

    [AllowAnonymous]
    [PublicEndpoint]
    [HttpPost("{app_id}/public/{entityName}/lookup/{lookupName?}")]
    public async Task<ObjectResult> PublicLookup([FromServices] IAuthenticationProvider auth, [FromServices] IEntitiesService ents, string app_id, string entityName, string? lookupName, [FromBody] DataWebAPIRequest parms, CancellationToken ct)
    {
        try
        {
            parms.ServerClaims = User.Claims.ToClaimsDictionary();
            parms.ServerClaims[MicroMServerClaimTypes.MicroMUsername] = "public";
            using var ec = await ents.CreateDbConnection(app_id, parms.ServerClaims, auth, ct);
            var result = await ents.HandleLookupEntity(auth, app_id, entityName, parms, ec, ct, lookupName);

            return Ok(result);
        }
        catch (Exception ex) when (ex is TaskCanceledException || ex is OperationCanceledException)
        {
            return Ok(null);
        }
    }

    [AllowAnonymous]
    [PublicEndpoint]
    [HttpPost("{app_id}/public/{entityName}/proc/{procName}")]
    public async Task<ObjectResult> PublicProc([FromServices] IAuthenticationProvider auth, [FromServices] IEntitiesService ents, string app_id, string entityName, string procName, [FromBody] DataWebAPIRequest parms, CancellationToken ct)
    {
        try
        {
            parms.ServerClaims = User.Claims.ToClaimsDictionary();
            parms.ServerClaims[MicroMServerClaimTypes.MicroMUsername] = "public";
            using var ec = await ents.CreateDbConnection(app_id, parms.ServerClaims, auth, ct);
            var result = await ents.HandleExecuteProc(auth, app_id, entityName, procName, parms, ec, ct);
            if (result != null)
            {
                return Ok(result);
            }
            return BadRequest("");
        }
        catch (Exception ex) when (ex is TaskCanceledException || ex is OperationCanceledException)
        {
            return Ok(null);
        }
    }

    [AllowAnonymous]
    [PublicEndpoint]
    [HttpPost("{app_id}/public/{entityName}/process/{procName}")]
    public async Task<ObjectResult> PublicProcess([FromServices] IAuthenticationProvider auth, [FromServices] IEntitiesService ents, string app_id, string entityName, string procName, [FromBody] DataWebAPIRequest parms, CancellationToken ct)
    {
        try
        {
            parms.ServerClaims = User.Claims.ToClaimsDictionary();
            parms.ServerClaims[MicroMServerClaimTypes.MicroMUsername] = "public";
            using var ec = await ents.CreateDbConnection(app_id, parms.ServerClaims, auth, ct);
            var result = await ents.HandleExecuteProcDBStatus(auth, app_id, entityName, procName, parms, ec, ct);
            if (result != null)
            {
                return Ok(result);
            }
            return BadRequest("");
        }
        catch (Exception ex) when (ex is TaskCanceledException || ex is OperationCanceledException)
        {
            return Ok(null);
        }
    }

    [AllowAnonymous]
    [PublicEndpoint]
    [HttpPost("{app_id}/public/{entityName}/update")]
    public async Task<ObjectResult> PublicUpdate([FromServices] IAuthenticationProvider auth, [FromServices] IEntitiesService ents, string app_id, string entityName, [FromBody] DataWebAPIRequest parms, CancellationToken ct)
    {
        try
        {
            parms.ServerClaims = User.Claims.ToClaimsDictionary();
            parms.ServerClaims[MicroMServerClaimTypes.MicroMUsername] = "public";
            using var ec = await ents.CreateDbConnection(app_id, parms.ServerClaims, auth, ct);
            var result = await ents.HandleUpdateEntity(auth, app_id, entityName, parms, ec, ct);

            if (result != null)
            {
                return Ok(result);
            }

            return BadRequest("");
        }
        catch (Exception ex) when (ex is TaskCanceledException || ex is OperationCanceledException)
        {
            return Ok(null);
        }
    }

    [AllowAnonymous]
    [PublicEndpoint]
    [HttpPost("{app_id}/public/{entityName}/view/{viewName}")]
    public async Task<ObjectResult> PublicView([FromServices] IAuthenticationProvider auth, [FromServices] IEntitiesService ents, string app_id, string entityName, string viewName, [FromBody] DataWebAPIRequest parms, CancellationToken ct)
    {
        try
        {
            parms.ServerClaims = User.Claims.ToClaimsDictionary();
            parms.ServerClaims[MicroMServerClaimTypes.MicroMUsername] = "public";
            using var ec = await ents.CreateDbConnection(app_id, parms.ServerClaims, auth, ct);
            var result = await ents.HandleExecuteView(auth, app_id, entityName, viewName, parms, ec, ct);
            if (result != null)
            {
                return Ok(result);
            }
            return BadRequest("");
        }
        catch (Exception ex) when (ex is TaskCanceledException || ex is OperationCanceledException)
        {
            return Ok(null);
        }
    }
}
