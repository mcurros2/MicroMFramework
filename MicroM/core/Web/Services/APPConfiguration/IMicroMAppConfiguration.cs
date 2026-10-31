using MicroM.Configuration;
using MicroM.Data;
using MicroM.Web.Services.Security;
using Microsoft.AspNetCore.Http;
using System.Reflection;

namespace MicroM.Web.Services;

public sealed record EntityTypeRecord
{
    public required string EntityName { get; set; }
    public required string Type { get; set; }
}

public interface IMicroMAppConfiguration
{
    public ApplicationOption? GetAppConfiguration(string app_id);

    public Type? GetEntityType(string app_id, string entity_name);

    public List<Assembly> GetAllAPPAssemblies(string app_id);

    public Task<bool> RefreshConfiguration(string? app_id, CancellationToken ct);

    /// <summary>Refreshes configuration, optionally skipping only test-data seeding during automatic database updates.</summary>
    public Task<bool> RefreshConfiguration(string? app_id, CancellationToken ct, bool skipSeedTestData);

    public Task<bool> RefreshConfiguration(string? app_id, ConfigurationRefreshOptions options, CancellationToken ct);

    /// <summary>Schedules eligible seed work after initialization; startup work waits for ApplicationStarted.</summary>
    public void QueueSeedTestData(string app_id, IReadOnlyList<IDatabaseSchema> initializers);

    /// <summary>Queues the combined entity catalog after application database initialization.</summary>
    public void QueuePersistAppEntityTypes(string appId);

    public DatabaseClient? GetDatabaseClient(string app_id, int connection_tiemout_secs = 5);

    public List<string> GetAppIDs();

    public PublicEndpointSecurityRecord? GetPublicAccessAllowedRoutes(string app_id);

    public bool IsCORSOriginAllowed(string? app_id, string origin);

    public string? GetTenantPath(HttpContext context);

    public bool IsDDType(string type_name);

    public Task StartApplicationServices();

    public Task StopApplicationServices();

}
