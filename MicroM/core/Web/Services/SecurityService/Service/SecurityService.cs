using MicroM.Configuration;
using MicroM.DataDictionary;
using MicroM.DataDictionary.CategoriesDefinitions;
using MicroM.Extensions;
using MicroM.Web.Authentication;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Collections.Concurrent;

namespace MicroM.Web.Services.Security
{
    /// <summary>
    /// Data transfer object returned when loading allowed routes for groups.
    /// </summary>
    public record GetAllGroupsAllowedRoutesResult
    {
        /// <summary>Identifier of the user group.</summary>
        public string c_user_group_id = "";
        /// <summary>Route path that the group can access.</summary>
        public string vc_route_path = "";
        /// <summary>Unique identifier of the route.</summary>
        public string c_route_id = "";
        /// <summary>Date when the route was last updated.</summary>
        public DateTime? dt_last_route_updated;
    }

    /// <summary>
    /// Provides authorization checks and caches group route permissions.
    /// </summary>
    /// <param name="app_config">Application configuration provider.</param>
    /// <param name="logger">Logger used to record diagnostic information.</param>
    /// <param name="options">Runtime options that influence security behavior.</param>
    public class SecurityService(IMicroMAppConfiguration app_config, ILogger<SecurityService> logger, IOptions<MicroMOptions> options) : ISecurityService, IHostedService
    {

        private ConcurrentDictionary<string, GroupSecurityRecord> _groupsSecurityRecords = new(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// Retrieves all group security records for the specified application
        /// from the database and maps them into <see cref="GroupSecurityRecord"/> instances.
        /// </summary>
        /// <param name="app_id">Application identifier.</param>
        /// <param name="ct">Cancellation token.</param>
        /// <returns>A dictionary keyed by group ID with their security records.</returns>
        private async Task<Dictionary<string, GroupSecurityRecord>> GetAllGroupsSecurityRecords(string app_id, CancellationToken ct)
        {
            var groupsSecurityRecords = new Dictionary<string, GroupSecurityRecord>(StringComparer.OrdinalIgnoreCase);
            try
            {
                using var dbc = app_config.GetDatabaseClient(app_id);
                if (dbc == null)
                {
                    return [];
                }

                var groups = new MicromUsersGroups(dbc);

                var result = await groups.ExecuteProc<GetAllGroupsAllowedRoutesResult>(ct, groups.Def.mug_GetAllGroupsAllowedRoutes);


                if (result != null)
                {
                    // group
                    var groupedResults = result.GroupBy(
                        r => r.c_user_group_id,
                        StringComparer.OrdinalIgnoreCase
                    );

                    foreach (var group in groupedResults)
                    {
                        // Get routes
                        var allowedRoutes = group
                            .Select(r => $"/{options.Value.MicroMAPIBaseRootPath}/{app_id}/ent/{r.vc_route_path}");


                        // last update
                        var latestUpdatedRoute = group
                            .Max(r => r.dt_last_route_updated);

                        var record = new GroupSecurityRecord(
                            group.Key,
                            latestUpdatedRoute,
                            allowedRoutes
                        );

                        groupsSecurityRecords.TryAdd($"{app_id}.{group.Key}", record);
                    }

                }

                logger.LogInformation("Groups security records for {app_id} loaded successfully", app_id);
            }
            catch (Exception ex)
            {
                logger.LogError("Error getting groups security records for {app_id}\nException: {ex}", app_id, ex.ToString());
            }
            return groupsSecurityRecords;
        }

        /// <summary>
        /// Refreshes the cached security information for the specified application.
        /// </summary>
        /// <param name="app_id">Application identifier whose group records should be refreshed.</param>
        /// <param name="ct">Cancellation token.</param>
        /// <returns>A task representing the asynchronous refresh operation.</returns>
        public async Task RefreshGroupsSecurityRecords(string? app_id, CancellationToken ct)
        {
            if (string.IsNullOrEmpty(app_id))
            {
                return;
            }
            var new_records = await GetAllGroupsSecurityRecords(app_id, ct);

            var original_dict = _groupsSecurityRecords;
            var updated_dict = new ConcurrentDictionary<string, GroupSecurityRecord>(original_dict, StringComparer.OrdinalIgnoreCase);

            // app key prefix
            var prefix = $"{app_id}.";

            // Get existing records to remove
            var keys_to_remove = updated_dict.Keys
                .Where(k => k.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                .ToList();

            if (keys_to_remove != null && keys_to_remove.Count > 0)
            {
                foreach (var key in keys_to_remove)
                {
                    updated_dict.TryRemove(key, out _);
                }
            }

            // Add new
            foreach (var kvp in new_records)
            {
                updated_dict.TryAdd(kvp.Key, kvp.Value);
            }

            Interlocked.Exchange(ref _groupsSecurityRecords, updated_dict);
        }

        /// <summary>
        /// Determines whether the supplied claims authorize access to the
        /// specified route within the given application. The method first
        /// checks if the route is globally allowed via
        /// <see cref="EveryoneAllowedRoutes"/> configuration and then verifies
        /// membership-based permissions loaded by <see cref="RefreshGroupsSecurityRecords"/>.
        /// </summary>
        /// <param name="app_id">Application identifier.</param>
        /// <param name="route_path">Route path being accessed.</param>
        /// <param name="server_claims">Claims describing the current user.</param>
        /// <returns>
        /// <see langword="true"/> when the route is authorized; otherwise
        /// <see langword="false"/>.
        /// </returns>
        public bool IsAuthorized(string app_id, string route_path, Dictionary<string, object?> server_claims)
        {
            var user_type = server_claims.TryGetValue(MicroMServerClaimTypes.MicroMUserType_id, out var userTypeObj) && userTypeObj is string userType ? userType : "";
            if (user_type == nameof(UserTypes.ADMIN)) return true;

            if (EveryoneAllowedRoutes.IsEveryoneAllowedRoute(options.Value.MicroMAPIBaseRootPath ?? "", app_id, route_path)) return true;

            var user_groups = server_claims.TryGetValue(MicroMServerClaimTypes.MicroMUserGroups, out var userGroupsObj) && userGroupsObj is string userGroups ? userGroups : "[]";

            var user_groups_list = user_groups.FromJsonStringArray();

            if (user_groups_list == null || user_groups_list.Length == 0)
            {
                return false;
            }

            foreach (var group in user_groups_list)
            {
                if (_groupsSecurityRecords.TryGetValue($"{app_id}.{group}", out var record))
                {
                    if (record?.AllowedRoutes?.Contains(route_path) ?? false)
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        /// <summary>
        /// Loads the security records for all configured applications when the
        /// service starts.
        /// </summary>
        /// <param name="cancellationToken">Token used to cancel the startup operation.</param>
        /// <returns>A task that completes when initialization is finished.</returns>
        public async Task StartAsync(CancellationToken cancellationToken)
        {
            foreach (var app_id in app_config.GetAppIDs())
            {
                await RefreshGroupsSecurityRecords(app_id, cancellationToken);
            }
        }

        /// <summary>
        /// Stops the service. No cleanup is required so a completed task is returned.
        /// </summary>
        /// <param name="cancellationToken">Token used to cancel the shutdown operation.</param>
        /// <returns>A completed task.</returns>
        public Task StopAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }
}
