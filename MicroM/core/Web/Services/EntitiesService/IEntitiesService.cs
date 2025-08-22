using MicroM.Configuration;
using MicroM.Core;
using MicroM.Data;
using MicroM.ImportData;

namespace MicroM.Web.Services;

public interface IEntitiesService
{
    /// <summary>
    /// Creates an Entity if exists in the configured assembly <see cref="LoadEntityTypes(Assembly)"/>.
    /// </summary>
    /// <param name="entity_name"></param>
    /// <param name="ec"></param>
    /// <returns></returns>
    public EntityBase? CreateEntity(ApplicationOption app, string entity_name, Dictionary<string, object>? server_claims, IEntityClient? ec = null);
    public EntityBase? CreateEntity(ApplicationOption app, string entity_name, Dictionary<string, object>? server_claims, CancellationToken ct);

    /// <summary>
    /// Connection factory for the webAPI.
    /// </summary>
    /// <returns></returns>
    public IEntityClient CreateDbConnection(ApplicationOption app, Dictionary<string, object>? server_claims);

    public Task<IEntityClient> CreateDbConnection(ApplicationOption app, Dictionary<string, object>? server_claims, CancellationToken ct);

    // TODO: dynamic entities

    public EntityDefinition? HandleGetEntityDefinition(ApplicationOption app, string entity_name);

    public Task<Dictionary<string, object?>?> HandleGetEntity(ApplicationOption app, string entity_name, DataWebAPIRequest parms, IEntityClient ec, CancellationToken ct);

    public Task<DBStatusResult?> HandleUpdateEntity(ApplicationOption app, string entity_name, DataWebAPIRequest parms, IEntityClient ec, CancellationToken ct);

    public Task<DBStatusResult?> HandleDeleteEntity(ApplicationOption app, string entity_name, DataWebAPIRequest parms, IEntityClient ec, CancellationToken ct);

    public Task<DBStatusResult?> HandleInsertEntity(ApplicationOption app, string entity_name, DataWebAPIRequest parms, IEntityClient ec, CancellationToken ct);

    public Task<LookupResult> HandleLookupEntity(ApplicationOption app, string entity_name, DataWebAPIRequest parms, IEntityClient ec, CancellationToken ct, string? lookup_name = null);

    public Task<List<DataResult>?> HandleExecuteView(ApplicationOption app, string entity_name, string view_name, DataWebAPIRequest parms, IEntityClient ec, CancellationToken ct);

    public Task<List<DataResult>?> HandleExecuteProc(ApplicationOption app, string entity_name, string proc_name, DataWebAPIRequest parms, IEntityClient ec, CancellationToken ct);

    public Task<DBStatusResult?> HandleExecuteProcDBStatus(ApplicationOption app, string entity_name, string proc_name, DataWebAPIRequest parms, IEntityClient ec, CancellationToken ct);

    public Task<EntityActionResult?> HandleExecuteAction(ApplicationOption app, string entity_name, string entity_action, DataWebAPIRequest parms, IEntityClient ec, CancellationToken ct);

    public Task<CSVImportResult?> HandleImportData(ApplicationOption app, string entity_name, string? import_proc, DataWebAPIRequest parms, IEntityClient ec, CancellationToken ct);

    public Task<int> HandleGetTimeZoneOffset(ApplicationOption app, IEntityClient ec, CancellationToken ct);

    public void ReplaceApplicationKey(string app_id, string key, string value);

    public void EnsureApplicationKeys(string app_id, Dictionary<string, object> values);

    public Dictionary<string, object> GetApplicationKeys(string app_id);
}
