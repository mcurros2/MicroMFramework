using System.Collections.Immutable;

namespace MicroM.Web.Services.Security;

public class PublicEndpointSecurityRecord(string app_id, IEnumerable<string>? allowed_routes = null)
{
    public readonly string AppId = app_id;
    public ImmutableHashSet<string>? AllowedRoutes { get; internal set; } = allowed_routes?.ToImmutableHashSet(StringComparer.OrdinalIgnoreCase);

    public void AddAllowedRoutes(IEnumerable<string> routes)
    {
        AllowedRoutes = routes.ToImmutableHashSet(StringComparer.OrdinalIgnoreCase);
    }
}
