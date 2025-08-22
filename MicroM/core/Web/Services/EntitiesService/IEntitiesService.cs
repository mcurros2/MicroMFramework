using MicroM.Configuration;
using MicroM.Core;
using MicroM.Data;
using MicroM.ImportData;

namespace MicroM.Web.Services;

/// <summary>
/// Defines operations for entity CRUD, lookups, imports and actions.
/// </summary>
public interface IEntitiesService
{
    /// <summary>
    /// Instantiates an entity type and binds it to the supplied or new connection.
    /// </summary>
    /// <param name="app">Application-specific configuration.</param>
    /// <param name="entity_name">Name of the entity to instantiate.</param>
    /// <param name="server_claims">Server claims for user context.</param>
    /// <param name="ec">Existing connection to reuse; if <c>null</c> a new one is created.</param>
    /// <returns>The instantiated entity or <c>null</c> if the type is not found.</returns>
    public EntityBase? CreateEntity(ApplicationOption app, string entity_name, Dictionary<string, object>? server_claims, IEntityClient? ec = null);

    /// <summary>
    /// Asynchronously creates an entity using a newly established connection.
    /// </summary>
    /// <param name="app">Application-specific configuration.</param>
    /// <param name="entity_name">Name of the entity to instantiate.</param>
    /// <param name="server_claims">Server claims for user context.</param>
    /// <param name="ct">Token used to cancel the operation.</param>
    /// <returns>The instantiated entity or <c>null</c> if the type is not found.</returns>
    public EntityBase? CreateEntity(ApplicationOption app, string entity_name, Dictionary<string, object>? server_claims, CancellationToken ct);

    /// <summary>
    /// Creates a database connection with user and device information.
    /// </summary>
    /// <param name="app">Application-specific configuration.</param>
    /// <param name="server_claims">Server claims that may include user and device information.</param>
    /// <returns>A configured <see cref="IEntityClient"/>.</returns>
    public IEntityClient CreateDbConnection(ApplicationOption app, Dictionary<string, object>? server_claims);

    /// <summary>
    /// Asynchronously creates a database connection with user and device context.
    /// </summary>
    /// <param name="app">Application-specific configuration.</param>
    /// <param name="server_claims">Server claims that may include user and device information.</param>
    /// <param name="ct">Token used to cancel the operation.</param>
    /// <returns>A task that returns the configured <see cref="IEntityClient"/>.</returns>
    public Task<IEntityClient> CreateDbConnection(ApplicationOption app, Dictionary<string, object>? server_claims, CancellationToken ct);

    // TODO: dynamic entities

    /// <summary>
    /// Retrieves metadata definition for the specified entity.
    /// </summary>
    /// <param name="app">Application context.</param>
    /// <param name="entity_name">Name of the entity.</param>
    /// <returns>The entity definition or <c>null</c> if the type is not found.</returns>
    public EntityDefinition? HandleGetEntityDefinition(ApplicationOption app, string entity_name);

    /// <summary>
    /// Retrieves an entity record using the provided key values.
    /// </summary>
    /// <param name="app">Application context.</param>
    /// <param name="entity_name">Name of the entity to retrieve.</param>
    /// <param name="parms">Request containing key values and additional options.</param>
    /// <param name="ec">Database client used for the operation.</param>
    /// <param name="ct">Token used to cancel the operation.</param>
    /// <returns>A dictionary of column values or <c>null</c> if the entity is not found.</returns>
    public Task<Dictionary<string, object?>?> HandleGetEntity(ApplicationOption app, string entity_name, DataWebAPIRequest parms, IEntityClient ec, CancellationToken ct);

    /// <summary>
    /// Updates existing entity records with the supplied values.
    /// </summary>
    /// <param name="app">Application context.</param>
    /// <param name="entity_name">Name of the entity to update.</param>
    /// <param name="parms">Request containing column values and selection data.</param>
    /// <param name="ec">Database client used for the operation.</param>
    /// <param name="ct">Token used to cancel the operation.</param>
    /// <returns>Aggregated status for the update operation or <c>null</c> if the entity is not found.</returns>
    public Task<DBStatusResult?> HandleUpdateEntity(ApplicationOption app, string entity_name, DataWebAPIRequest parms, IEntityClient ec, CancellationToken ct);

    /// <summary>
    /// Deletes entity records matching the supplied parameters.
    /// </summary>
    /// <param name="app">Application context.</param>
    /// <param name="entity_name">Name of the entity to delete.</param>
    /// <param name="parms">Request values and selection criteria.</param>
    /// <param name="ec">Database client used for the operation.</param>
    /// <param name="ct">Token used to cancel the operation.</param>
    /// <returns>Aggregated status for the delete operation or <c>null</c> if the entity is not found.</returns>
    public Task<DBStatusResult?> HandleDeleteEntity(ApplicationOption app, string entity_name, DataWebAPIRequest parms, IEntityClient ec, CancellationToken ct);

    /// <summary>
    /// Inserts new entity records using the supplied values.
    /// </summary>
    /// <param name="app">Application context.</param>
    /// <param name="entity_name">Name of the entity to insert.</param>
    /// <param name="parms">Request containing column values and selection data.</param>
    /// <param name="ec">Database client used for the operation.</param>
    /// <param name="ct">Token used to cancel the operation.</param>
    /// <returns>Aggregated status for the insert operation or <c>null</c> if the entity is not found.</returns>
    public Task<DBStatusResult?> HandleInsertEntity(ApplicationOption app, string entity_name, DataWebAPIRequest parms, IEntityClient ec, CancellationToken ct);

