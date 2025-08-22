using MicroM.Data;
using MicroM.Web.Authentication;
using MicroM.Web.Services;
using MicroM.Web.Services.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using static MicroM.Web.Controllers.MicroMControllersMessages;

namespace MicroM.Web.Controllers;

/// <summary>
/// Provides endpoints for uploading and serving files.
/// </summary>
[ApiController]
public class FileController : ControllerBase, IFileController
{
    /// <summary>
    /// Returns a simple response indicating that the file API is available.
    /// </summary>
    [AllowAnonymous]
    [HttpGet("file-api-status")]
    public string GetStatus()
    {
        return "OK";
    }

    /// <summary>
    /// Serves a previously uploaded file.
    /// </summary>
    [Authorize(policy: nameof(MicroMPermissionsConstants.MicroMPermissionsPolicy))]
    [HttpGet("{app_id}/serve/{fileguid}")]
    public async Task<IActionResult> Serve([FromServices] IAuthenticationProvider auth, [FromServices] IMicroMAppConfiguration app_config, [FromServices] IEntitiesService ents, [FromServices] IFileUploadService ups, string app_id, string fileguid, CancellationToken ct)
    {
        try
        {
            DataWebAPIRequest parms = new();
            var app = auth.GetAppAndUnencryptClaims(app_config, app_id, parms, User.Claims.ToClaimsDictionary());
            if (app == null) return BadRequest(APPLICATION_NOT_FOUND);

            using var ec = await ents.CreateDbConnection(app, parms.ServerClaims, ct);

            var result = await ups.ServeFile(app_id, fileguid, ec, ct);

            if (result == null) return NotFound();

            return File(result.FileStream, result.ContentType);
        }
        catch (Exception ex) when (ex is TaskCanceledException || ex is OperationCanceledException || (ex.InnerException is TaskCanceledException || ex.InnerException is OperationCanceledException))
        {
            return new EmptyResult();
        }
    }

    /// <summary>
    /// Serves a thumbnail for a file.
    /// </summary>
    [Authorize(policy: nameof(MicroMPermissionsConstants.MicroMPermissionsPolicy))]
    [HttpGet("{app_id}/thumbnail/{fileguid}/{maxSize?}/{quality?}")]
    public async Task<IActionResult> ServeThumbnail([FromServices] IAuthenticationProvider auth, [FromServices] IMicroMAppConfiguration app_config, [FromServices] IEntitiesService ents, [FromServices] IFileUploadService ups, string app_id, string fileguid, int? maxSize, int? quality, CancellationToken ct)
    {
        try
        {
            DataWebAPIRequest parms = new();
            var app = auth.GetAppAndUnencryptClaims(app_config, app_id, parms, User.Claims.ToClaimsDictionary());
            if (app == null) return BadRequest(APPLICATION_NOT_FOUND);

            using var ec = await ents.CreateDbConnection(app, parms.ServerClaims, ct);

            var result = await ups.ServeThumbnail(app_id, fileguid, maxSize, quality, ec, ct);

            if (result == null) return NotFound();

            return File(result.FileStream, result.ContentType);
        }
        catch (Exception ex) when (ex is TaskCanceledException || ex is OperationCanceledException || (ex.InnerException is TaskCanceledException || ex.InnerException is OperationCanceledException))
        {
            return new EmptyResult();
        }
    }

    /// <summary>
    /// Uploads a temporary file.
    /// </summary>
    [Authorize(policy: nameof(MicroMPermissionsConstants.MicroMPermissionsPolicy))]
    [HttpPost("{app_id}/tmpupload")]
    public async Task<ObjectResult> Upload([FromServices] IAuthenticationProvider auth, [FromServices] IMicroMAppConfiguration app_config, [FromServices] IEntitiesService ents, [FromServices] IFileUploadService ups, string app_id, [FromQuery] string fileprocess_id, [FromQuery] string file_name, [FromQuery] int? maxSize, [FromQuery] int? quality, CancellationToken ct)
    {
        try
        {
            DataWebAPIRequest parms = new();
            var app = auth.GetAppAndUnencryptClaims(app_config, app_id, parms, User.Claims.ToClaimsDictionary());
            if (app == null) return BadRequest(APPLICATION_NOT_FOUND);

            using var ec = await ents.CreateDbConnection(app, parms.ServerClaims, ct);

            var result = await ups.UploadFile(app_id, fileprocess_id, file_name, Request.Body, maxSize, quality, ec, ct);

            return Ok(result);
        }
        catch (Exception ex) when (ex is TaskCanceledException || ex is OperationCanceledException || (ex.InnerException is TaskCanceledException || ex.InnerException is OperationCanceledException))
        {
            return Ok(null);
        }
    }

}
