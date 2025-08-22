namespace MicroM.Web.Services;

/// <summary>
/// Represents the SSOTokenResult.
/// </summary>
public class SSOTokenResult
{
    /// <summary>
    /// Gets or sets the }.
    /// </summary>
    public string AccessToken { get; set; }
    /// <summary>
    /// Gets or sets the }.
    /// </summary>
    public string IdToken { get; set; }
    /// <summary>
    /// Gets or sets the }.
    /// </summary>
    public string RefreshToken { get; set; }
    /// <summary>
    /// Gets or sets the }.
    /// </summary>
    public int ExpiresIn { get; set; }
    /// <summary>
    /// Gets or sets the "Bearer";.
    /// </summary>
    public string TokenType { get; set; } = "Bearer";
}