    /// <summary>
    /// Performs a lookup for an entity and returns a descriptive value.
    /// </summary>
    /// <param name="app">Application context.</param>
    /// <param name="entity_name">Name of the entity.</param>
    /// <param name="parms">Request containing key values.</param>
    /// <param name="ec">Database client used for the operation.</param>
    /// <param name="ct">Token used to cancel the operation.</param>
    /// <param name="lookup_name">Optional lookup definition name.</param>
    /// <returns>A <see cref="LookupResult"/> containing the description.</returns>
    public Task<LookupResult> HandleLookupEntity(ApplicationOption app, string entity_name, DataWebAPIRequest parms, IEntityClient ec, CancellationToken ct, string? lookup_name = null);

    /// <summary>
    /// Executes a view defined on the entity and returns its result sets.
    /// </summary>
    /// <param name="app">Application context.</param>
    /// <param name="entity_name">Name of the entity that defines the view.</param>
    /// <param name="view_name">Name of the view to execute.</param>
    /// <param name="parms">Request values applied to the view.</param>
    /// <param name="ec">Database client used for the operation.</param>
    /// <param name="ct">Token used to cancel the operation.</param>
    /// <returns>A list of <see cref="DataResult"/> objects or <c>null</c> if the entity is not found.</returns>
    public Task<List<DataResult>?> HandleExecuteView(ApplicationOption app, string entity_name, string view_name, DataWebAPIRequest parms, IEntityClient ec, CancellationToken ct);

    /// <summary>
    /// Executes an entity-defined stored procedure and returns its result sets.
    /// </summary>
    /// <param name="app">Application context.</param>
    /// <param name="entity_name">Name of the entity that defines the procedure.</param>
    /// <param name="proc_name">Name of the procedure to execute.</param>
    /// <param name="parms">Request values applied to the procedure.</param>
    /// <param name="ec">Database client used for the operation.</param>
    /// <param name="ct">Token used to cancel the operation.</param>
    /// <returns>A list of <see cref="DataResult"/> objects or <c>null</c> if the entity is not found.</returns>
    public Task<List<DataResult>?> HandleExecuteProc(ApplicationOption app, string entity_name, string proc_name, DataWebAPIRequest parms, IEntityClient ec, CancellationToken ct);

    /// <summary>
    /// Executes an entity-defined stored procedure and returns database status information.
    /// </summary>
    /// <param name="app">Application context.</param>
    /// <param name="entity_name">Name of the entity that defines the procedure.</param>
    /// <param name="proc_name">Name of the procedure to execute.</param>
    /// <param name="parms">Request values applied to the procedure.</param>
    /// <param name="ec">Database client used for the operation.</param>
    /// <param name="ct">Token used to cancel the operation.</param>
    /// <returns>Status results produced by the procedure or <c>null</c> if the entity is not found.</returns>
    public Task<DBStatusResult?> HandleExecuteProcDBStatus(ApplicationOption app, string entity_name, string proc_name, DataWebAPIRequest parms, IEntityClient ec, CancellationToken ct);

    /// <summary>
    /// Executes a named action on the entity using the supplied parameters.
    /// </summary>
    /// <param name="app">Application context.</param>
    /// <param name="entity_name">Name of the entity.</param>
    /// <param name="entity_action">Action identifier to execute.</param>
    /// <param name="parms">Request values passed to the action.</param>
    /// <param name="ec">Database client used for the operation.</param>
    /// <param name="ct">Token used to cancel the operation.</param>
    /// <returns>The action result or <c>null</c> if the entity is not found.</returns>
    public Task<EntityActionResult?> HandleExecuteAction(ApplicationOption app, string entity_name, string entity_action, DataWebAPIRequest parms, IEntityClient ec, CancellationToken ct);

    /// <summary>
    /// Imports data for an entity from an uploaded file using an optional import procedure.
    /// </summary>
    /// <param name="app">Application context.</param>
    /// <param name="entity_name">Name of the target entity.</param>
    /// <param name="import_proc">Optional name of the import procedure to execute.</param>
    /// <param name="parms">Request containing file and parameter information.</param>
    /// <param name="ec">Database client used for the operation.</param>
    /// <param name="ct">Token used to cancel the operation.</param>
    /// <returns>Result of the import process or <c>null</c> on failure.</returns>
    public Task<CSVImportResult?> HandleImportData(ApplicationOption app, string entity_name, string? import_proc, DataWebAPIRequest parms, IEntityClient ec, CancellationToken ct);

    /// <summary>
    /// Gets the application's time zone offset, caching the result for subsequent calls.
    /// </summary>
    /// <param name="app">Application context.</param>
    /// <param name="ec">Database client used to query the offset.</param>
    /// <param name="ct">Token used to cancel the operation.</param>
    /// <returns>The time zone offset in minutes.</returns>
    public Task<int> HandleGetTimeZoneOffset(ApplicationOption app, IEntityClient ec, CancellationToken ct);

    /// <summary>
    /// Stores or replaces an application-level key value in the cache.
    /// </summary>
    /// <param name="app_id">Application identifier.</param>
    /// <param name="key">Key name to replace.</param>
    /// <param name="value">New value for the key.</param>
    public void ReplaceApplicationKey(string app_id, string key, string value);

    /// <summary>
    /// Merges cached application-level keys into the provided dictionary.
    /// </summary>
    /// <param name="app_id">Application identifier whose keys are applied.</param>
    /// <param name="values">Dictionary to augment with key values. Modified in place.</param>
    public void EnsureApplicationKeys(string app_id, Dictionary<string, object> values);

    /// <summary>
    /// Retrieves cached application-level keys for the specified application.
    /// </summary>
    /// <param name="app_id">Application identifier.</param>
    /// <returns>A dictionary containing the application's keys or an empty dictionary.</returns>
    public Dictionary<string, object> GetApplicationKeys(string app_id);
}
