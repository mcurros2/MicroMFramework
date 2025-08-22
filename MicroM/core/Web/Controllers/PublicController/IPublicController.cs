using MicroM.Data;
using MicroM.Web.Services;
using MicroM.Web.Services.Security;
using Microsoft.AspNetCore.Mvc;

namespace MicroM.Web.Controllers;

/// <summary>
/// Defines the contract for controllers exposing unauthenticated entity operations to public clients.
/// </summary>
public interface IPublicController
{
    /// <summary>
    /// Performs the GetStatus operation.
    /// </summary>
    string GetStatus();

    /// <summary>
    /// Executes an unauthenticated action on the specified entity.
    /// Implementations should apply <see cref="PublicEndpointAttribute"/> to allow public access without authentication.
    /// </summary>
    /// <param name="app_config">Application configuration provider.</param>
    /// <param name="api">Service used to execute entity operations.</param>
    /// <param name="app_id">Identifier of the application containing the entity.</param>
    /// <param name="entityName">Name of the entity on which to execute the action.</param>
    /// <param name="actionName">Name of the action to execute.</param>
    /// <param name="parms">Request body containing action parameters.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>An <see cref="ObjectResult"/> containing the action execution result.</returns>
    Task<ObjectResult> PublicAction(IMicroMAppConfiguration app_config, IEntitiesService api, string app_id, string entityName, string actionName, DataWebAPIRequest parms, CancellationToken ct);
    /// <summary>
    /// Executes an unauthenticated delete on the specified entity.
    /// Implementations should apply <see cref="PublicEndpointAttribute"/> to allow public access without authentication.
    /// </summary>
    /// <param name="app_config">Application configuration provider.</param>
    /// <param name="api">Service used to execute entity operations.</param>
    /// <param name="app_id">Identifier of the application containing the entity.</param>
    /// <param name="entityName">Name of the entity to delete.</param>
    /// <param name="parms">Request body containing delete parameters.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>An <see cref="ObjectResult"/> containing the delete result.</returns>
    Task<ObjectResult> PublicDelete(IMicroMAppConfiguration app_config, IEntitiesService api, string app_id, string entityName, DataWebAPIRequest parms, CancellationToken ct);
    /// <summary>
    /// Executes an unauthenticated retrieval of the specified entity.
    /// Implementations should apply <see cref="PublicEndpointAttribute"/> to allow public access without authentication.
    /// </summary>
    /// <param name="app_config">Application configuration provider.</param>
    /// <param name="api">Service used to execute entity operations.</param>
    /// <param name="app_id">Identifier of the application containing the entity.</param>
    /// <param name="entityName">Name of the entity to retrieve.</param>
    /// <param name="parms">Request body containing retrieval parameters.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>An <see cref="ObjectResult"/> containing the retrieved entity.</returns>
    Task<ObjectResult> PublicGet(IMicroMAppConfiguration app_config, IEntitiesService api, string app_id, string entityName, DataWebAPIRequest parms, CancellationToken ct);
    /// <summary>
    /// Executes an unauthenticated insert on the specified entity.
    /// Implementations should apply <see cref="PublicEndpointAttribute"/> to allow public access without authentication.
    /// </summary>
    /// <param name="app_config">Application configuration provider.</param>
    /// <param name="api">Service used to execute entity operations.</param>
    /// <param name="parms">Request body containing insert parameters.</param>
    /// <param name="app_id">Identifier of the application containing the entity.</param>
    /// <param name="entityName">Name of the entity to insert.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>An <see cref="ObjectResult"/> containing the insert result.</returns>
    Task<ObjectResult> PublicInsert(IMicroMAppConfiguration app_config, IEntitiesService api, DataWebAPIRequest parms, string app_id, string entityName, CancellationToken ct);
    /// <summary>
    /// Executes an unauthenticated lookup on the specified entity.
    /// Implementations should apply <see cref="PublicEndpointAttribute"/> to allow public access without authentication.
    /// </summary>
    /// <param name="app_config">Application configuration provider.</param>
    /// <param name="api">Service used to execute entity operations.</param>
    /// <param name="app_id">Identifier of the application containing the entity.</param>
    /// <param name="entityName">Name of the entity to look up.</param>
    /// <param name="lookupName">Optional name of the lookup to execute.</param>
    /// <param name="parms">Request body containing lookup parameters.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>An <see cref="ObjectResult"/> containing the lookup results.</returns>
    Task<ObjectResult> PublicLookup(IMicroMAppConfiguration app_config, IEntitiesService api, string app_id, string entityName, string? lookupName, DataWebAPIRequest parms, CancellationToken ct);
    /// <summary>
    /// Executes an unauthenticated stored procedure on the specified entity.
    /// Implementations should apply <see cref="PublicEndpointAttribute"/> to allow public access without authentication.
    /// </summary>
    /// <param name="app_config">Application configuration provider.</param>
    /// <param name="api">Service used to execute entity operations.</param>
    /// <param name="app_id">Identifier of the application containing the entity.</param>
    /// <param name="entityName">Name of the entity that owns the procedure.</param>
    /// <param name="procName">Name of the stored procedure to execute.</param>
    /// <param name="parms">Request body containing procedure parameters.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>An <see cref="ObjectResult"/> containing the procedure results.</returns>
    Task<ObjectResult> PublicProc(IMicroMAppConfiguration app_config, IEntitiesService api, string app_id, string entityName, string procName, DataWebAPIRequest parms, CancellationToken ct);
    /// <summary>
    /// Executes an unauthenticated process on the specified entity.
    /// Implementations should apply <see cref="PublicEndpointAttribute"/> to allow public access without authentication.
    /// </summary>
    /// <param name="app_config">Application configuration provider.</param>
    /// <param name="api">Service used to execute entity operations.</param>
    /// <param name="app_id">Identifier of the application containing the entity.</param>
    /// <param name="entityName">Name of the entity that owns the process.</param>
    /// <param name="procName">Name of the process to execute.</param>
    /// <param name="parms">Request body containing process parameters.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>An <see cref="ObjectResult"/> containing the process results.</returns>
    Task<ObjectResult> PublicProcess(IMicroMAppConfiguration app_config, IEntitiesService api, string app_id, string entityName, string procName, DataWebAPIRequest parms, CancellationToken ct);
    /// <summary>
    /// Executes an unauthenticated update on the specified entity.
    /// Implementations should apply <see cref="PublicEndpointAttribute"/> to allow public access without authentication.
    /// </summary>
    /// <param name="app_config">Application configuration provider.</param>
    /// <param name="api">Service used to execute entity operations.</param>
    /// <param name="app_id">Identifier of the application containing the entity.</param>
    /// <param name="entityName">Name of the entity to update.</param>
    /// <param name="parms">Request body containing update parameters.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>An <see cref="ObjectResult"/> containing the update result.</returns>
    Task<ObjectResult> PublicUpdate(IMicroMAppConfiguration app_config, IEntitiesService api, string app_id, string entityName, DataWebAPIRequest parms, CancellationToken ct);
    /// <summary>
    /// Executes an unauthenticated view on the specified entity.
    /// Implementations should apply <see cref="PublicEndpointAttribute"/> to allow public access without authentication.
    /// </summary>
    /// <param name="app_config">Application configuration provider.</param>
    /// <param name="api">Service used to execute entity operations.</param>
    /// <param name="app_id">Identifier of the application containing the entity.</param>
    /// <param name="entityName">Name of the entity that owns the view.</param>
    /// <param name="viewName">Name of the view to execute.</param>
    /// <param name="parms">Request body containing view parameters.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>An <see cref="ObjectResult"/> containing the view results.</returns>
    Task<ObjectResult> PublicView(IMicroMAppConfiguration app_config, IEntitiesService api, string app_id, string entityName, string viewName, DataWebAPIRequest parms, CancellationToken ct);

}
