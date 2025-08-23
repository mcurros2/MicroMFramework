namespace MicroM.Web.Services;

/// <summary>
/// Result of an SSO authentication attempt.
/// </summary>
public class SSOAuthenticatorResult
{
    /// <summary>
    /// Indicates whether authentication succeeded.
    /// </summary>
    public bool Succeeded { get; set; }
    /// <summary>
    /// Error message when authentication fails.
    /// </summary>
    public string? ErrorMessage { get; set; }
    /// <summary>
    /// Claims returned by the identity provider when authentication succeeds.
    /// </summary>
    public Dictionary<string, object>? Claims { get; set; }
}