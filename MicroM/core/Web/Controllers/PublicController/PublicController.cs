using MicroM.Data;
using MicroM.Web.Authentication;
using MicroM.Web.Services;
using MicroM.Web.Services.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using static MicroM.Web.Controllers.MicroMControllersMessages;

namespace MicroM.Web.Controllers;

/// <summary>
/// Represents the PublicController.
/// </summary>
[ApiController]
/// <summary>
/// Represents the PublicController.
/// </summary>
public class PublicController : ControllerBase, IPublicController
{
    const string PUBLIC_USERNAME = "public";

    /// <summary>
    /// Performs the GetStatus operation.
    /// </summary>
    [AllowAnonymous]
    [HttpGet("public-api-status")]
    public string GetStatus()
    {
        return "OK";
    }

    /// <summary>
    /// Performs the PublicAction operation.
    /// </summary>
    [AllowAnonymous]
    [PublicEndpoint]
    [HttpPost("{app_id}/public/{entityName}/action/{actionName}")]
    public async Task<ObjectResult> PublicAction([FromServices] IMicroMAppConfiguration app_config, [FromServices] IEntitiesService ents, string app_id, string entityName, string actionName, [FromBody] DataWebAPIRequest parms, CancellationToken ct)
    {
        try
        {
            var app = app_config.GetAppConfiguration(app_id);
            if (app == null) return BadRequest(APPLICATION_NOT_FOUND);

            parms.ServerClaims ??= [];
            parms.ServerClaims[MicroMServerClaimTypes.MicroMUsername] = PUBLIC_USERNAME;


            using var ec = await ents.CreateDbConnection(app, parms.ServerClaims, ct);

            var result = await ents.HandleExecuteAction(app, entityName, actionName, parms, ec, ct);

            if (result != null)
            {
                return Ok(result);
            }
            return BadRequest("");
        }
        catch (Exception ex) when (ex is TaskCanceledException || ex is OperationCanceledException || (ex.InnerException is TaskCanceledException || ex.InnerException is OperationCanceledException))
        {
            return Conflict(OPERATION_CANCELLED);
        }
    }


    /// <summary>
    /// Performs the PublicDelete operation.
    /// </summary>
    [AllowAnonymous]
    [PublicEndpoint]
    [HttpPost("{app_id}/public/{entityName}/delete")]
    public async Task<ObjectResult> PublicDelete([FromServices] IMicroMAppConfiguration app_config, [FromServices] IEntitiesService ents, string app_id, string entityName, [FromBody] DataWebAPIRequest parms, CancellationToken ct)
    {
        try
        {
            var app = app_config.GetAppConfiguration(app_id);
            if (app == null) return BadRequest(APPLICATION_NOT_FOUND);

            parms.ServerClaims ??= [];
            parms.ServerClaims[MicroMServerClaimTypes.MicroMUsername] = PUBLIC_USERNAME;
            using var ec = await ents.CreateDbConnection(app, parms.ServerClaims, ct);
            var result = await ents.HandleDeleteEntity(app, entityName, parms, ec, ct);
            if (result != null)
            {
                return Ok(result);
            }
            return BadRequest("");
        }
        catch (Exception ex) when (ex is TaskCanceledException || ex is OperationCanceledException || (ex.InnerException is TaskCanceledException || ex.InnerException is OperationCanceledException))
        {
            return Conflict(OPERATION_CANCELLED);
        }
    }

    /// <summary>
    /// Performs the PublicGet operation.
    /// </summary>
    [AllowAnonymous]
    [PublicEndpoint]
    [HttpPost("{app_id}/public/{entityName}/get")]
    public async Task<ObjectResult> PublicGet([FromServices] IMicroMAppConfiguration app_config, [FromServices] IEntitiesService ents, string app_id, string entityName, [FromBody] DataWebAPIRequest parms, CancellationToken ct)
    {
        try
        {
            var app = app_config.GetAppConfiguration(app_id);
            if (app == null) return BadRequest(APPLICATION_NOT_FOUND);

            parms.ServerClaims ??= [];
            parms.ServerClaims[MicroMServerClaimTypes.MicroMUsername] = PUBLIC_USERNAME;

            using var ec = await ents.CreateDbConnection(app, parms.ServerClaims, ct);
            var result = await ents.HandleGetEntity(app, entityName, parms, ec, ct);

            if (result != null)
            {
                return Ok(result);
            }

            return BadRequest("");
        }
        catch (Exception ex) when (ex is TaskCanceledException || ex is OperationCanceledException || (ex.InnerException is TaskCanceledException || ex.InnerException is OperationCanceledException))
        {
            return Conflict(OPERATION_CANCELLED);
        }
    }

