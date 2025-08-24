namespace MicroM.Web.Services;

/// <summary>
/// Tokens and expiration information returned from an SSO authorization flow.
/// </summary>
public class SSOTokenResult
{
    /// <summary>
    /// Access token issued to the client.
    /// </summary>
    public string AccessToken { get; set; }
    /// <summary>
    /// ID token containing user identity claims.
    /// </summary>
    public string IdToken { get; set; }
    /// <summary>
    /// Refresh token used to obtain new access tokens.
    /// </summary>
    public string RefreshToken { get; set; }
    /// <summary>
    /// Number of seconds until the access token expires.
    /// </summary>
    public int ExpiresIn { get; set; }
    /// <summary>
    /// Type of token issued, typically "Bearer".
    /// </summary>
    public string TokenType { get; set; } = "Bearer";
}