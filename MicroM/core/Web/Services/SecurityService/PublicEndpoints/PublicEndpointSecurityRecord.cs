using System.Collections.Immutable;

namespace MicroM.Web.Services.Security;

/// <summary>
/// Stores the set of publicly accessible routes for a given application.
/// These records are used to validate requests hitting endpoints marked with
/// <see cref="PublicEndpointAttribute"/>.
/// </summary>
/// <param name="app_id">Application identifier.</param>
/// <param name="allowed_routes">Initial collection of allowed routes.</param>
public class PublicEndpointSecurityRecord(string app_id, IEnumerable<string>? allowed_routes = null)
{
    /// <summary>Identifier of the application.</summary>
    public readonly string AppId = app_id;
    /// <summary>Routes that can be accessed without authentication.</summary>
    public ImmutableHashSet<string>? AllowedRoutes { get; internal set; } = allowed_routes?.ToImmutableHashSet(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Replaces the current set of allowed routes.
    /// </summary>
    /// <param name="routes">Routes to allow publicly.</param>
    public void AddAllowedRoutes(IEnumerable<string> routes)
    {
        AllowedRoutes = routes.ToImmutableHashSet(StringComparer.OrdinalIgnoreCase);
    }
}
