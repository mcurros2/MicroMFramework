using MicroM.Configuration;
using MicroM.Data;
using MicroM.Web.Services.Security;
using System.Reflection;

namespace MicroM.Web.Services
{
    public interface IMicroMAppConfiguration
    {
        public ApplicationOption? GetAppConfiguration(string app_id);

        public Type? GetEntityType(string app_id, string entity_name);

        public List<Assembly> GetAllAPPAssemblies(string app_id);

        public Task<bool> RefreshConfiguration(string? app_id, CancellationToken ct);

        public DatabaseClient? GetDatabaseClient(string app_id, int? connection_tiemout_secs = 15);

        public List<string> GetAppIDs();

        public PublicEndpointSecurityRecord? GetPublicAccessAllowedRoutes(string app_id);

    }
}
