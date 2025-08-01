using MicroM.Web.Authentication;
using MicroM.Web.Services;
using Microsoft.AspNetCore.Mvc;

namespace MicroM.Web.Controllers;

public interface IFileController
{
    string GetStatus();

    Task<ObjectResult> Upload(IAuthenticationProvider auth, IMicroMAppConfiguration app_config, IEntitiesService ents, IFileUploadService ups, string app_id, string fileprocess_id, string file_name, int? maxSize, int? quality, CancellationToken ct);
    Task<IActionResult> Serve(IAuthenticationProvider auth, IMicroMAppConfiguration app_config, IEntitiesService ents, IFileUploadService ups, string app_id, string fileguid, CancellationToken ct);
    Task<IActionResult> ServeThumbnail(IAuthenticationProvider auth, IMicroMAppConfiguration app_config, IEntitiesService ents, IFileUploadService ups, string app_id, string fileguid, int? maxSize, int? quality, CancellationToken ct);
}
