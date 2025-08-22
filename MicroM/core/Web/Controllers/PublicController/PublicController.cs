using MicroM.Data;
using MicroM.Web.Authentication;
using MicroM.Web.Services;
using MicroM.Web.Services.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using static MicroM.Web.Controllers.MicroMControllersMessages;

namespace MicroM.Web.Controllers;

/// <summary>
/// Exposes unauthenticated entity operations for public clients.
/// </summary>
[ApiController]
/// <summary>
/// Exposes unauthenticated entity operations for public clients.
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
    /// Executes an unauthenticated action on the specified entity.
    /// The <see cref="PublicEndpointAttribute"/> allows public access without authentication.
    /// </summary>
    /// <param name="app_config">Application configuration provider.</param>
    /// <param name="ents">Service used to execute entity operations.</param>
    /// <param name="app_id">Identifier of the application containing the entity.</param>
    /// <param name="entityName">Name of the entity on which to execute the action.</param>
    /// <param name="actionName">Name of the action to execute.</param>
    /// <param name="parms">Request body containing action parameters.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>An <see cref="ObjectResult"/> containing the action execution result.</returns>
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
    /// Executes an unauthenticated delete on the specified entity.
    /// The <see cref="PublicEndpointAttribute"/> allows public access without authentication.
    /// </summary>
    /// <param name="app_config">Application configuration provider.</param>
    /// <param name="ents">Service used to execute entity operations.</param>
    /// <param name="app_id">Identifier of the application containing the entity.</param>
    /// <param name="entityName">Name of the entity to delete.</param>
    /// <param name="parms">Request body containing delete parameters.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>An <see cref="ObjectResult"/> containing the delete result.</returns>
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
    /// Executes an unauthenticated retrieval of the specified entity.
    /// The <see cref="PublicEndpointAttribute"/> allows public access without authentication.
    /// </summary>
    /// <param name="app_config">Application configuration provider.</param>
    /// <param name="ents">Service used to execute entity operations.</param>
    /// <param name="app_id">Identifier of the application containing the entity.</param>
    /// <param name="entityName">Name of the entity to retrieve.</param>
    /// <param name="parms">Request body containing retrieval parameters.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>An <see cref="ObjectResult"/> containing the retrieved entity.</returns>
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
    /// Executes an unauthenticated insert on the specified entity.
    /// The <see cref="PublicEndpointAttribute"/> allows public access without authentication.
    /// </summary>
    /// <param name="app_config">Application configuration provider.</param>
    /// <param name="ents">Service used to execute entity operations.</param>
    /// <param name="parms">Request body containing insert parameters.</param>
    /// <param name="app_id">Identifier of the application containing the entity.</param>
    /// <param name="entityName">Name of the entity to insert.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>An <see cref="ObjectResult"/> containing the insert result.</returns>
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
    /// Executes an unauthenticated lookup on the specified entity.
    /// The <see cref="PublicEndpointAttribute"/> allows public access without authentication.
    /// </summary>
    /// <param name="app_config">Application configuration provider.</param>
    /// <param name="ents">Service used to execute entity operations.</param>
    /// <param name="app_id">Identifier of the application containing the entity.</param>
    /// <param name="entityName">Name of the entity to look up.</param>
    /// <param name="lookupName">Optional name of the lookup to execute.</param>
    /// <param name="parms">Request body containing lookup parameters.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>An <see cref="ObjectResult"/> containing the lookup results.</returns>
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
    /// Executes an unauthenticated stored procedure on the specified entity.
    /// The <see cref="PublicEndpointAttribute"/> allows public access without authentication.
    /// </summary>
    /// <param name="app_config">Application configuration provider.</param>
    /// <param name="ents">Service used to execute entity operations.</param>
    /// <param name="app_id">Identifier of the application containing the entity.</param>
    /// <param name="entityName">Name of the entity that owns the procedure.</param>
    /// <param name="procName">Name of the stored procedure to execute.</param>
    /// <param name="parms">Request body containing procedure parameters.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>An <see cref="ObjectResult"/> containing the procedure results.</returns>
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
    /// Executes an unauthenticated process on the specified entity.
    /// The <see cref="PublicEndpointAttribute"/> allows public access without authentication.
    /// </summary>
    /// <param name="app_config">Application configuration provider.</param>
    /// <param name="ents">Service used to execute entity operations.</param>
    /// <param name="app_id">Identifier of the application containing the entity.</param>
    /// <param name="entityName">Name of the entity that owns the process.</param>
    /// <param name="procName">Name of the process to execute.</param>
    /// <param name="parms">Request body containing process parameters.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>An <see cref="ObjectResult"/> containing the process results.</returns>
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
    /// Executes an unauthenticated update on the specified entity.
    /// The <see cref="PublicEndpointAttribute"/> allows public access without authentication.
    /// </summary>
    /// <param name="app_config">Application configuration provider.</param>
    /// <param name="ents">Service used to execute entity operations.</param>
    /// <param name="app_id">Identifier of the application containing the entity.</param>
    /// <param name="entityName">Name of the entity to update.</param>
    /// <param name="parms">Request body containing update parameters.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>An <see cref="ObjectResult"/> containing the update result.</returns>
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
    /// Executes an unauthenticated view on the specified entity.
    /// The <see cref="PublicEndpointAttribute"/> allows public access without authentication.
    /// </summary>
    /// <param name="app_config">Application configuration provider.</param>
    /// <param name="ents">Service used to execute entity operations.</param>
    /// <param name="app_id">Identifier of the application containing the entity.</param>
    /// <param name="entityName">Name of the entity that owns the view.</param>
    /// <param name="viewName">Name of the view to execute.</param>
    /// <param name="parms">Request body containing view parameters.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>An <see cref="ObjectResult"/> containing the view results.</returns>
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
