using MicroM.Data;
using MicroM.Web.Authentication;
using MicroM.Web.Services;
using Microsoft.AspNetCore.Mvc;

namespace MicroM.Web.Controllers;

/// <summary>
/// Defines the contract for controller endpoints that manage entities.
/// </summary>
public interface IEntitiesController
{
    /// <summary>
    /// Returns a simple response indicating that the entities API is available.
    /// </summary>
    string GetStatus();

    /// <summary>
    /// Executes a custom action on the specified entity.
    /// </summary>
    Task<ObjectResult> Action(IAuthenticationProvider auth, IMicroMAppConfiguration app_config, IEntitiesService ents, string app_id, string entityName, string actionName, DataWebAPIRequest parms, CancellationToken ct);
    /// <summary>
    /// Creates a new entity instance.
    /// </summary>
    Task<ObjectResult> Insert(IAuthenticationProvider auth, IMicroMAppConfiguration app_config, IEntitiesService ents, DataWebAPIRequest parms, string app_id, string entityName, CancellationToken ct);
    /// <summary>
    /// Retrieves a single entity by name.
    /// </summary>
    Task<ObjectResult> Get(IAuthenticationProvider auth, IMicroMAppConfiguration app_config, IEntitiesService ents, string app_id, string entityName, DataWebAPIRequest parms, CancellationToken ct);
    /// <summary>
    /// Updates an existing entity instance.
    /// </summary>
    Task<ObjectResult> Update(IAuthenticationProvider auth, IMicroMAppConfiguration app_config, IEntitiesService ents, string app_id, string entityName, DataWebAPIRequest parms, CancellationToken ct);
    /// <summary>
    /// Deletes an entity instance.
    /// </summary>
    Task<ObjectResult> Delete(IAuthenticationProvider auth, IMicroMAppConfiguration app_config, IEntitiesService ents, string app_id, string entityName, DataWebAPIRequest parms, CancellationToken ct);
    /// <summary>
    /// Runs a lookup query for an entity.
    /// </summary>
    Task<ObjectResult> Lookup(IAuthenticationProvider auth, IMicroMAppConfiguration app_config, IEntitiesService ents, string app_id, string entityName, string? lookupName, DataWebAPIRequest parms, CancellationToken ct);
    /// <summary>
    /// Imports data into the specified entity using an optional procedure.
    /// </summary>
    Task<ObjectResult> Import(IAuthenticationProvider auth, IMicroMAppConfiguration app_config, IEntitiesService ents, string app_id, string entityName, string? import_proc, DataWebAPIRequest parms, CancellationToken ct);
    /// <summary>
    /// Executes a view for an entity and returns its results.
    /// </summary>
    Task<ObjectResult> View(IAuthenticationProvider auth, IMicroMAppConfiguration app_config, IEntitiesService ents, string app_id, string entityName, string viewName, DataWebAPIRequest parms, CancellationToken ct);
    /// <summary>
    /// Executes a stored procedure for an entity.
    /// </summary>
    Task<ObjectResult> Proc(IAuthenticationProvider auth, IMicroMAppConfiguration app_config, IEntitiesService ents, string app_id, string entityName, string procName, DataWebAPIRequest parms, CancellationToken ct);
    /// <summary>
    /// Executes a stored procedure that returns status information.
    /// </summary>
    Task<ObjectResult> Process(IAuthenticationProvider auth, IMicroMAppConfiguration app_config, IEntitiesService ents, string app_id, string entityName, string procName, DataWebAPIRequest parms, CancellationToken ct);
    /// <summary>
    /// Retrieves the metadata definition of an entity.
    /// </summary>
    ObjectResult GetDefinition(IEntitiesService ents, IMicroMAppConfiguration app_config, string app_id, string entityName);
    /// <summary>
    /// Gets the time-zone offset in minutes for the server.
    /// </summary>
    Task<int> GetTimeZoneOffset(IAuthenticationProvider auth, IMicroMAppConfiguration app_config, IEntitiesService ents, string app_id, CancellationToken ct);

}
