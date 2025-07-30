using MicroM.Data;
using MicroM.Web.Authentication;
using MicroM.Web.Services;
using MicroM.Web.Services.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MicroM.Web.Controllers;

[ApiController]
public class EntitiesController : ControllerBase, IEntitiesController
{
    [AllowAnonymous]
    [HttpGet("entities-api-status")]
    public string GetStatus()
    {
        return "OK";
    }

    [Authorize(policy: nameof(MicroMPermissionsConstants.MicroMPermissionsPolicy))]
    [HttpPost("{app_id}/{entityName}/action/{actionName}")]
    public async Task<ObjectResult> Action([FromServices] IAuthenticationProvider auth, [FromServices] IEntitiesService ents, string app_id, string entityName, string actionName, [FromBody] DataWebAPIRequest parms, CancellationToken ct)
    {
        try
        {
            parms.ServerClaims = User.Claims.ToClaimsDictionary();
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

    [Authorize(policy: nameof(MicroMPermissionsConstants.MicroMPermissionsPolicy))]
    [HttpPost("{app_id}/{entityName}/delete")]
    public async Task<ObjectResult> Delete([FromServices] IAuthenticationProvider auth, [FromServices] IEntitiesService ents, string app_id, string entityName, [FromBody] DataWebAPIRequest parms, CancellationToken ct)
    {
        try
        {
            parms.ServerClaims = User.Claims.ToClaimsDictionary();
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


    [Authorize(policy: nameof(MicroMPermissionsConstants.MicroMPermissionsPolicy))]
    [HttpPost("{app_id}/{entityName}/get")]
    public async Task<ObjectResult> Get([FromServices] IAuthenticationProvider auth, [FromServices] IEntitiesService ents, string app_id, string entityName, [FromBody] DataWebAPIRequest parms, CancellationToken ct)
    {
        try
        {
            parms.ServerClaims = User.Claims.ToClaimsDictionary();

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



    [Authorize(policy: nameof(MicroMPermissionsConstants.MicroMPermissionsPolicy))]
    [HttpGet("{app_id}/{entityName}/definition")]
    public ObjectResult GetDefinition([FromServices] IEntitiesService ents, string app_id, string entityName)
    {
        try
        {
            var result = ents.HandleGetEntityDefinition(app_id, entityName);
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


    [Authorize(policy: nameof(MicroMPermissionsConstants.MicroMPermissionsPolicy))]
    [HttpPost("{app_id}/timezoneoffset")]
    public async Task<int> GetTimeZoneOffset([FromServices] IAuthenticationProvider auth, [FromServices] IEntitiesService ents, string app_id, CancellationToken ct)
    {
        try
        {
            var serverClaims = User.Claims.ToClaimsDictionary();
            using var ec = await ents.CreateDbConnection(app_id, serverClaims, auth, ct);

            var result = await ents.HandleGetTimeZoneOffset(auth, app_id, ec, ct);

            return result;
        }
        catch (Exception ex) when (ex is TaskCanceledException || ex is OperationCanceledException)
        {
            return 0;
        }

    }

    [Authorize(policy: nameof(MicroMPermissionsConstants.MicroMPermissionsPolicy))]
    [HttpPost("{app_id}/{entityName}/import/{import_proc?}")]
    public async Task<ObjectResult> Import([FromServices] IAuthenticationProvider auth, [FromServices] IEntitiesService ents, string app_id, string entityName, string? import_proc, [FromBody] DataWebAPIRequest parms, CancellationToken ct)
    {
        try
        {
            parms.ServerClaims = User.Claims.ToClaimsDictionary();
            using var ec = await ents.CreateDbConnection(app_id, parms.ServerClaims, auth, ct);

            var result = await ents.HandleImportData(auth, app_id, entityName, import_proc, parms, ec, ct);

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

    [Authorize(policy: nameof(MicroMPermissionsConstants.MicroMPermissionsPolicy))]
    [HttpPost("{app_id}/{entityName}/insert")]
    public async Task<ObjectResult> Insert(
        [FromServices] IAuthenticationProvider auth,
        [FromServices] IEntitiesService ents,
        [FromBody] DataWebAPIRequest parms,
        string app_id, string entityName,
        CancellationToken ct)
    {
        try
        {
            parms.ServerClaims = User.Claims.ToClaimsDictionary();
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


    [Authorize(policy: nameof(MicroMPermissionsConstants.MicroMPermissionsPolicy))]
    [HttpPost("{app_id}/{entityName}/lookup/{lookupName?}")]
    public async Task<ObjectResult> Lookup([FromServices] IAuthenticationProvider auth, [FromServices] IEntitiesService ents, string app_id, string entityName, string? lookupName, [FromBody] DataWebAPIRequest parms, CancellationToken ct)
    {
        try
        {
            parms.ServerClaims = User.Claims.ToClaimsDictionary();
            using var ec = await ents.CreateDbConnection(app_id, parms.ServerClaims, auth, ct);
            var result = await ents.HandleLookupEntity(auth, app_id, entityName, parms, ec, ct, lookupName);

            return Ok(result);
        }
        catch (Exception ex) when (ex is TaskCanceledException || ex is OperationCanceledException)
        {
            return Ok(null);
        }
    }

    [Authorize(policy: nameof(MicroMPermissionsConstants.MicroMPermissionsPolicy))]
    [HttpPost("{app_id}/{entityName}/proc/{procName}")]
    public async Task<ObjectResult> Proc([FromServices] IAuthenticationProvider auth, [FromServices] IEntitiesService ents, string app_id, string entityName, string procName, [FromBody] DataWebAPIRequest parms, CancellationToken ct)
    {
        try
        {
            parms.ServerClaims = User.Claims.ToClaimsDictionary();
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

    [Authorize(policy: nameof(MicroMPermissionsConstants.MicroMPermissionsPolicy))]
    [HttpPost("{app_id}/{entityName}/process/{procName}")]
    public async Task<ObjectResult> Process([FromServices] IAuthenticationProvider auth, [FromServices] IEntitiesService ents, string app_id, string entityName, string procName, [FromBody] DataWebAPIRequest parms, CancellationToken ct)
    {
        try
        {
            parms.ServerClaims = User.Claims.ToClaimsDictionary();
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

    [Authorize(policy: nameof(MicroMPermissionsConstants.MicroMPermissionsPolicy))]
    [HttpPost("{app_id}/{entityName}/update")]
    public async Task<ObjectResult> Update([FromServices] IAuthenticationProvider auth, [FromServices] IEntitiesService ents, string app_id, string entityName, [FromBody] DataWebAPIRequest parms, CancellationToken ct)
    {
        try
        {
            parms.ServerClaims = User.Claims.ToClaimsDictionary();
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

    [Authorize(policy: nameof(MicroMPermissionsConstants.MicroMPermissionsPolicy))]
    [HttpPost("{app_id}/{entityName}/view/{viewName}")]
    public async Task<ObjectResult> View([FromServices] IAuthenticationProvider auth, [FromServices] IEntitiesService ents, string app_id, string entityName, string viewName, [FromBody] DataWebAPIRequest parms, CancellationToken ct)
    {
        try
        {
            parms.ServerClaims = User.Claims.ToClaimsDictionary();
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
