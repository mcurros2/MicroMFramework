namespace MicroM.Web.Services;

public class SSOClientConfiguration
{
    public string ClientAppId { get; set; }
    public List<string> RedirectUris { get; set; }
    public List<string> PostLogoutRedirectUris { get; set; }
}