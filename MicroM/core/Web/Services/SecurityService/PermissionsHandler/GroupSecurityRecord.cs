using System.Collections.Immutable;

namespace MicroM.Web.Services.Security
{
    /// <summary>
    /// Holds the cached security information for a user group. Each record
    /// tracks the set of route paths the group may access along with the time
    /// those permissions were last refreshed.
    /// </summary>
    /// <param name="groupId">Identifier of the security group.</param>
    /// <param name="last_updated">Timestamp of the latest update to the group's permissions.</param>
    /// <param name="allowed_routes">Initial set of allowed route paths.</param>
    public class GroupSecurityRecord(string groupId, DateTime? last_updated, IEnumerable<string>? allowed_routes = null)
    {
        /// <summary>
        /// groupId; field.
        /// </summary>
        public readonly string GroupId = groupId;
        /// <summary>
        /// last_updated; field.
        /// </summary>
        public readonly DateTime? LastUpdated = last_updated;
        /// <summary>
        /// Collection of routes that the group is authorized to call.
        /// </summary>
        public ImmutableHashSet<string>? AllowedRoutes { get; internal set; } = allowed_routes?.ToImmutableHashSet(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// Replaces the current allowed routes with the provided set.
        /// </summary>
        /// <param name="routes">Route paths that should be allowed for the group.</param>
        public void AddAllowedRoutes(IEnumerable<string> routes)
        {
            AllowedRoutes = routes.ToImmutableHashSet(StringComparer.OrdinalIgnoreCase);
        }
    }
}
