namespace MicroM.Web.Services;

/// <summary>
/// Represents the SSOClientConfiguration.
/// </summary>
public class SSOClientConfiguration
{
    /// <summary>
    /// Gets or sets the }.
    /// </summary>
    public string ClientAppId { get; set; }
    /// <summary>
    /// Gets or sets the }.
    /// </summary>
    public List<string> RedirectUris { get; set; }
    /// <summary>
    /// Gets or sets the }.
    /// </summary>
    public List<string> PostLogoutRedirectUris { get; set; }
}