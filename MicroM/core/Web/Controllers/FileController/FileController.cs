using MicroM.Data;
using MicroM.Web.Authentication;
using MicroM.Web.Services;
using MicroM.Web.Services.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Mvc;
using static MicroM.Web.Controllers.MicroMControllersMessages;

namespace MicroM.Web.Controllers;

[ApiController]
public class FileController : ControllerBase, IFileController
{
    [AllowAnonymous]
    [HttpGet("file-api-status")]
    public string GetStatus()
    {
        return "OK";
    }

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

            var result = await ups.ServeFile(app, fileguid, ec, ct);

            if (result == null) return NotFound();

            return File(result.Stream, result.ContentType);
        }
        catch (Exception ex) when (ex is TaskCanceledException || ex is OperationCanceledException || (ex.InnerException is TaskCanceledException || ex.InnerException is OperationCanceledException))
        {
            return new EmptyResult();
        }
    }

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

            var result = await ups.ServeThumbnail(app, fileguid, maxSize, quality, ec, ct);

            if (result == null) return NotFound();

            return File(result.Stream, result.ContentType);
        }
        catch (Exception ex) when (ex is TaskCanceledException || ex is OperationCanceledException || (ex.InnerException is TaskCanceledException || ex.InnerException is OperationCanceledException))
        {
            return new EmptyResult();
        }
    }

    [Authorize(policy: nameof(MicroMPermissionsConstants.MicroMPermissionsPolicy))]
    [HttpPost("{app_id}/tmpupload")]
    public async Task<ObjectResult> Upload(
        [FromServices] IAuthenticationProvider auth, [FromServices] IMicroMAppConfiguration app_config, [FromServices] IEntitiesService ents,
        [FromServices] IFileUploadService ups, string app_id, [FromQuery] string fileprocess_id, [FromQuery] string file_name,
        [FromQuery] int? maxSize, [FromQuery] int? quality, [FromQuery] string? file_tag, CancellationToken ct)
    {
        try
        {
            DataWebAPIRequest parms = new();
            var app = auth.GetAppAndUnencryptClaims(app_config, app_id, parms, User.Claims.ToClaimsDictionary());
            if (app == null) return BadRequest(APPLICATION_NOT_FOUND);

            var maxBodySizeFeature = HttpContext.Features.Get<IHttpMaxRequestBodySizeFeature>();

            if (maxBodySizeFeature is not null && !maxBodySizeFeature.IsReadOnly)
            {
                maxBodySizeFeature.MaxRequestBodySize = app.UploadLimitBytes;
            }

            if (Request.ContentType is null ||
                !Request.ContentType.StartsWith("application/octet-stream", StringComparison.OrdinalIgnoreCase))
            {
                return BadRequest("Content-Type must be application/octet-stream.");
            }

            if (Request.ContentLength is null or <= 0)
            {
                return BadRequest("Request body is empty.");
            }

            if (Request.ContentLength > app.UploadLimitBytes)
            {
                return StatusCode(StatusCodes.Status413PayloadTooLarge, new
                {
                    ErrorMessage = $"File is too large. Max allowed size is {app.UploadLimitBytes} bytes."
                });
            }

            if (string.IsNullOrWhiteSpace(file_name)) return BadRequest("file_name is required.");

            if (string.IsNullOrWhiteSpace(fileprocess_id)) return BadRequest("fileprocess_id is required.");

            using var ec = await ents.CreateDbConnection(app, parms.ServerClaims, ct);

            var result = await ups.UploadFile(app, fileprocess_id, file_name, Request.Body, file_tag, maxSize, quality, ec, ct);

            return Ok(result);
        }
        catch (Exception ex) when (ex is TaskCanceledException || ex is OperationCanceledException || (ex.InnerException is TaskCanceledException || ex.InnerException is OperationCanceledException))
        {
            return Ok(null);
        }
    }

}
