using MicroM.Data;
using MicroM.Web.Services;
using Microsoft.AspNetCore.Mvc;

namespace MicroM.Web.Controllers;

/// <summary>
/// Defines the contract for anonymous entity access endpoints.
/// </summary>
public interface IPublicController
{
    /// <summary>
    /// Returns a simple response indicating that the public API is available.
    /// </summary>
    string GetStatus();

    /// <summary>
    /// Executes a public action on an entity.
    /// </summary>
    Task<ObjectResult> PublicAction(IMicroMAppConfiguration app_config, IEntitiesService api, string app_id, string entityName, string actionName, DataWebAPIRequest parms, CancellationToken ct);
    /// <summary>
    /// Deletes an entity using public access.
    /// </summary>
    Task<ObjectResult> PublicDelete(IMicroMAppConfiguration app_config, IEntitiesService api, string app_id, string entityName, DataWebAPIRequest parms, CancellationToken ct);
    /// <summary>
    /// Retrieves an entity using public access.
    /// </summary>
    Task<ObjectResult> PublicGet(IMicroMAppConfiguration app_config, IEntitiesService api, string app_id, string entityName, DataWebAPIRequest parms, CancellationToken ct);
    /// <summary>
    /// Creates an entity using public access.
    /// </summary>
    Task<ObjectResult> PublicInsert(IMicroMAppConfiguration app_config, IEntitiesService api, DataWebAPIRequest parms, string app_id, string entityName, CancellationToken ct);
    /// <summary>
    /// Runs a lookup query on an entity using public access.
    /// </summary>
    Task<ObjectResult> PublicLookup(IMicroMAppConfiguration app_config, IEntitiesService api, string app_id, string entityName, string? lookupName, DataWebAPIRequest parms, CancellationToken ct);
    /// <summary>
    /// Executes a stored procedure on an entity using public access.
    /// </summary>
    Task<ObjectResult> PublicProc(IMicroMAppConfiguration app_config, IEntitiesService api, string app_id, string entityName, string procName, DataWebAPIRequest parms, CancellationToken ct);
    /// <summary>
    /// Executes a stored procedure that returns status information using public access.
    /// </summary>
    Task<ObjectResult> PublicProcess(IMicroMAppConfiguration app_config, IEntitiesService api, string app_id, string entityName, string procName, DataWebAPIRequest parms, CancellationToken ct);
    /// <summary>
    /// Updates an entity using public access.
    /// </summary>
    Task<ObjectResult> PublicUpdate(IMicroMAppConfiguration app_config, IEntitiesService api, string app_id, string entityName, DataWebAPIRequest parms, CancellationToken ct);
    /// <summary>
    /// Executes a view on an entity using public access.
    /// </summary>
    Task<ObjectResult> PublicView(IMicroMAppConfiguration app_config, IEntitiesService api, string app_id, string entityName, string viewName, DataWebAPIRequest parms, CancellationToken ct);

}
