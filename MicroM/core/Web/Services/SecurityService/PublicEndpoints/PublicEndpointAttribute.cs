namespace MicroM.Web.Services.Security
{
    /// <summary>
    /// Represents the PublicEndpointAttribute.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Class)]
    /// <summary>
    /// Represents the PublicEndpointAttribute.
    /// </summary>
    public class PublicEndpointAttribute : Attribute
    {
        // for marking public routes and triggering public routes middleware
    }
}
