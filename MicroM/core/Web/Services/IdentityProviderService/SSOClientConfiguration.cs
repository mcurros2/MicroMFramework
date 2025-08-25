namespace MicroM.Web.Services;

/// <summary>
/// Configuration options for an SSO client application.
/// </summary>
public class SSOClientConfiguration
{
    /// <summary>
    /// Identifier of the client application.
    /// </summary>
    public string ClientAppId { get; set; }
    /// <summary>
    /// Allowed redirect URIs after successful authentication.
    /// </summary>
    public List<string> RedirectUris { get; set; }
    /// <summary>
    /// URIs to redirect to after the user logs out.
    /// </summary>
    public List<string> PostLogoutRedirectUris { get; set; }
}