using MicroM.Data;
using MicroM.Web.Authentication;
using MicroM.Web.Services;
using MicroM.Web.Services.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.IO;
using static MicroM.Web.Controllers.MicroMControllersMessages;

namespace MicroM.Web.Controllers;

/// <summary>
/// Provides endpoints for serving, uploading, and generating thumbnails for application files.
/// </summary>
[ApiController]
/// <summary>
/// Provides endpoints for serving, uploading, and generating thumbnails for application files.
/// </summary>
public class FileController : ControllerBase, IFileController
{
    /// <summary>
    /// Indicates whether the file API is reachable.
    /// </summary>
    /// <returns>Returns <c>"OK"</c> in a 200 response if the API is available.</returns>
    /// <exception cref="OperationCanceledException">The request was canceled.</exception>
    [AllowAnonymous]
    [HttpGet("file-api-status")]
    public string GetStatus()
    {
        return "OK";
    }

    /// <summary>
    /// Streams a stored file to the client.
    /// </summary>
    /// <param name="auth">Authentication provider used to validate the application.</param>
    /// <param name="app_config">Application configuration service.</param>
    /// <param name="ents">Service for creating database connections.</param>
    /// <param name="ups">File service that retrieves the stored file.</param>
    /// <param name="app_id">Identifier of the application that owns the file.</param>
    /// <param name="fileguid">Unique identifier of the file to stream.</param>
    /// <param name="ct">Token used to cancel the request.</param>
    /// <returns>
    /// Returns the file stream with its MIME type on success (HTTP 200).
    /// Returns <see cref="NotFoundResult"/> when the file does not exist (HTTP 404).
    /// Returns <see cref="BadRequestObjectResult"/> when the application is not recognized (HTTP 400).
    /// Returns <see cref="EmptyResult"/> when the request is cancelled.
    /// </returns>
    /// <exception cref="FileNotFoundException">The requested file was not found.</exception>
    /// <exception cref="OperationCanceledException">The operation was cancelled.</exception>
    /// <exception cref="TaskCanceledException">The operation was cancelled.</exception>
    [Authorize(policy: nameof(MicroMPermissionsConstants.MicroMPermissionsPolicy))]
    [HttpGet("{app_id}/serve/{fileguid}")]
    public async Task<IActionResult> Serve(
        [FromServices] IAuthenticationProvider auth,
        [FromServices] IMicroMAppConfiguration app_config,
        [FromServices] IEntitiesService ents,
        [FromServices] IFileUploadService ups,
        string app_id,
        string fileguid,
        CancellationToken ct)
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
    /// Streams a generated thumbnail for the specified file to the client.
    /// </summary>
    /// <param name="auth">Authentication provider used to validate the application.</param>
    /// <param name="app_config">Application configuration service.</param>
    /// <param name="ents">Service for creating database connections.</param>
    /// <param name="ups">File service that generates the thumbnail.</param>
    /// <param name="app_id">Identifier of the application that owns the file.</param>
    /// <param name="fileguid">Unique identifier of the file to thumbnail.</param>
    /// <param name="maxSize">Maximum pixel size of the thumbnail.</param>
    /// <param name="quality">Optional quality of the generated image.</param>
    /// <param name="ct">Token used to cancel the request.</param>
    /// <returns>
    /// Returns the thumbnail stream with an image MIME type on success (HTTP 200).
    /// Returns <see cref="NotFoundResult"/> when the file or thumbnail cannot be generated (HTTP 404).
    /// Returns <see cref="BadRequestObjectResult"/> when the application is not recognized (HTTP 400).
    /// Returns <see cref="EmptyResult"/> when the request is cancelled.
    /// </returns>
    /// <exception cref="FileNotFoundException">The target file was not found.</exception>
    /// <exception cref="OperationCanceledException">The operation was cancelled.</exception>
    /// <exception cref="TaskCanceledException">The operation was cancelled.</exception>
    [Authorize(policy: nameof(MicroMPermissionsConstants.MicroMPermissionsPolicy))]
    [HttpGet("{app_id}/thumbnail/{fileguid}/{maxSize?}/{quality?}")]
    public async Task<IActionResult> ServeThumbnail(
        [FromServices] IAuthenticationProvider auth,
        [FromServices] IMicroMAppConfiguration app_config,
        [FromServices] IEntitiesService ents,
        [FromServices] IFileUploadService ups,
        string app_id,
        string fileguid,
        int? maxSize,
        int? quality,
        CancellationToken ct)
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
    /// Uploads a file for temporary processing.
    /// </summary>
    /// <param name="auth">Authentication provider used to validate the application.</param>
    /// <param name="app_config">Application configuration service.</param>
    /// <param name="ents">Service for creating database connections.</param>
    /// <param name="ups">File service that handles the upload.</param>
    /// <param name="app_id">Identifier of the application receiving the upload.</param>
    /// <param name="fileprocess_id">Processing identifier used to correlate subsequent requests.</param>
    /// <param name="file_name">Original file name.</param>
    /// <param name="maxSize">Maximum allowed file size in bytes.</param>
    /// <param name="quality">Optional quality for image processing.</param>
    /// <param name="ct">Token used to cancel the request.</param>
    /// <returns>
    /// Returns an object describing the uploaded file on success (HTTP 200).
    /// Returns <see cref="BadRequestObjectResult"/> when the application is not recognized (HTTP 400).
    /// Returns <c>null</c> when the operation is cancelled.
    /// </returns>
    /// <exception cref="FileNotFoundException">The upload stream could not be read.</exception>
    /// <exception cref="OperationCanceledException">The operation was cancelled.</exception>
    /// <exception cref="TaskCanceledException">The operation was cancelled.</exception>
    [Authorize(policy: nameof(MicroMPermissionsConstants.MicroMPermissionsPolicy))]
    [HttpPost("{app_id}/tmpupload")]
    public async Task<ObjectResult> Upload(
        [FromServices] IAuthenticationProvider auth,
        [FromServices] IMicroMAppConfiguration app_config,
        [FromServices] IEntitiesService ents,
        [FromServices] IFileUploadService ups,
        string app_id,
        [FromQuery] string fileprocess_id,
        [FromQuery] string file_name,
        [FromQuery] int? maxSize,
        [FromQuery] int? quality,
        CancellationToken ct)
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

