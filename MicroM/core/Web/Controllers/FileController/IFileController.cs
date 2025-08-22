using MicroM.Web.Authentication;
using MicroM.Web.Services;
using Microsoft.AspNetCore.Mvc;

namespace MicroM.Web.Controllers;

/// <summary>
/// Represents the IFileController.
/// </summary>
public interface IFileController
{
    /// <summary>
    /// Performs the GetStatus operation.
    /// </summary>
    string GetStatus();

    /// <summary>
    /// Performs the Upload operation.
    /// </summary>
    Task<ObjectResult> Upload(IAuthenticationProvider auth, IMicroMAppConfiguration app_config, IEntitiesService ents, IFileUploadService ups, string app_id, string fileprocess_id, string file_name, int? maxSize, int? quality, CancellationToken ct);
    /// <summary>
    /// Performs the Serve operation.
    /// </summary>
    Task<IActionResult> Serve(IAuthenticationProvider auth, IMicroMAppConfiguration app_config, IEntitiesService ents, IFileUploadService ups, string app_id, string fileguid, CancellationToken ct);
    /// <summary>
    /// Performs the ServeThumbnail operation.
    /// </summary>
    Task<IActionResult> ServeThumbnail(IAuthenticationProvider auth, IMicroMAppConfiguration app_config, IEntitiesService ents, IFileUploadService ups, string app_id, string fileguid, int? maxSize, int? quality, CancellationToken ct);
}
