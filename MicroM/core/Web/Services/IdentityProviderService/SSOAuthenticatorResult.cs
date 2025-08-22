namespace MicroM.Web.Services;

/// <summary>
/// Represents the SSOAuthenticatorResult.
/// </summary>
public class SSOAuthenticatorResult
{
    /// <summary>
    /// Gets or sets the }.
    /// </summary>
    public bool Succeeded { get; set; }
    /// <summary>
    /// Gets or sets the }.
    /// </summary>
    public string? ErrorMessage { get; set; }
    /// <summary>
    /// Gets or sets the }.
    /// </summary>
    public Dictionary<string, object>? Claims { get; set; }
}