using System.Collections.Immutable;

namespace MicroM.Web.Services.Security;

/// <summary>
/// Represents the PublicEndpointSecurityRecord.
/// </summary>
public class PublicEndpointSecurityRecord(string app_id, IEnumerable<string>? allowed_routes = null)
{
    /// <summary>
    /// app_id; field.
    /// </summary>
    public readonly string AppId = app_id;
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
