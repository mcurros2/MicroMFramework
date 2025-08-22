using MicroM.Data;
using MicroM.Web.Authentication;
using MicroM.Web.Services;
using Microsoft.AspNetCore.Mvc;

namespace MicroM.Web.Controllers;

/// <summary>
/// HTTP API contract for CRUD and action endpoints on application entities.
/// </summary>
public interface IEntitiesController
{
    /// <summary>
    /// Reports the availability of the entities API.
    /// </summary>
    /// <returns>200 OK with the value "OK" when the API is responsive.</returns>
    string GetStatus();

    /// <summary>
    /// Executes a named action on the specified entity.
    /// </summary>
    /// <param name="auth">Authentication provider used to validate the request and resolve the application.</param>
    /// <param name="app_config">Configuration provider supplying application settings.</param>
    /// <param name="ents">Service used to perform entity operations.</param>
    /// <param name="app_id">Identifier of the application containing the entity.</param>
    /// <param name="entityName">Name of the entity on which to invoke the action.</param>
    /// <param name="actionName">Name of the action to execute.</param>
    /// <param name="parms">Request parameters and payload.</param>
    /// <param name="ct">Token used to cancel the request.</param>
    /// <returns>200 OK with the action result, 400 if the application or result is invalid, 409 if the operation is cancelled.</returns>
    /// <exception cref="OperationCanceledException">The request was cancelled.</exception>
    /// <exception cref="TaskCanceledException">The request was cancelled.</exception>
    Task<ObjectResult> Action(IAuthenticationProvider auth, IMicroMAppConfiguration app_config, IEntitiesService ents, string app_id, string entityName, string actionName, DataWebAPIRequest parms, CancellationToken ct);
    /// <summary>
    /// Inserts a new record into the specified entity.
    /// </summary>
    /// <param name="auth">Authentication provider used to validate the request and resolve the application.</param>
    /// <param name="app_config">Configuration provider supplying application settings.</param>
    /// <param name="ents">Service used to perform entity operations.</param>
    /// <param name="parms">Request parameters and payload.</param>
    /// <param name="app_id">Identifier of the application containing the entity.</param>
    /// <param name="entityName">Name of the entity in which to insert the record.</param>
    /// <param name="ct">Token used to cancel the request.</param>
    /// <returns>200 OK with the insert result, 400 if the application or result is invalid, 409 if the operation is cancelled.</returns>
    /// <exception cref="OperationCanceledException">The request was cancelled.</exception>
    /// <exception cref="TaskCanceledException">The request was cancelled.</exception>
    Task<ObjectResult> Insert(IAuthenticationProvider auth, IMicroMAppConfiguration app_config, IEntitiesService ents, DataWebAPIRequest parms, string app_id, string entityName, CancellationToken ct);
    /// <summary>
    /// Retrieves records for the specified entity.
    /// </summary>
    /// <param name="auth">Authentication provider used to validate the request and resolve the application.</param>
    /// <param name="app_config">Configuration provider supplying application settings.</param>
    /// <param name="ents">Service used to perform entity operations.</param>
    /// <param name="app_id">Identifier of the application containing the entity.</param>
    /// <param name="entityName">Name of the entity to query.</param>
    /// <param name="parms">Request parameters and payload.</param>
    /// <param name="ct">Token used to cancel the request.</param>
    /// <returns>200 OK with the entity data, 400 if the application or result is invalid, 409 if the operation is cancelled.</returns>
    /// <exception cref="OperationCanceledException">The request was cancelled.</exception>
    /// <exception cref="TaskCanceledException">The request was cancelled.</exception>
    Task<ObjectResult> Get(IAuthenticationProvider auth, IMicroMAppConfiguration app_config, IEntitiesService ents, string app_id, string entityName, DataWebAPIRequest parms, CancellationToken ct);
    /// <summary>
    /// Updates an existing record in the specified entity.
    /// </summary>
    /// <param name="auth">Authentication provider used to validate the request and resolve the application.</param>
    /// <param name="app_config">Configuration provider supplying application settings.</param>
    /// <param name="ents">Service used to perform entity operations.</param>
    /// <param name="app_id">Identifier of the application containing the entity.</param>
    /// <param name="entityName">Name of the entity to update.</param>
    /// <param name="parms">Request parameters and payload.</param>
    /// <param name="ct">Token used to cancel the request.</param>
    /// <returns>200 OK with the update result, 400 if the application or result is invalid, 409 if the operation is cancelled.</returns>
    /// <exception cref="OperationCanceledException">The request was cancelled.</exception>
    /// <exception cref="TaskCanceledException">The request was cancelled.</exception>
    Task<ObjectResult> Update(IAuthenticationProvider auth, IMicroMAppConfiguration app_config, IEntitiesService ents, string app_id, string entityName, DataWebAPIRequest parms, CancellationToken ct);
    /// <summary>
    /// Deletes a record from the specified entity.
    /// </summary>
    /// <param name="auth">Authentication provider used to validate the request and resolve the application.</param>
    /// <param name="app_config">Configuration provider supplying application settings.</param>
    /// <param name="ents">Service used to perform entity operations.</param>
    /// <param name="app_id">Identifier of the application containing the entity.</param>
    /// <param name="entityName">Name of the entity that contains the record to delete.</param>
    /// <param name="parms">Request parameters and payload.</param>
    /// <param name="ct">Token used to cancel the request.</param>
    /// <returns>200 OK with the delete result, 400 if the application or result is invalid, 409 if the operation is cancelled.</returns>
    /// <exception cref="OperationCanceledException">The request was cancelled.</exception>
    /// <exception cref="TaskCanceledException">The request was cancelled.</exception>
    Task<ObjectResult> Delete(IAuthenticationProvider auth, IMicroMAppConfiguration app_config, IEntitiesService ents, string app_id, string entityName, DataWebAPIRequest parms, CancellationToken ct);
    /// <summary>
    /// Performs a lookup query on the specified entity.
    /// </summary>
    /// <param name="auth">Authentication provider used to validate the request and resolve the application.</param>
    /// <param name="app_config">Configuration provider supplying application settings.</param>
    /// <param name="ents">Service used to perform entity operations.</param>
    /// <param name="app_id">Identifier of the application containing the entity.</param>
    /// <param name="entityName">Name of the entity to query.</param>
    /// <param name="lookupName">Optional name of the lookup to execute.</param>
    /// <param name="parms">Request parameters and payload.</param>
    /// <param name="ct">Token used to cancel the request.</param>
    /// <returns>200 OK with the lookup result, 400 if the application is not found, 200 OK with null if the operation is cancelled.</returns>
    /// <exception cref="OperationCanceledException">The request was cancelled.</exception>
    /// <exception cref="TaskCanceledException">The request was cancelled.</exception>
    Task<ObjectResult> Lookup(IAuthenticationProvider auth, IMicroMAppConfiguration app_config, IEntitiesService ents, string app_id, string entityName, string? lookupName, DataWebAPIRequest parms, CancellationToken ct);
    /// <summary>
    /// Imports data into the specified entity using an optional import procedure.
    /// </summary>
    /// <param name="auth">Authentication provider used to validate the request and resolve the application.</param>
    /// <param name="app_config">Configuration provider supplying application settings.</param>
    /// <param name="ents">Service used to perform entity operations.</param>
    /// <param name="app_id">Identifier of the application containing the entity.</param>
    /// <param name="entityName">Name of the entity receiving the data.</param>
    /// <param name="import_proc">Optional name of the import procedure to execute.</param>
    /// <param name="parms">Request parameters and payload.</param>
    /// <param name="ct">Token used to cancel the request.</param>
    /// <returns>200 OK with the import result, 400 if the application or result is invalid, 409 if the operation is cancelled.</returns>
    /// <exception cref="OperationCanceledException">The request was cancelled.</exception>
    /// <exception cref="TaskCanceledException">The request was cancelled.</exception>
    Task<ObjectResult> Import(IAuthenticationProvider auth, IMicroMAppConfiguration app_config, IEntitiesService ents, string app_id, string entityName, string? import_proc, DataWebAPIRequest parms, CancellationToken ct);
    /// <summary>
    /// Executes a predefined view for the specified entity.
    /// </summary>
    /// <param name="auth">Authentication provider used to validate the request and resolve the application.</param>
    /// <param name="app_config">Configuration provider supplying application settings.</param>
    /// <param name="ents">Service used to perform entity operations.</param>
    /// <param name="app_id">Identifier of the application containing the entity.</param>
    /// <param name="entityName">Name of the entity associated with the view.</param>
    /// <param name="viewName">Name of the view to execute.</param>
    /// <param name="parms">Request parameters and payload.</param>
    /// <param name="ct">Token used to cancel the request.</param>
    /// <returns>200 OK with the view result, 400 if the application or result is invalid, 409 if the operation is cancelled.</returns>
    /// <exception cref="OperationCanceledException">The request was cancelled.</exception>
    /// <exception cref="TaskCanceledException">The request was cancelled.</exception>
    Task<ObjectResult> View(IAuthenticationProvider auth, IMicroMAppConfiguration app_config, IEntitiesService ents, string app_id, string entityName, string viewName, DataWebAPIRequest parms, CancellationToken ct);
    /// <summary>
    /// Executes a stored procedure for the specified entity.
    /// </summary>
    /// <param name="auth">Authentication provider used to validate the request and resolve the application.</param>
    /// <param name="app_config">Configuration provider supplying application settings.</param>
    /// <param name="ents">Service used to perform entity operations.</param>
    /// <param name="app_id">Identifier of the application containing the entity.</param>
    /// <param name="entityName">Name of the entity associated with the procedure.</param>
    /// <param name="procName">Name of the stored procedure to execute.</param>
    /// <param name="parms">Request parameters and payload.</param>
    /// <param name="ct">Token used to cancel the request.</param>
    /// <returns>200 OK with the procedure result, 400 if the application or result is invalid, 409 if the operation is cancelled.</returns>
    /// <exception cref="OperationCanceledException">The request was cancelled.</exception>
    /// <exception cref="TaskCanceledException">The request was cancelled.</exception>
    Task<ObjectResult> Proc(IAuthenticationProvider auth, IMicroMAppConfiguration app_config, IEntitiesService ents, string app_id, string entityName, string procName, DataWebAPIRequest parms, CancellationToken ct);
    /// <summary>
    /// Executes a database process for the specified entity and returns status information.
    /// </summary>
    /// <param name="auth">Authentication provider used to validate the request and resolve the application.</param>
    /// <param name="app_config">Configuration provider supplying application settings.</param>
    /// <param name="ents">Service used to perform entity operations.</param>
    /// <param name="app_id">Identifier of the application containing the entity.</param>
    /// <param name="entityName">Name of the entity associated with the process.</param>
    /// <param name="procName">Name of the process to execute.</param>
    /// <param name="parms">Request parameters and payload.</param>
    /// <param name="ct">Token used to cancel the request.</param>
    /// <returns>200 OK with the process result, 400 if the application or result is invalid, 409 if the operation is cancelled.</returns>
    /// <exception cref="OperationCanceledException">The request was cancelled.</exception>
    /// <exception cref="TaskCanceledException">The request was cancelled.</exception>
    Task<ObjectResult> Process(IAuthenticationProvider auth, IMicroMAppConfiguration app_config, IEntitiesService ents, string app_id, string entityName, string procName, DataWebAPIRequest parms, CancellationToken ct);
    /// <summary>
    /// Retrieves the schema definition for the specified entity.
    /// </summary>
    /// <param name="ents">Service used to perform entity operations.</param>
    /// <param name="app_config">Configuration provider supplying application settings.</param>
    /// <param name="app_id">Identifier of the application containing the entity.</param>
    /// <param name="entityName">Name of the entity whose definition is requested.</param>
    /// <returns>200 OK with the entity definition, 400 if the application or result is invalid, 409 if the operation is cancelled.</returns>
    /// <exception cref="OperationCanceledException">The request was cancelled.</exception>
    /// <exception cref="TaskCanceledException">The request was cancelled.</exception>
    ObjectResult GetDefinition(IEntitiesService ents, IMicroMAppConfiguration app_config, string app_id, string entityName);
    /// <summary>
    /// Retrieves the time zone offset for the current application.
    /// </summary>
    /// <param name="auth">Authentication provider used to validate the request and resolve the application.</param>
    /// <param name="app_config">Configuration provider supplying application settings.</param>
    /// <param name="ents">Service used to perform entity operations.</param>
    /// <param name="app_id">Identifier of the application containing the entity.</param>
    /// <param name="ct">Token used to cancel the request.</param>
    /// <returns>200 OK with the offset in minutes; 0 if the application is not found or the operation is cancelled.</returns>
    /// <exception cref="OperationCanceledException">The request was cancelled.</exception>
    /// <exception cref="TaskCanceledException">The request was cancelled.</exception>
    Task<int> GetTimeZoneOffset(IAuthenticationProvider auth, IMicroMAppConfiguration app_config, IEntitiesService ents, string app_id, CancellationToken ct);

}
