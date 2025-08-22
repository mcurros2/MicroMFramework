using MicroM.Data;
using MicroM.Web.Authentication;
using MicroM.Web.Services;
using Microsoft.AspNetCore.Mvc;

namespace MicroM.Web.Controllers;

/// <summary>
/// Represents the IEntitiesController.
/// </summary>
public interface IEntitiesController
{
    /// <summary>
    /// Performs the GetStatus operation.
    /// </summary>
    string GetStatus();

    /// <summary>
    /// Performs the Action operation.
    /// </summary>
    Task<ObjectResult> Action(IAuthenticationProvider auth, IMicroMAppConfiguration app_config, IEntitiesService ents, string app_id, string entityName, string actionName, DataWebAPIRequest parms, CancellationToken ct);
    /// <summary>
    /// Performs the Insert operation.
    /// </summary>
    Task<ObjectResult> Insert(IAuthenticationProvider auth, IMicroMAppConfiguration app_config, IEntitiesService ents, DataWebAPIRequest parms, string app_id, string entityName, CancellationToken ct);
    /// <summary>
    /// Performs the Get operation.
    /// </summary>
    Task<ObjectResult> Get(IAuthenticationProvider auth, IMicroMAppConfiguration app_config, IEntitiesService ents, string app_id, string entityName, DataWebAPIRequest parms, CancellationToken ct);
    /// <summary>
    /// Performs the Update operation.
    /// </summary>
    Task<ObjectResult> Update(IAuthenticationProvider auth, IMicroMAppConfiguration app_config, IEntitiesService ents, string app_id, string entityName, DataWebAPIRequest parms, CancellationToken ct);
    /// <summary>
    /// Performs the Delete operation.
    /// </summary>
    Task<ObjectResult> Delete(IAuthenticationProvider auth, IMicroMAppConfiguration app_config, IEntitiesService ents, string app_id, string entityName, DataWebAPIRequest parms, CancellationToken ct);
    /// <summary>
    /// Performs the Lookup operation.
    /// </summary>
    Task<ObjectResult> Lookup(IAuthenticationProvider auth, IMicroMAppConfiguration app_config, IEntitiesService ents, string app_id, string entityName, string? lookupName, DataWebAPIRequest parms, CancellationToken ct);
    /// <summary>
    /// Performs the Import operation.
    /// </summary>
    Task<ObjectResult> Import(IAuthenticationProvider auth, IMicroMAppConfiguration app_config, IEntitiesService ents, string app_id, string entityName, string? import_proc, DataWebAPIRequest parms, CancellationToken ct);
    /// <summary>
    /// Performs the View operation.
    /// </summary>
    Task<ObjectResult> View(IAuthenticationProvider auth, IMicroMAppConfiguration app_config, IEntitiesService ents, string app_id, string entityName, string viewName, DataWebAPIRequest parms, CancellationToken ct);
    /// <summary>
    /// Performs the Proc operation.
    /// </summary>
    Task<ObjectResult> Proc(IAuthenticationProvider auth, IMicroMAppConfiguration app_config, IEntitiesService ents, string app_id, string entityName, string procName, DataWebAPIRequest parms, CancellationToken ct);
    /// <summary>
    /// Performs the Process operation.
    /// </summary>
    Task<ObjectResult> Process(IAuthenticationProvider auth, IMicroMAppConfiguration app_config, IEntitiesService ents, string app_id, string entityName, string procName, DataWebAPIRequest parms, CancellationToken ct);
    /// <summary>
    /// Performs the GetDefinition operation.
    /// </summary>
    ObjectResult GetDefinition(IEntitiesService ents, IMicroMAppConfiguration app_config, string app_id, string entityName);
    /// <summary>
    /// Performs the GetTimeZoneOffset operation.
    /// </summary>
    Task<int> GetTimeZoneOffset(IAuthenticationProvider auth, IMicroMAppConfiguration app_config, IEntitiesService ents, string app_id, CancellationToken ct);

}
