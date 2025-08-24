namespace MicroM.Web.Services.Security
{
    /// <summary>
    /// Marks controllers or actions that are publicly accessible. Endpoints
    /// decorated with this attribute bypass normal authentication but are
    /// still validated by <see cref="PublicEndpointsMiddleware"/> against the
    /// configured list of allowed public routes.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Class)]
    public class PublicEndpointAttribute : Attribute
    {
        // Marker attribute – no members required.
    }
}

