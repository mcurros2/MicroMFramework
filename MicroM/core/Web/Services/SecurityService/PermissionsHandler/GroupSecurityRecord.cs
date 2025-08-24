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
        /// Unique identifier for the group whose permissions are represented by this record.
        /// </summary>
        public readonly string GroupId = groupId;
        /// <summary>
        /// Timestamp indicating when the group's permissions were last refreshed.
        /// </summary>
        public readonly DateTime? LastUpdated = last_updated;
        /// <summary>
        /// Set of route paths the group can access, stored case-insensitively for fast lookups.
        /// </summary>
        public ImmutableHashSet<string>? AllowedRoutes { get; internal set; } = allowed_routes?.ToImmutableHashSet(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// Replaces the current allowed routes with the provided set.
        /// </summary>
        /// <param name="routes">Route paths that should be allowed for the group.</param>
        /// <returns>
        /// <see langword="void"/>. The internal <see cref="AllowedRoutes"/> collection is updated in place.
        /// </returns>
        public void AddAllowedRoutes(IEnumerable<string> routes)
        {
            AllowedRoutes = routes.ToImmutableHashSet(StringComparer.OrdinalIgnoreCase);
        }
    }
}
