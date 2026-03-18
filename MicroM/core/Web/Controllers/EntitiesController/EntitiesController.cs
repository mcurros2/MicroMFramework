using MicroM.Configuration;
using MicroM.Data;
using MicroM.Web.Authentication;
using MicroM.Web.Services;
using MicroM.Web.Services.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Runtime.CompilerServices;
using System.Threading.Channels;
using static MicroM.Excel.ExcelWriter;
using static MicroM.Web.Controllers.MicroMControllersMessages;

namespace MicroM.Web.Controllers;

[ApiController]
public class EntitiesController() : ControllerBase, IEntitiesController
{
    [AllowAnonymous]
    [HttpGet("entities-api-status")]
    public string GetStatus()
    {
        return "OK";
    }

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

    private async IAsyncEnumerable<object?[]> StreamRows(ChannelReader<object?[]> rows, [EnumeratorCancellation] CancellationToken ct)
    {
        await foreach (var row in rows.ReadAllAsync(ct))
        {
            yield return row;
        }
    }

    [Authorize(policy: nameof(MicroMPermissionsConstants.MicroMPermissionsPolicy))]
    [HttpPost("{app_id}/ent/{entityName}/viewstream/{viewName}")]
    public async IAsyncEnumerable<object> ViewStream([FromServices] IAuthenticationProvider auth, [FromServices] IMicroMAppConfiguration app_config, [FromServices] IEntitiesService ents, string app_id, string entityName, string viewName, [FromBody] DataWebAPIRequest parms, [EnumeratorCancellation] CancellationToken ct)
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

    [Authorize(policy: nameof(MicroMPermissionsConstants.MicroMPermissionsPolicy))]
    [HttpPost("{app_id}/ent/{entityName}/procstream/{procName}")]
    public async IAsyncEnumerable<object> ProcStream([FromServices] IAuthenticationProvider auth, [FromServices] IMicroMAppConfiguration app_config, [FromServices] IEntitiesService ents, string app_id, string entityName, string procName, [FromBody] DataWebAPIRequest parms, [EnumeratorCancellation] CancellationToken ct)
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
                resultSet.Header,
                resultSet.typeInfo,
                records = StreamRows(resultSet.records.Reader, ct)
            };
        }

        // We let errors bubble on purpose. If the producer task threw an error, we want that to be observed and not silently ignored.
        // The client will see the error as a stream termination with an error status code.
        await producerTask;
    }

    [Authorize(policy: nameof(MicroMPermissionsConstants.MicroMPermissionsPolicy))]
    [HttpPost("{app_id}/ent/{entityName}/viewtoexcel/{viewName}")]
    public async Task<IActionResult> ExportViewExcel([FromServices] IAuthenticationProvider auth, [FromServices] IMicroMAppConfiguration app_config, [FromServices] IEntitiesService ents, string app_id, string entityName, string viewName, [FromBody] DataWebAPIRequest parms, CancellationToken ct)
    {
        var app = auth.GetAppAndUnencryptClaims(app_config, app_id, parms, User.Claims.ToClaimsDictionary());
        if (app == null) return BadRequest(APPLICATION_NOT_FOUND);

        using var ec = await ents.CreateDbConnection(app, parms.ServerClaims, ct);
        var resultChannel = new DataResultSetChannel(capacity: 2);

        Task producerTask = ents.HandleExecuteViewChannel(app, entityName, viewName, parms, ec, resultChannel, ct, records_channel_capacity: DataDefaults.DefaultChannelExportToExcelBuffer);

        // we prefer Sylvan for now as it consumes less memory than OpenXML
        var fileResult = await ExportSylvanFromChannelAsync($"{entityName}_export", resultChannel, producerTask, ct);

        return fileResult;
    }

    [Authorize(policy: nameof(MicroMPermissionsConstants.MicroMPermissionsPolicy))]
    [HttpPost("{app_id}/ent/{entityName}/proctoexcel/{procName}")]
    public async Task<IActionResult> ExportProcExcel([FromServices] IAuthenticationProvider auth, [FromServices] IMicroMAppConfiguration app_config, [FromServices] IEntitiesService ents, string app_id, string entityName, string procName, [FromBody] DataWebAPIRequest parms, CancellationToken ct)
    {
        var app = auth.GetAppAndUnencryptClaims(app_config, app_id, parms, User.Claims.ToClaimsDictionary());
        if (app == null) return BadRequest(APPLICATION_NOT_FOUND);

        using var ec = await ents.CreateDbConnection(app, parms.ServerClaims, ct);
        var resultChannel = new DataResultSetChannel(capacity: 2);

        Task producerTask = ents.HandleExecuteProcChannel(app, entityName, procName, parms, ec, resultChannel, ct, records_channel_capacity: DataDefaults.DefaultChannelExportToExcelBuffer);

        using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);

        try
        {
            // we prefer Sylvan for now as it consumes less memory than OpenXML
            var fileResult = await ExportSylvanFromChannelAsync($"{entityName}_export", resultChannel, producerTask, ct);
            return fileResult;
        }
        catch
        {
            cts.Cancel();
            throw;
        }


    }
}
