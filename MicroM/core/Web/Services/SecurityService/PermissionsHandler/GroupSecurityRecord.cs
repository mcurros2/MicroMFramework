using System.Collections.Immutable;

namespace MicroM.Web.Services.Security
{
    public class GroupSecurityRecord(string groupId, DateTime? last_updated, IEnumerable<string>? allowed_routes = null)
    {
        public readonly string GroupId = groupId;
        public readonly DateTime? LastUpdated = last_updated;
        public ImmutableHashSet<string>? AllowedRoutes { get; internal set; } = allowed_routes?.ToImmutableHashSet(StringComparer.OrdinalIgnoreCase);

        public void AddAllowedRoutes(IEnumerable<string> routes)
        {
            AllowedRoutes = routes.ToImmutableHashSet(StringComparer.OrdinalIgnoreCase);
        }
    }
}
