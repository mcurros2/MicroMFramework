using MicroM.Configuration;
using MicroM.Core;
using MicroM.Data;
using MicroM.ImportData;
using MicroM.Web.Authentication;

namespace MicroM.Web.Services;

public interface IEntitiesService
{
    /// <summary>
    /// Creates an Entity if exists in the configured assembly <see cref="LoadEntityTypes(Assembly)"/>.
    /// </summary>
    /// <param name="entity_name"></param>
    /// <param name="ec"></param>
    /// <returns></returns>
    public EntityBase? CreateEntity(ApplicationOption app, string entity_name, Dictionary<string, object>? server_claims, IAuthenticationProvider auth, IEntityClient? ec = null);
    public EntityBase? CreateEntity(string app_id, string entity_name, Dictionary<string, object>? server_claims, IAuthenticationProvider auth, CancellationToken ct);

    /// <summary>
    /// Connection factory for the webAPI.
    /// </summary>
    /// <returns></returns>
    public IEntityClient CreateDbConnection(ApplicationOption app, Dictionary<string, object>? server_claims, IAuthenticationProvider auth);

    public Task<IEntityClient> CreateDbConnection(string app_id, Dictionary<string, object>? server_claims, IAuthenticationProvider auth, CancellationToken ct);

    // TODO: dynamic entities

    public EntityDefinition? HandleGetEntityDefinition(string app_id, string entity_name);

    public Task<Dictionary<string, object?>?> HandleGetEntity(IAuthenticationProvider auth, string app_id, string entity_name, DataWebAPIRequest parms, IEntityClient ec, CancellationToken ct);

    public Task<DBStatusResult?> HandleUpdateEntity(IAuthenticationProvider auth, string app_id, string entity_name, DataWebAPIRequest parms, IEntityClient ec, CancellationToken ct);

    public Task<DBStatusResult?> HandleDeleteEntity(IAuthenticationProvider auth, string app_id, string entity_name, DataWebAPIRequest parms, IEntityClient ec, CancellationToken ct);

    public Task<DBStatusResult?> HandleInsertEntity(IAuthenticationProvider auth, string app_id, string entity_name, DataWebAPIRequest parms, IEntityClient ec, CancellationToken ct);

    public Task<LookupResult> HandleLookupEntity(IAuthenticationProvider auth, string app_id, string entity_name, DataWebAPIRequest parms, IEntityClient ec, CancellationToken ct, string? lookup_name = null);

    public Task<List<DataResult>?> HandleExecuteView(IAuthenticationProvider auth, string app_id, string entity_name, string view_name, DataWebAPIRequest parms, IEntityClient ec, CancellationToken ct);

    public Task<List<DataResult>?> HandleExecuteProc(IAuthenticationProvider auth, string app_id, string entity_name, string proc_name, DataWebAPIRequest parms, IEntityClient ec, CancellationToken ct);

    public Task<DBStatusResult?> HandleExecuteProcDBStatus(IAuthenticationProvider auth, string app_id, string entity_name, string proc_name, DataWebAPIRequest parms, IEntityClient ec, CancellationToken ct);

    public Task<EntityActionResult?> HandleExecuteAction(IAuthenticationProvider auth, string app_id, string entity_name, string entity_action, DataWebAPIRequest parms, IEntityClient ec, CancellationToken ct);

    public Task<CSVImportResult?> HandleImportData(IAuthenticationProvider auth, string app_id, string entity_name, string? import_proc, DataWebAPIRequest parms, IEntityClient ec, CancellationToken ct);

    public Task<int> HandleGetTimeZoneOffset(IAuthenticationProvider auth, string app_id, IEntityClient ec, CancellationToken ct);

    public void ReplaceApplicationKey(string app_id, string key, string value);

    public void EnsureApplicationKeys(string app_id, Dictionary<string, object> values);

    public Dictionary<string, object> GetApplicationKeys(string app_id);
}
