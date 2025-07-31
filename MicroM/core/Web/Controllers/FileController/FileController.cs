using MicroM.Web.Authentication;
using MicroM.Web.Services;
using MicroM.Web.Services.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

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
    public async Task<IActionResult> Serve([FromServices] IAuthenticationProvider auth, [FromServices] IEntitiesService ents, [FromServices] IFileUploadService ups, string app_id, string fileguid, CancellationToken ct)
    {
        try
        {
            var serverClaims = User.Claims.ToClaimsDictionary();
            using var ec = await ents.CreateDbConnection(app_id, serverClaims, auth, ct);

            var result = await ups.ServeFile(app_id, fileguid, ec, ct);

            if (result == null) return NotFound();

            return File(result.FileStream, result.ContentType);
        }
        catch (Exception ex) when (ex is TaskCanceledException || ex is OperationCanceledException)
        {
            return new EmptyResult();
        }
    }

    [Authorize(policy: nameof(MicroMPermissionsConstants.MicroMPermissionsPolicy))]
    [HttpGet("{app_id}/thumbnail/{fileguid}/{maxSize?}/{quality?}")]
    public async Task<IActionResult> ServeThumbnail([FromServices] IAuthenticationProvider auth, [FromServices] IEntitiesService ents, [FromServices] IFileUploadService ups, string app_id, string fileguid, int? maxSize, int? quality, CancellationToken ct)
    {
        try
        {
            var serverClaims = User.Claims.ToClaimsDictionary();
            using var ec = await ents.CreateDbConnection(app_id, serverClaims, auth, ct);

            var result = await ups.ServeThumbnail(app_id, fileguid, maxSize, quality, ec, ct);

            if (result == null) return NotFound();

            return File(result.FileStream, result.ContentType);
        }
        catch (Exception ex) when (ex is TaskCanceledException || ex is OperationCanceledException)
        {
            return new EmptyResult();
        }
    }

    [Authorize(policy: nameof(MicroMPermissionsConstants.MicroMPermissionsPolicy))]
    [HttpPost("{app_id}/tmpupload")]
    public async Task<ObjectResult> Upload([FromServices] IAuthenticationProvider auth, [FromServices] IEntitiesService ents, [FromServices] IFileUploadService ups, string app_id, [FromQuery] string fileprocess_id, [FromQuery] string file_name, [FromQuery] int? maxSize, [FromQuery] int? quality, CancellationToken ct)
    {
        try
        {
            var serverClaims = User.Claims.ToClaimsDictionary();
            using var ec = await ents.CreateDbConnection(app_id, serverClaims, auth, ct);

            var result = await ups.UploadFile(app_id, fileprocess_id, file_name, Request.Body, maxSize, quality, ec, ct);

            return Ok(result);
        }
        catch (Exception ex) when (ex is TaskCanceledException || ex is OperationCanceledException)
        {
            return Ok(null);
        }
    }

}
