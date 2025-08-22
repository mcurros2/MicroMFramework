using MicroM.Configuration;
using MicroM.Data;
using MicroM.Web.Services.Security;
using System.Reflection;

namespace MicroM.Web.Services
{
    /// <summary>
    /// Represents the IMicroMAppConfiguration.
    /// </summary>
    public interface IMicroMAppConfiguration
    {
        /// <summary>
        /// Performs the GetAppConfiguration operation.
        /// </summary>
        public ApplicationOption? GetAppConfiguration(string app_id);

        /// <summary>
        /// Performs the GetEntityType operation.
        /// </summary>
        public Type? GetEntityType(string app_id, string entity_name);

        /// <summary>
        /// Performs the GetAllAPPAssemblies operation.
        /// </summary>
        public List<Assembly> GetAllAPPAssemblies(string app_id);

        /// <summary>
        /// Performs the RefreshConfiguration operation.
        /// </summary>
        public Task<bool> RefreshConfiguration(string? app_id, CancellationToken ct);

        /// <summary>
        /// Performs the GetDatabaseClient operation.
        /// </summary>
        public DatabaseClient? GetDatabaseClient(string app_id, int? connection_tiemout_secs = 15);

        /// <summary>
        /// Performs the GetAppIDs operation.
        /// </summary>
        public List<string> GetAppIDs();

        /// <summary>
        /// Performs the GetPublicAccessAllowedRoutes operation.
        /// </summary>
        public PublicEndpointSecurityRecord? GetPublicAccessAllowedRoutes(string app_id);

        /// <summary>
        /// Performs the IsCORSOriginAllowed operation.
        /// </summary>
        public bool IsCORSOriginAllowed(string? app_id, string origin);

    }
}
