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
    public record GetAllGroupsAllowedRoutesResult
    {
        public string c_user_group_id = "";
        public string vc_route_path = "";
        public string c_route_id = "";
        public DateTime? dt_last_route_updated;
    }

    /// <summary>
    /// The security service is responsible for checking if a user is authorized to access a route.
    /// It will cache groups defined in <see cref="MicromUsersGroups"/> and their allowed menu items in <see cref="MicromMenus"/> and <see cref="MicromMenusItems"/>"
    /// Allowed routes are defined by <see cref="MicroM.DataDictionary.Configuration.MenuDefinition"/>
    /// </summary>
    /// <param name="app_config"></param>
    /// <param name="logger"></param>
    /// <param name="options"></param>
    public class SecurityService(IMicroMAppConfiguration app_config, ILogger<SecurityService> logger, IOptions<MicroMOptions> options) : ISecurityService, IHostedService
    {

        private ConcurrentDictionary<string, GroupSecurityRecord> _groupsSecurityRecords = new(StringComparer.OrdinalIgnoreCase);

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

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            foreach (var app_id in app_config.GetAppIDs())
            {
                await RefreshGroupsSecurityRecords(app_id, cancellationToken);
            }
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }
}