    /// <summary>
    /// Performs the PublicInsert operation.
    /// </summary>
    [AllowAnonymous]
    [PublicEndpoint]
    [HttpPost("{app_id}/public/{entityName}/insert")]
    public async Task<ObjectResult> PublicInsert([FromServices] IMicroMAppConfiguration app_config, [FromServices] IEntitiesService ents, [FromBody] DataWebAPIRequest parms, string app_id, string entityName, CancellationToken ct)
    {
        try
        {
            var app = app_config.GetAppConfiguration(app_id);
            if (app == null) return BadRequest(APPLICATION_NOT_FOUND);

            parms.ServerClaims ??= [];
            parms.ServerClaims[MicroMServerClaimTypes.MicroMUsername] = PUBLIC_USERNAME;
            using var ec = await ents.CreateDbConnection(app, parms.ServerClaims, ct);
            var result = await ents.HandleInsertEntity(app, entityName, parms, ec, ct);

            if (result != null)
            {
                return Ok(result);
            }

            return BadRequest("");
        }
        catch (Exception ex) when (ex is TaskCanceledException || ex is OperationCanceledException || (ex.InnerException is TaskCanceledException || ex.InnerException is OperationCanceledException))
        {
            return Conflict(OPERATION_CANCELLED);
        }
    }

    /// <summary>
    /// Performs the PublicLookup operation.
    /// </summary>
    [AllowAnonymous]
    [PublicEndpoint]
    [HttpPost("{app_id}/public/{entityName}/lookup/{lookupName?}")]
    public async Task<ObjectResult> PublicLookup([FromServices] IMicroMAppConfiguration app_config, [FromServices] IEntitiesService ents, string app_id, string entityName, string? lookupName, [FromBody] DataWebAPIRequest parms, CancellationToken ct)
    {
        try
        {
            var app = app_config.GetAppConfiguration(app_id);
            if (app == null) return BadRequest(APPLICATION_NOT_FOUND);

            parms.ServerClaims ??= [];
            parms.ServerClaims[MicroMServerClaimTypes.MicroMUsername] = PUBLIC_USERNAME;
            using var ec = await ents.CreateDbConnection(app, parms.ServerClaims, ct);
            var result = await ents.HandleLookupEntity(app, entityName, parms, ec, ct, lookupName);

            return Ok(result);
        }
        catch (Exception ex) when (ex is TaskCanceledException || ex is OperationCanceledException || (ex.InnerException is TaskCanceledException || ex.InnerException is OperationCanceledException))
        {
            return Conflict(OPERATION_CANCELLED);
        }
    }

    /// <summary>
    /// Performs the PublicProc operation.
    /// </summary>
    [AllowAnonymous]
    [PublicEndpoint]
    [HttpPost("{app_id}/public/{entityName}/proc/{procName}")]
    public async Task<ObjectResult> PublicProc([FromServices] IMicroMAppConfiguration app_config, [FromServices] IEntitiesService ents, string app_id, string entityName, string procName, [FromBody] DataWebAPIRequest parms, CancellationToken ct)
    {
        try
        {
            var app = app_config.GetAppConfiguration(app_id);
            if (app == null) return BadRequest(APPLICATION_NOT_FOUND);

            parms.ServerClaims ??= [];
            parms.ServerClaims[MicroMServerClaimTypes.MicroMUsername] = PUBLIC_USERNAME;
            using var ec = await ents.CreateDbConnection(app, parms.ServerClaims, ct);
            var result = await ents.HandleExecuteProc(app, entityName, procName, parms, ec, ct);
            if (result != null)
            {
                return Ok(result);
            }
            return BadRequest("");
        }
        catch (Exception ex) when (ex is TaskCanceledException || ex is OperationCanceledException || (ex.InnerException is TaskCanceledException || ex.InnerException is OperationCanceledException))
        {
            return Conflict(OPERATION_CANCELLED);
        }
    }

