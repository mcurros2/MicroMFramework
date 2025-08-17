namespace MicroM.Configuration
{
    /// <summary>
    /// Defines routes that should be accessible without authentication.
    /// </summary>
    public interface IPublicEndpoints
    {
        /// <summary>
        /// Returns additional route patterns that are publicly accessible.
        /// </summary>
        List<string>? AddAllowedPublicEndpointRoutes();
    }
}
