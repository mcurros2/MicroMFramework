using MicroM.Web.Authentication;
using MicroM.Web.Services;
using Microsoft.AspNetCore.Mvc;

namespace MicroM.Web.Controllers;

/// <summary>
/// Defines the contract for file upload and retrieval endpoints.
/// </summary>
public interface IFileController
{
    /// <summary>
    /// Returns a simple response indicating that the file API is available.
    /// </summary>
    string GetStatus();

    /// <summary>
    /// Uploads a temporary file.
    /// </summary>
    Task<ObjectResult> Upload(IAuthenticationProvider auth, IMicroMAppConfiguration app_config, IEntitiesService ents, IFileUploadService ups, string app_id, string fileprocess_id, string file_name, int? maxSize, int? quality, CancellationToken ct);
    /// <summary>
    /// Serves a previously uploaded file.
    /// </summary>
    Task<IActionResult> Serve(IAuthenticationProvider auth, IMicroMAppConfiguration app_config, IEntitiesService ents, IFileUploadService ups, string app_id, string fileguid, CancellationToken ct);
    /// <summary>
    /// Serves a thumbnail for a file.
    /// </summary>
    Task<IActionResult> ServeThumbnail(IAuthenticationProvider auth, IMicroMAppConfiguration app_config, IEntitiesService ents, IFileUploadService ups, string app_id, string fileguid, int? maxSize, int? quality, CancellationToken ct);
}
