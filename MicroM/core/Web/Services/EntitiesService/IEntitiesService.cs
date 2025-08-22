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
    /// Performs the CreateEntity operation.
    /// </summary>
    public EntityBase? CreateEntity(ApplicationOption app, string entity_name, Dictionary<string, object>? server_claims, CancellationToken ct);

    /// <summary>
    /// Connection factory for the webAPI.
    /// </summary>
    /// <returns></returns>
    public IEntityClient CreateDbConnection(ApplicationOption app, Dictionary<string, object>? server_claims);

    /// <summary>
    /// Performs the CreateDbConnection operation.
    /// </summary>
    public Task<IEntityClient> CreateDbConnection(ApplicationOption app, Dictionary<string, object>? server_claims, CancellationToken ct);

    // TODO: dynamic entities

    /// <summary>
    /// Performs the HandleGetEntityDefinition operation.
    /// </summary>
    public EntityDefinition? HandleGetEntityDefinition(ApplicationOption app, string entity_name);

    /// <summary>
    /// Performs the HandleGetEntity operation.
    /// </summary>
    public Task<Dictionary<string, object?>?> HandleGetEntity(ApplicationOption app, string entity_name, DataWebAPIRequest parms, IEntityClient ec, CancellationToken ct);

    /// <summary>
    /// Performs the HandleUpdateEntity operation.
    /// </summary>
    public Task<DBStatusResult?> HandleUpdateEntity(ApplicationOption app, string entity_name, DataWebAPIRequest parms, IEntityClient ec, CancellationToken ct);

    /// <summary>
    /// Performs the HandleDeleteEntity operation.
    /// </summary>
    public Task<DBStatusResult?> HandleDeleteEntity(ApplicationOption app, string entity_name, DataWebAPIRequest parms, IEntityClient ec, CancellationToken ct);

    /// <summary>
    /// Performs the HandleInsertEntity operation.
    /// </summary>
    public Task<DBStatusResult?> HandleInsertEntity(ApplicationOption app, string entity_name, DataWebAPIRequest parms, IEntityClient ec, CancellationToken ct);

    /// <summary>
    /// Performs the HandleLookupEntity operation.
    /// </summary>
    public Task<LookupResult> HandleLookupEntity(ApplicationOption app, string entity_name, DataWebAPIRequest parms, IEntityClient ec, CancellationToken ct, string? lookup_name = null);

    /// <summary>
    /// Performs the HandleExecuteView operation.
    /// </summary>
    public Task<List<DataResult>?> HandleExecuteView(ApplicationOption app, string entity_name, string view_name, DataWebAPIRequest parms, IEntityClient ec, CancellationToken ct);

    /// <summary>
    /// Performs the HandleExecuteProc operation.
    /// </summary>
    public Task<List<DataResult>?> HandleExecuteProc(ApplicationOption app, string entity_name, string proc_name, DataWebAPIRequest parms, IEntityClient ec, CancellationToken ct);

    /// <summary>
    /// Performs the HandleExecuteProcDBStatus operation.
    /// </summary>
    public Task<DBStatusResult?> HandleExecuteProcDBStatus(ApplicationOption app, string entity_name, string proc_name, DataWebAPIRequest parms, IEntityClient ec, CancellationToken ct);

    /// <summary>
    /// Performs the HandleExecuteAction operation.
    /// </summary>
    public Task<EntityActionResult?> HandleExecuteAction(ApplicationOption app, string entity_name, string entity_action, DataWebAPIRequest parms, IEntityClient ec, CancellationToken ct);

    /// <summary>
    /// Performs the HandleImportData operation.
    /// </summary>
    public Task<CSVImportResult?> HandleImportData(ApplicationOption app, string entity_name, string? import_proc, DataWebAPIRequest parms, IEntityClient ec, CancellationToken ct);

    /// <summary>
    /// Performs the HandleGetTimeZoneOffset operation.
    /// </summary>
    public Task<int> HandleGetTimeZoneOffset(ApplicationOption app, IEntityClient ec, CancellationToken ct);

    /// <summary>
    /// Performs the ReplaceApplicationKey operation.
    /// </summary>
    public void ReplaceApplicationKey(string app_id, string key, string value);

    /// <summary>
    /// Performs the EnsureApplicationKeys operation.
    /// </summary>
    public void EnsureApplicationKeys(string app_id, Dictionary<string, object> values);

    /// <summary>
    /// Performs the GetApplicationKeys operation.
    /// </summary>
    public Dictionary<string, object> GetApplicationKeys(string app_id);
}
