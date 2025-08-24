using MicroM.Configuration;
using MicroM.Core;
using MicroM.Data;
using MicroM.ImportData;

namespace MicroM.Web.Services;

/// <summary>
/// Represents the IEntitiesService.
/// </summary>
public interface IEntitiesService
{
    /// <summary>
    /// Creates an Entity if it exists in the configured assembly.
    /// </summary>
    /// <param name="entity_name"></param>
    /// <param name="ec"></param>
    /// <returns></returns>
    public EntityBase? CreateEntity(ApplicationOption app, string entity_name, Dictionary<string, object>? server_claims, IEntityClient? ec = null);
    /// <summary>
    /// Creates an entity instance using the provided cancellation token.
    /// </summary>
    EntityBase? CreateEntity(ApplicationOption app, string entity_name, Dictionary<string, object>? server_claims, CancellationToken ct);

    /// <summary>
    /// Creates a new database client configured for the specified application.
    /// </summary>
    /// <param name="app">Application configuration.</param>
    /// <param name="server_claims">Claims used to resolve SQL credentials when required.</param>
    /// <returns>A configured <see cref="IEntityClient"/>.</returns>
    public IEntityClient CreateDbConnection(ApplicationOption app, Dictionary<string, object>? server_claims);

    /// <summary>
    /// Asynchronously creates a new database client for the specified application.
    /// </summary>
    /// <param name="app">Application configuration.</param>
    /// <param name="server_claims">Claims used to resolve SQL credentials when required.</param>
    /// <param name="ct">Token to observe for cancellation.</param>
    /// <returns>A task producing a configured <see cref="IEntityClient"/>.</returns>
    /// <remarks>Throws <see cref="OperationCanceledException"/> when <paramref name="ct"/> is cancelled.</remarks>
    public Task<IEntityClient> CreateDbConnection(ApplicationOption app, Dictionary<string, object>? server_claims, CancellationToken ct);

    // TODO: dynamic entities

    /// <summary>
    /// Retrieves definition metadata for an entity.
    /// </summary>
    EntityDefinition? HandleGetEntityDefinition(ApplicationOption app, string entity_name);

    /// <summary>
    /// Retrieves a single entity record.
    /// </summary>
    Task<Dictionary<string, object?>?> HandleGetEntity(ApplicationOption app, string entity_name, DataWebAPIRequest parms, IEntityClient ec, CancellationToken ct);

    /// <summary>
    /// Updates an entity record.
    /// </summary>
    Task<DBStatusResult?> HandleUpdateEntity(ApplicationOption app, string entity_name, DataWebAPIRequest parms, IEntityClient ec, CancellationToken ct);

    /// <summary>
    /// Deletes entity records.
    /// </summary>
    Task<DBStatusResult?> HandleDeleteEntity(ApplicationOption app, string entity_name, DataWebAPIRequest parms, IEntityClient ec, CancellationToken ct);

    /// <summary>
    /// Inserts a new entity record.
    /// </summary>
    Task<DBStatusResult?> HandleInsertEntity(ApplicationOption app, string entity_name, DataWebAPIRequest parms, IEntityClient ec, CancellationToken ct);

    /// <summary>
    /// Performs a lookup for entity values.
    /// </summary>
    Task<LookupResult> HandleLookupEntity(ApplicationOption app, string entity_name, DataWebAPIRequest parms, IEntityClient ec, CancellationToken ct, string? lookup_name = null);

    /// <summary>
    /// Executes a view and returns its data.
    /// </summary>
    Task<List<DataResult>?> HandleExecuteView(ApplicationOption app, string entity_name, string view_name, DataWebAPIRequest parms, IEntityClient ec, CancellationToken ct);

    /// <summary>
    /// Executes a stored procedure and returns its data.
    /// </summary>
    Task<List<DataResult>?> HandleExecuteProc(ApplicationOption app, string entity_name, string proc_name, DataWebAPIRequest parms, IEntityClient ec, CancellationToken ct);

    /// <summary>
    /// Executes a stored procedure returning database status information.
    /// </summary>
    Task<DBStatusResult?> HandleExecuteProcDBStatus(ApplicationOption app, string entity_name, string proc_name, DataWebAPIRequest parms, IEntityClient ec, CancellationToken ct);

    /// <summary>
    /// Executes an entity action.
    /// </summary>
    Task<EntityActionResult?> HandleExecuteAction(ApplicationOption app, string entity_name, string entity_action, DataWebAPIRequest parms, IEntityClient ec, CancellationToken ct);

    /// <summary>
    /// Imports data using the specified procedure and parameters.
    /// </summary>
    Task<CSVImportResult?> HandleImportData(ApplicationOption app, string entity_name, string? import_proc, DataWebAPIRequest parms, IEntityClient ec, CancellationToken ct);

    /// <summary>
    /// Retrieves the application's time zone offset.
    /// </summary>
    Task<int> HandleGetTimeZoneOffset(ApplicationOption app, IEntityClient ec, CancellationToken ct);

    /// <summary>
    /// Replaces a key/value pair for the specified application.
    /// </summary>
    void ReplaceApplicationKey(string app_id, string key, string value);

    /// <summary>
    /// Adds required application keys to the provided dictionary if missing.
    /// </summary>
    void EnsureApplicationKeys(string app_id, Dictionary<string, object> values);

    /// <summary>
    /// Gets the cached key/value pairs for the application.
    /// </summary>
    Dictionary<string, object> GetApplicationKeys(string app_id);
}
