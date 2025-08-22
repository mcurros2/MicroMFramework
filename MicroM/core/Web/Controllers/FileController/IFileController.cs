using MicroM.Web.Authentication;
using MicroM.Web.Services;
using Microsoft.AspNetCore.Mvc;
using System.IO;

namespace MicroM.Web.Controllers;

/// <summary>
/// Defines operations for serving, uploading, and generating thumbnails for application files.
/// </summary>
public interface IFileController
{
    /// <summary>
    /// Indicates whether the file API is reachable.
    /// </summary>
    /// <returns>Returns <c>"OK"</c> in a 200 response when the service is operational.</returns>
    /// <exception cref="OperationCanceledException">The request was canceled.</exception>
    string GetStatus();

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
    /// An object describing the uploaded file (HTTP 200) or <see cref="BadRequestObjectResult"/> when the application is not recognized (HTTP 400).
    /// </returns>
    /// <exception cref="FileNotFoundException">The upload stream could not be read.</exception>
    /// <exception cref="OperationCanceledException">The operation was cancelled.</exception>
    /// <exception cref="TaskCanceledException">The operation was cancelled.</exception>
    Task<ObjectResult> Upload(IAuthenticationProvider auth, IMicroMAppConfiguration app_config, IEntitiesService ents, IFileUploadService ups, string app_id, string fileprocess_id, string file_name, int? maxSize, int? quality, CancellationToken ct);

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
    /// The file stream and its MIME type on success (HTTP 200) or <see cref="NotFoundResult"/> when the file does not exist (HTTP 404).
    /// </returns>
    /// <exception cref="FileNotFoundException">The requested file was not found.</exception>
    /// <exception cref="OperationCanceledException">The operation was cancelled.</exception>
    /// <exception cref="TaskCanceledException">The operation was cancelled.</exception>
    Task<IActionResult> Serve(IAuthenticationProvider auth, IMicroMAppConfiguration app_config, IEntitiesService ents, IFileUploadService ups, string app_id, string fileguid, CancellationToken ct);

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
    /// The thumbnail stream with an image MIME type on success (HTTP 200) or <see cref="NotFoundResult"/> when the file or thumbnail cannot be generated (HTTP 404).
    /// </returns>
    /// <exception cref="FileNotFoundException">The target file was not found.</exception>
    /// <exception cref="OperationCanceledException">The operation was cancelled.</exception>
    /// <exception cref="TaskCanceledException">The operation was cancelled.</exception>
    Task<IActionResult> ServeThumbnail(IAuthenticationProvider auth, IMicroMAppConfiguration app_config, IEntitiesService ents, IFileUploadService ups, string app_id, string fileguid, int? maxSize, int? quality, CancellationToken ct);
}

