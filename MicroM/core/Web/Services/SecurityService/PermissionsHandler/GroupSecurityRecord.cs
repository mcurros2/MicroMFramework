using System.Collections.Immutable;

namespace MicroM.Web.Services.Security
{
    /// <summary>
    /// Represents the GroupSecurityRecord.
    /// </summary>
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
        /// Performs the allowed_routes?.ToImmutableHashSet operation.
        /// </summary>
        public ImmutableHashSet<string>? AllowedRoutes { get; internal set; } = allowed_routes?.ToImmutableHashSet(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// Performs the AddAllowedRoutes operation.
        /// </summary>
        public void AddAllowedRoutes(IEnumerable<string> routes)
        {
            AllowedRoutes = routes.ToImmutableHashSet(StringComparer.OrdinalIgnoreCase);
        }
    }
}
