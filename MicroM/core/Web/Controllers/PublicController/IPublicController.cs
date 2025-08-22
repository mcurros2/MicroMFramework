using MicroM.Data;
using MicroM.Web.Services;
using Microsoft.AspNetCore.Mvc;

namespace MicroM.Web.Controllers;

/// <summary>
/// Represents the IPublicController.
/// </summary>
public interface IPublicController
{
    /// <summary>
    /// Performs the GetStatus operation.
    /// </summary>
    string GetStatus();

    /// <summary>
    /// Performs the PublicAction operation.
    /// </summary>
    Task<ObjectResult> PublicAction(IMicroMAppConfiguration app_config, IEntitiesService api, string app_id, string entityName, string actionName, DataWebAPIRequest parms, CancellationToken ct);
    /// <summary>
    /// Performs the PublicDelete operation.
    /// </summary>
    Task<ObjectResult> PublicDelete(IMicroMAppConfiguration app_config, IEntitiesService api, string app_id, string entityName, DataWebAPIRequest parms, CancellationToken ct);
    /// <summary>
    /// Performs the PublicGet operation.
    /// </summary>
    Task<ObjectResult> PublicGet(IMicroMAppConfiguration app_config, IEntitiesService api, string app_id, string entityName, DataWebAPIRequest parms, CancellationToken ct);
    /// <summary>
    /// Performs the PublicInsert operation.
    /// </summary>
    Task<ObjectResult> PublicInsert(IMicroMAppConfiguration app_config, IEntitiesService api, DataWebAPIRequest parms, string app_id, string entityName, CancellationToken ct);
    /// <summary>
    /// Performs the PublicLookup operation.
    /// </summary>
    Task<ObjectResult> PublicLookup(IMicroMAppConfiguration app_config, IEntitiesService api, string app_id, string entityName, string? lookupName, DataWebAPIRequest parms, CancellationToken ct);
    /// <summary>
    /// Performs the PublicProc operation.
    /// </summary>
    Task<ObjectResult> PublicProc(IMicroMAppConfiguration app_config, IEntitiesService api, string app_id, string entityName, string procName, DataWebAPIRequest parms, CancellationToken ct);
    /// <summary>
    /// Performs the PublicProcess operation.
    /// </summary>
    Task<ObjectResult> PublicProcess(IMicroMAppConfiguration app_config, IEntitiesService api, string app_id, string entityName, string procName, DataWebAPIRequest parms, CancellationToken ct);
    /// <summary>
    /// Performs the PublicUpdate operation.
    /// </summary>
    Task<ObjectResult> PublicUpdate(IMicroMAppConfiguration app_config, IEntitiesService api, string app_id, string entityName, DataWebAPIRequest parms, CancellationToken ct);
    /// <summary>
    /// Performs the PublicView operation.
    /// </summary>
    Task<ObjectResult> PublicView(IMicroMAppConfiguration app_config, IEntitiesService api, string app_id, string entityName, string viewName, DataWebAPIRequest parms, CancellationToken ct);

}
