using MicroM.Configuration;
using MicroM.Data;
using MicroM.Web.Authentication;
using MicroM.Web.Services;
using MicroM.Web.Services.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using static MicroM.Web.Controllers.MicroMControllersMessages;

namespace MicroM.Web.Controllers;

/// <summary>
/// HTTP API for CRUD and action endpoints on application entities.
/// </summary>
[ApiController]
public class EntitiesController : ControllerBase, IEntitiesController
{
    /// <summary>
    /// Reports the availability of the entities API.
    /// </summary>
    /// <returns>200 OK with the value "OK" when the API is responsive.</returns>
    [AllowAnonymous]
    [HttpGet("entities-api-status")]
    public string GetStatus()
    {
        return "OK";
    }

    /// <summary>
    /// Executes a named action on the specified entity.
    /// </summary>
    /// <param name="auth">Authentication provider used to validate the request and resolve the application.</param>
    /// <param name="app_config">Configuration provider supplying application settings.</param>
    /// <param name="ents">Service used to perform entity operations.</param>
    /// <param name="app_id">Identifier of the application containing the entity.</param>
    /// <param name="entityName">Name of the entity on which to invoke the action.</param>
    /// <param name="actionName">Name of the action to execute.</param>
    /// <param name="parms">Request parameters and payload.</param>
    /// <param name="ct">Token used to cancel the request.</param>
    /// <returns>200 OK with the action result, 400 if the application or result is invalid, 409 if the operation is cancelled.</returns>
    /// <exception cref="OperationCanceledException">The request was cancelled.</exception>
    /// <exception cref="TaskCanceledException">The request was cancelled.</exception>
    [Authorize(policy: nameof(MicroMPermissionsConstants.MicroMPermissionsPolicy))]
    [HttpPost("{app_id}/ent/{entityName}/action/{actionName}")]
    public async Task<ObjectResult> Action([FromServices] IAuthenticationProvider auth, [FromServices] IMicroMAppConfiguration app_config, [FromServices] IEntitiesService ents, string app_id, string entityName, string actionName, [FromBody] DataWebAPIRequest parms, CancellationToken ct)
    {
        try
        {
            var app = auth.GetAppAndUnencryptClaims(app_config, app_id, parms, User.Claims.ToClaimsDictionary());
            if (app == null) return BadRequest(APPLICATION_NOT_FOUND);

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
    /// Deletes a record from the specified entity.
    /// </summary>
    /// <param name="auth">Authentication provider used to validate the request and resolve the application.</param>
    /// <param name="app_config">Configuration provider supplying application settings.</param>
    /// <param name="ents">Service used to perform entity operations.</param>
    /// <param name="app_id">Identifier of the application containing the entity.</param>
    /// <param name="entityName">Name of the entity that contains the record to delete.</param>
    /// <param name="parms">Request parameters and payload.</param>
    /// <param name="ct">Token used to cancel the request.</param>
    /// <returns>200 OK with the delete result, 400 if the application or result is invalid, 409 if the operation is cancelled.</returns>
    /// <exception cref="OperationCanceledException">The request was cancelled.</exception>
    /// <exception cref="TaskCanceledException">The request was cancelled.</exception>
    [Authorize(policy: nameof(MicroMPermissionsConstants.MicroMPermissionsPolicy))]
    [HttpPost("{app_id}/ent/{entityName}/delete")]
    public async Task<ObjectResult> Delete([FromServices] IAuthenticationProvider auth, [FromServices] IMicroMAppConfiguration app_config, [FromServices] IEntitiesService ents, string app_id, string entityName, [FromBody] DataWebAPIRequest parms, CancellationToken ct)
    {
        try
        {
            var app = auth.GetAppAndUnencryptClaims(app_config, app_id, parms, User.Claims.ToClaimsDictionary());
            if (app == null) return BadRequest(APPLICATION_NOT_FOUND);

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
    /// Retrieves records for the specified entity.
    /// </summary>
    /// <param name="auth">Authentication provider used to validate the request and resolve the application.</param>
    /// <param name="app_config">Configuration provider supplying application settings.</param>
    /// <param name="ents">Service used to perform entity operations.</param>
    /// <param name="app_id">Identifier of the application containing the entity.</param>
    /// <param name="entityName">Name of the entity to query.</param>
    /// <param name="parms">Request parameters and payload.</param>
    /// <param name="ct">Token used to cancel the request.</param>
    /// <returns>200 OK with the entity data, 400 if the application or result is invalid, 409 if the operation is cancelled.</returns>
    /// <exception cref="OperationCanceledException">The request was cancelled.</exception>
    /// <exception cref="TaskCanceledException">The request was cancelled.</exception>
    [Authorize(policy: nameof(MicroMPermissionsConstants.MicroMPermissionsPolicy))]
    [HttpPost("{app_id}/ent/{entityName}/get")]
    public async Task<ObjectResult> Get([FromServices] IAuthenticationProvider auth, [FromServices] IMicroMAppConfiguration app_config, [FromServices] IEntitiesService ents, string app_id, string entityName, [FromBody] DataWebAPIRequest parms, CancellationToken ct)
    {
        try
        {
            var app = auth.GetAppAndUnencryptClaims(app_config, app_id, parms, User.Claims.ToClaimsDictionary());
            if (app == null) return BadRequest(APPLICATION_NOT_FOUND);

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
    /// Retrieves the schema definition for the specified entity.
    /// </summary>
    /// <param name="ents">Service used to perform entity operations.</param>
    /// <param name="app_config">Configuration provider supplying application settings.</param>
    /// <param name="app_id">Identifier of the application containing the entity.</param>
    /// <param name="entityName">Name of the entity whose definition is requested.</param>
    /// <returns>200 OK with the entity definition, 400 if the application or result is invalid, 409 if the operation is cancelled.</returns>
    /// <exception cref="OperationCanceledException">The request was cancelled.</exception>
    /// <exception cref="TaskCanceledException">The request was cancelled.</exception>
    [Authorize(policy: nameof(MicroMPermissionsConstants.MicroMPermissionsPolicy))]
    [HttpGet("{app_id}/ent/{entityName}/definition")]
    public ObjectResult GetDefinition([FromServices] IEntitiesService ents, [FromServices] IMicroMAppConfiguration app_config, string app_id, string entityName)
    {
        try
        {
            ApplicationOption? app = app_config.GetAppConfiguration(app_id);
            if (app == null) return BadRequest(APPLICATION_NOT_FOUND);

            var result = ents.HandleGetEntityDefinition(app, entityName);
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
    /// Retrieves the time zone offset for the current application.
    /// </summary>
    /// <param name="auth">Authentication provider used to validate the request and resolve the application.</param>
    /// <param name="app_config">Configuration provider supplying application settings.</param>
    /// <param name="ents">Service used to perform entity operations.</param>
    /// <param name="app_id">Identifier of the application containing the entity.</param>
    /// <param name="ct">Token used to cancel the request.</param>
    /// <returns>200 OK with the offset in minutes; 0 if the application is not found or the operation is cancelled.</returns>
    /// <exception cref="OperationCanceledException">The request was cancelled.</exception>
    /// <exception cref="TaskCanceledException">The request was cancelled.</exception>
    [Authorize(policy: nameof(MicroMPermissionsConstants.MicroMPermissionsPolicy))]
    [HttpPost("{app_id}/ent/timezoneoffset")]
    public async Task<int> GetTimeZoneOffset([FromServices] IAuthenticationProvider auth, [FromServices] IMicroMAppConfiguration app_config, [FromServices] IEntitiesService ents, string app_id, CancellationToken ct)
    {
        try
        {
            DataWebAPIRequest parms = new();
            var app = auth.GetAppAndUnencryptClaims(app_config, app_id, parms, User.Claims.ToClaimsDictionary());
            if (app == null) return 0;

            using var ec = await ents.CreateDbConnection(app, parms.ServerClaims, ct);

            var result = await ents.HandleGetTimeZoneOffset(app, ec, ct);

            return result;
        }
        catch (Exception ex) when (ex is TaskCanceledException || ex is OperationCanceledException || (ex.InnerException is TaskCanceledException || ex.InnerException is OperationCanceledException))
        {
            return 0;
        }
    }

    /// <summary>
    /// Imports data into the specified entity using an optional import procedure.
    /// </summary>
    /// <param name="auth">Authentication provider used to validate the request and resolve the application.</param>
    /// <param name="app_config">Configuration provider supplying application settings.</param>
    /// <param name="ents">Service used to perform entity operations.</param>
    /// <param name="app_id">Identifier of the application containing the entity.</param>
    /// <param name="entityName">Name of the entity receiving the data.</param>
    /// <param name="import_proc">Optional name of the import procedure to execute.</param>
    /// <param name="parms">Request parameters and payload.</param>
    /// <param name="ct">Token used to cancel the request.</param>
    /// <returns>200 OK with the import result, 400 if the application or result is invalid, 409 if the operation is cancelled.</returns>
    /// <exception cref="OperationCanceledException">The request was cancelled.</exception>
    /// <exception cref="TaskCanceledException">The request was cancelled.</exception>
    [Authorize(policy: nameof(MicroMPermissionsConstants.MicroMPermissionsPolicy))]
    [HttpPost("{app_id}/ent/{entityName}/import/{import_proc?}")]
    public async Task<ObjectResult> Import([FromServices] IAuthenticationProvider auth, [FromServices] IMicroMAppConfiguration app_config, [FromServices] IEntitiesService ents, string app_id, string entityName, string? import_proc, [FromBody] DataWebAPIRequest parms, CancellationToken ct)
    {
        try
        {
            var app = auth.GetAppAndUnencryptClaims(app_config, app_id, parms, User.Claims.ToClaimsDictionary());
            if (app == null) return BadRequest(APPLICATION_NOT_FOUND);

            using var ec = await ents.CreateDbConnection(app, parms.ServerClaims, ct);

            var result = await ents.HandleImportData(app, entityName, import_proc, parms, ec, ct);

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
    /// Inserts a new record into the specified entity.
    /// </summary>
    /// <param name="auth">Authentication provider used to validate the request and resolve the application.</param>
    /// <param name="app_config">Configuration provider supplying application settings.</param>
    /// <param name="ents">Service used to perform entity operations.</param>
    /// <param name="parms">Request parameters and payload.</param>
    /// <param name="app_id">Identifier of the application containing the entity.</param>
    /// <param name="entityName">Name of the entity in which to insert the record.</param>
    /// <param name="ct">Token used to cancel the request.</param>
    /// <returns>200 OK with the insert result, 400 if the application or result is invalid, 409 if the operation is cancelled.</returns>
    /// <exception cref="OperationCanceledException">The request was cancelled.</exception>
    /// <exception cref="TaskCanceledException">The request was cancelled.</exception>
    [Authorize(policy: nameof(MicroMPermissionsConstants.MicroMPermissionsPolicy))]
    [HttpPost("{app_id}/ent/{entityName}/insert")]
    public async Task<ObjectResult> Insert(
        [FromServices] IAuthenticationProvider auth,
        [FromServices] IMicroMAppConfiguration app_config,
        [FromServices] IEntitiesService ents,
        [FromBody] DataWebAPIRequest parms,
        string app_id, string entityName,
        CancellationToken ct)
    {
        try
        {
            var app = auth.GetAppAndUnencryptClaims(app_config, app_id, parms, User.Claims.ToClaimsDictionary());
            if (app == null) return BadRequest(APPLICATION_NOT_FOUND);

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
    /// Performs a lookup query on the specified entity.
    /// </summary>
    /// <param name="auth">Authentication provider used to validate the request and resolve the application.</param>
    /// <param name="app_config">Configuration provider supplying application settings.</param>
    /// <param name="ents">Service used to perform entity operations.</param>
    /// <param name="app_id">Identifier of the application containing the entity.</param>
    /// <param name="entityName">Name of the entity to query.</param>
    /// <param name="lookupName">Optional name of the lookup to execute.</param>
    /// <param name="parms">Request parameters and payload.</param>
    /// <param name="ct">Token used to cancel the request.</param>
    /// <returns>200 OK with the lookup result, 400 if the application is not found, 200 OK with null if the operation is cancelled.</returns>
    /// <exception cref="OperationCanceledException">The request was cancelled.</exception>
    /// <exception cref="TaskCanceledException">The request was cancelled.</exception>
    [Authorize(policy: nameof(MicroMPermissionsConstants.MicroMPermissionsPolicy))]
    [HttpPost("{app_id}/ent/{entityName}/lookup/{lookupName?}")]
    public async Task<ObjectResult> Lookup([FromServices] IAuthenticationProvider auth, [FromServices] IMicroMAppConfiguration app_config, [FromServices] IEntitiesService ents, string app_id, string entityName, string? lookupName, [FromBody] DataWebAPIRequest parms, CancellationToken ct)
    {
        try
        {
            var app = auth.GetAppAndUnencryptClaims(app_config, app_id, parms, User.Claims.ToClaimsDictionary());
            if (app == null) return BadRequest(APPLICATION_NOT_FOUND);

            using var ec = await ents.CreateDbConnection(app, parms.ServerClaims, ct);
            var result = await ents.HandleLookupEntity(app, entityName, parms, ec, ct, lookupName);

            return Ok(result);
        }
        catch (Exception ex) when (ex is TaskCanceledException || ex is OperationCanceledException)
        {
            return Ok(null);
        }
    }

    /// <summary>
    /// Executes a stored procedure for the specified entity.
    /// </summary>
    /// <param name="auth">Authentication provider used to validate the request and resolve the application.</param>
    /// <param name="app_config">Configuration provider supplying application settings.</param>
    /// <param name="ents">Service used to perform entity operations.</param>
    /// <param name="app_id">Identifier of the application containing the entity.</param>
    /// <param name="entityName">Name of the entity associated with the procedure.</param>
    /// <param name="procName">Name of the stored procedure to execute.</param>
    /// <param name="parms">Request parameters and payload.</param>
    /// <param name="ct">Token used to cancel the request.</param>
    /// <returns>200 OK with the procedure result, 400 if the application or result is invalid, 409 if the operation is cancelled.</returns>
    /// <exception cref="OperationCanceledException">The request was cancelled.</exception>
    /// <exception cref="TaskCanceledException">The request was cancelled.</exception>
    [Authorize(policy: nameof(MicroMPermissionsConstants.MicroMPermissionsPolicy))]
    [HttpPost("{app_id}/ent/{entityName}/proc/{procName}")]
    public async Task<ObjectResult> Proc([FromServices] IAuthenticationProvider auth, [FromServices] IMicroMAppConfiguration app_config, [FromServices] IEntitiesService ents, string app_id, string entityName, string procName, [FromBody] DataWebAPIRequest parms, CancellationToken ct)
    {
        try
        {
            var app = auth.GetAppAndUnencryptClaims(app_config, app_id, parms, User.Claims.ToClaimsDictionary());
            if (app == null) return BadRequest(APPLICATION_NOT_FOUND);

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
    /// Executes a database process for the specified entity and returns status information.
    /// </summary>
    /// <param name="auth">Authentication provider used to validate the request and resolve the application.</param>
    /// <param name="app_config">Configuration provider supplying application settings.</param>
    /// <param name="ents">Service used to perform entity operations.</param>
    /// <param name="app_id">Identifier of the application containing the entity.</param>
    /// <param name="entityName">Name of the entity associated with the process.</param>
    /// <param name="procName">Name of the process to execute.</param>
    /// <param name="parms">Request parameters and payload.</param>
    /// <param name="ct">Token used to cancel the request.</param>
    /// <returns>200 OK with the process result, 400 if the application or result is invalid, 409 if the operation is cancelled.</returns>
    /// <exception cref="OperationCanceledException">The request was cancelled.</exception>
    /// <exception cref="TaskCanceledException">The request was cancelled.</exception>
    [Authorize(policy: nameof(MicroMPermissionsConstants.MicroMPermissionsPolicy))]
    [HttpPost("{app_id}/ent/{entityName}/process/{procName}")]
    public async Task<ObjectResult> Process([FromServices] IAuthenticationProvider auth, [FromServices] IMicroMAppConfiguration app_config, [FromServices] IEntitiesService ents, string app_id, string entityName, string procName, [FromBody] DataWebAPIRequest parms, CancellationToken ct)
    {
        try
        {
            var app = auth.GetAppAndUnencryptClaims(app_config, app_id, parms, User.Claims.ToClaimsDictionary());
            if (app == null) return BadRequest(APPLICATION_NOT_FOUND);

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
    /// Updates an existing record in the specified entity.
    /// </summary>
    /// <param name="auth">Authentication provider used to validate the request and resolve the application.</param>
    /// <param name="app_config">Configuration provider supplying application settings.</param>
    /// <param name="ents">Service used to perform entity operations.</param>
    /// <param name="app_id">Identifier of the application containing the entity.</param>
    /// <param name="entityName">Name of the entity to update.</param>
    /// <param name="parms">Request parameters and payload.</param>
    /// <param name="ct">Token used to cancel the request.</param>
    /// <returns>200 OK with the update result, 400 if the application or result is invalid, 409 if the operation is cancelled.</returns>
    /// <exception cref="OperationCanceledException">The request was cancelled.</exception>
    /// <exception cref="TaskCanceledException">The request was cancelled.</exception>
    [Authorize(policy: nameof(MicroMPermissionsConstants.MicroMPermissionsPolicy))]
    [HttpPost("{app_id}/ent/{entityName}/update")]
    public async Task<ObjectResult> Update([FromServices] IAuthenticationProvider auth, [FromServices] IMicroMAppConfiguration app_config, [FromServices] IEntitiesService ents, string app_id, string entityName, [FromBody] DataWebAPIRequest parms, CancellationToken ct)
    {
        try
        {
            var app = auth.GetAppAndUnencryptClaims(app_config, app_id, parms, User.Claims.ToClaimsDictionary());
            if (app == null) return BadRequest(APPLICATION_NOT_FOUND);

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
    /// Executes a predefined view for the specified entity.
    /// </summary>
    /// <param name="auth">Authentication provider used to validate the request and resolve the application.</param>
    /// <param name="app_config">Configuration provider supplying application settings.</param>
    /// <param name="ents">Service used to perform entity operations.</param>
    /// <param name="app_id">Identifier of the application containing the entity.</param>
    /// <param name="entityName">Name of the entity associated with the view.</param>
    /// <param name="viewName">Name of the view to execute.</param>
    /// <param name="parms">Request parameters and payload.</param>
    /// <param name="ct">Token used to cancel the request.</param>
    /// <returns>200 OK with the view result, 400 if the application or result is invalid, 409 if the operation is cancelled.</returns>
    /// <exception cref="OperationCanceledException">The request was cancelled.</exception>
    /// <exception cref="TaskCanceledException">The request was cancelled.</exception>
    [Authorize(policy: nameof(MicroMPermissionsConstants.MicroMPermissionsPolicy))]
    [HttpPost("{app_id}/ent/{entityName}/view/{viewName}")]
    public async Task<ObjectResult> View([FromServices] IAuthenticationProvider auth, [FromServices] IMicroMAppConfiguration app_config, [FromServices] IEntitiesService ents, string app_id, string entityName, string viewName, [FromBody] DataWebAPIRequest parms, CancellationToken ct)
    {
        try
        {
            var app = auth.GetAppAndUnencryptClaims(app_config, app_id, parms, User.Claims.ToClaimsDictionary());
            if (app == null) return BadRequest(APPLICATION_NOT_FOUND);

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
