using MicroM.Data;
using MicroM.Web.Authentication;
using MicroM.Web.Services;
using MicroM.Web.Services.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using System.Runtime.CompilerServices;
using System.Threading.Channels;
using static MicroM.Web.Controllers.MicroMControllersMessages;

namespace MicroM.Web.Controllers;

[ApiController]
public class PublicController : ControllerBase, IPublicController
{
    const string PUBLIC_USERNAME = "public";

    [AllowAnonymous]
    [HttpGet("public-api-status")]
    public string GetStatus()
    {
        return "OK";
    }

    [AllowAnonymous]
    [PublicEndpoint]
    [HttpPost("{app_id}/public/{entityName}/action/{actionName}")]
    [EnableRateLimiting(MicroMServicesConstants.RateLimitingPublicMutationPolicy)]
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


    [AllowAnonymous]
    [PublicEndpoint]
    [HttpPost("{app_id}/public/{entityName}/delete")]
    [EnableRateLimiting(MicroMServicesConstants.RateLimitingPublicMutationPolicy)]
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

    [AllowAnonymous]
    [PublicEndpoint]
    [HttpPost("{app_id}/public/{entityName}/get")]
    [EnableRateLimiting(MicroMServicesConstants.RateLimitingPublicGetPolicy)]
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

    [AllowAnonymous]
    [PublicEndpoint]
    [HttpPost("{app_id}/public/{entityName}/insert")]
    [EnableRateLimiting(MicroMServicesConstants.RateLimitingPublicMutationPolicy)]
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

    [AllowAnonymous]
    [PublicEndpoint]
    [HttpPost("{app_id}/public/{entityName}/lookup/{lookupName?}")]
    [EnableRateLimiting(MicroMServicesConstants.RateLimitingPublicGetPolicy)]
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

    [AllowAnonymous]
    [PublicEndpoint]
    [HttpPost("{app_id}/public/{entityName}/proc/{procName}")]
    [EnableRateLimiting(MicroMServicesConstants.RateLimitingPublicMutationPolicy)]
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

    [AllowAnonymous]
    [PublicEndpoint]
    [HttpPost("{app_id}/public/{entityName}/process/{procName}")]
    [EnableRateLimiting(MicroMServicesConstants.RateLimitingPublicMutationPolicy)]
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

    [AllowAnonymous]
    [PublicEndpoint]
    [HttpPost("{app_id}/public/{entityName}/update")]
    [EnableRateLimiting(MicroMServicesConstants.RateLimitingPublicMutationPolicy)]
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

    [AllowAnonymous]
    [PublicEndpoint]
    [HttpPost("{app_id}/public/{entityName}/view/{viewName}")]
    [EnableRateLimiting(MicroMServicesConstants.RateLimitingPublicGetPolicy)]
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

    private static async IAsyncEnumerable<object?[]> StreamRows(ChannelReader<object?[]> rows, [EnumeratorCancellation] CancellationToken ct)
    {
        await foreach (var row in rows.ReadAllAsync(ct))
        {
            yield return row;
        }
    }

    [AllowAnonymous]
    [PublicEndpoint]
    [HttpPost("{app_id}/public/{entityName}/viewstream/{viewName}")]
    [EnableRateLimiting(MicroMServicesConstants.RateLimitingPublicGetPolicy)]
    public async IAsyncEnumerable<object> PublicViewStream([FromServices] IAuthenticationProvider auth, [FromServices] IMicroMAppConfiguration app_config, [FromServices] IEntitiesService ents, string app_id, string entityName, string viewName, [FromBody] DataWebAPIRequest parms, [EnumeratorCancellation] CancellationToken ct)
    {
        var app = auth.GetAppAndUnencryptClaims(app_config, app_id, parms, User.Claims.ToClaimsDictionary());
        if (app == null)
        {
            Response.StatusCode = StatusCodes.Status400BadRequest;
            yield break;
        }

        using var ec = await ents.CreateDbConnection(app, parms.ServerClaims, ct);
        var result_channel = new DataResultSetChannel(capacity: 2);

        Task producerTask = ents.HandleExecuteViewChannel(app, entityName, viewName, parms, ec, result_channel, ct);

        await foreach (var resultSet in result_channel.Results.Reader.ReadAllAsync(ct))
        {
            yield return new
            {
                resultSet.Header,
                resultSet.typeInfo,
                records = StreamRows(resultSet.records.Reader, ct)
            };
        }

        // We let errors bubble on purpose. If the producer task threw an error, we want that to be observed and not silently ignored.
        // The client will see the error as a stream termination with an error status code.
        await producerTask;
    }

    [AllowAnonymous]
    [PublicEndpoint]
    [HttpPost("{app_id}/public/{entityName}/procstream/{procName}")]
    [EnableRateLimiting(MicroMServicesConstants.RateLimitingPublicGetPolicy)]
    public async IAsyncEnumerable<object> PublicProcStream([FromServices] IAuthenticationProvider auth, [FromServices] IMicroMAppConfiguration app_config, [FromServices] IEntitiesService ents, string app_id, string entityName, string procName, [FromBody] DataWebAPIRequest parms, [EnumeratorCancellation] CancellationToken ct)
    {
        var app = auth.GetAppAndUnencryptClaims(app_config, app_id, parms, User.Claims.ToClaimsDictionary());
        if (app == null)
        {
            Response.StatusCode = StatusCodes.Status400BadRequest;
            yield break;
        }

        using var ec = await ents.CreateDbConnection(app, parms.ServerClaims, ct);
        var result_channel = new DataResultSetChannel(capacity: 2);

        Task producerTask = ents.HandleExecuteProcChannel(app, entityName, procName, parms, ec, result_channel, ct);

        await foreach (var resultSet in result_channel.Results.Reader.ReadAllAsync(ct))
        {
            yield return new
            {
                headers = resultSet.Header,
                resultSet.typeInfo,
                rows = StreamRows(resultSet.records.Reader, ct)
            };
        }

        // We let errors bubble on purpose. If the producer task threw an error, we want that to be observed and not silently ignored.
        // The client will see the error as a stream termination with an error status code.
        await producerTask;
    }
}
