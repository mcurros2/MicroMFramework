namespace MicroM.Web.Services.Security
{
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Class)]
    public class PublicEndpointAttribute : Attribute
    {
        // for marking public routes and triggering public routes middleware
    }
}