    /// <summary>
    /// Performs the PublicProcess operation.
    /// </summary>
    [AllowAnonymous]
    [PublicEndpoint]
    [HttpPost("{app_id}/public/{entityName}/process/{procName}")]
    public async Task<ObjectResult> PublicProcess([FromServices] IMicroMAppConfiguration app_config, [FromServices] IEntitiesService ents, string app_id, string entityName, string procName, [FromBody] DataWebAPIRequest parms, CancellationToken ct)
    {
        try
        {
            var app = app_config.GetAppConfiguration(app_id);
            if (app == null) return BadRequest(APPLICATION_NOT_FOUND);

            parms.ServerClaims ??= [];
            parms.ServerClaims[MicroMServerClaimTypes.MicroMUsername] = PUBLIC_USERNAME;
            using var ec = await ents.CreateDbConnection(app, parms.ServerClaims, ct);
            var result = await ents.HandleExecuteProcDBStatus(app, entityName, procName, parms, ec, ct);
            if (result != null)
            {
                return Ok(result);
            }
            return BadRequest("");
        }
        catch (Exception ex) when (ex is TaskCanceledException || ex is OperationCanceledException || (ex.InnerException is TaskCanceledException || ex.InnerException is OperationCanceledException))
        {
            return Conflict(OPERATION_CANCELLED);
        }
    }

    /// <summary>
    /// Performs the PublicUpdate operation.
    /// </summary>
    [AllowAnonymous]
    [PublicEndpoint]
    [HttpPost("{app_id}/public/{entityName}/update")]
    public async Task<ObjectResult> PublicUpdate([FromServices] IMicroMAppConfiguration app_config, [FromServices] IEntitiesService ents, string app_id, string entityName, [FromBody] DataWebAPIRequest parms, CancellationToken ct)
    {
        try
        {
            var app = app_config.GetAppConfiguration(app_id);
            if (app == null) return BadRequest(APPLICATION_NOT_FOUND);

            parms.ServerClaims ??= [];
            parms.ServerClaims[MicroMServerClaimTypes.MicroMUsername] = PUBLIC_USERNAME;
            using var ec = await ents.CreateDbConnection(app, parms.ServerClaims, ct);
            var result = await ents.HandleUpdateEntity(app, entityName, parms, ec, ct);

            if (result != null)
            {
                return Ok(result);
            }

            return BadRequest("");
        }
        catch (Exception ex) when (ex is TaskCanceledException || ex is OperationCanceledException || (ex.InnerException is TaskCanceledException || ex.InnerException is OperationCanceledException))
        {
            return Conflict(OPERATION_CANCELLED);
        }
    }

    /// <summary>
    /// Performs the PublicView operation.
    /// </summary>
    [AllowAnonymous]
    [PublicEndpoint]
    [HttpPost("{app_id}/public/{entityName}/view/{viewName}")]
    public async Task<ObjectResult> PublicView([FromServices] IMicroMAppConfiguration app_config, [FromServices] IEntitiesService ents, string app_id, string entityName, string viewName, [FromBody] DataWebAPIRequest parms, CancellationToken ct)
    {
        try
        {
            var app = app_config.GetAppConfiguration(app_id);
            if (app == null) return BadRequest(APPLICATION_NOT_FOUND);

            parms.ServerClaims ??= [];
            parms.ServerClaims[MicroMServerClaimTypes.MicroMUsername] = PUBLIC_USERNAME;
            using var ec = await ents.CreateDbConnection(app, parms.ServerClaims, ct);
            var result = await ents.HandleExecuteView(app, entityName, viewName, parms, ec, ct);
            if (result != null)
            {
                return Ok(result);
            }
            return BadRequest("");
        }
        catch (Exception ex) when (ex is TaskCanceledException || ex is OperationCanceledException || (ex.InnerException is TaskCanceledException || ex.InnerException is OperationCanceledException))
        {
            return Conflict(OPERATION_CANCELLED);
        }
    }
}
