namespace MicroM.DataDictionary.Entities.MicromUsers;

/// <summary>
/// Result of a refresh token request.
/// </summary>
public record RefreshTokenResult
{
    /// <summary>
    /// Outcome of the refresh attempt.
    /// </summary>
    public LoginAttemptStatus Status { get; set; } = LoginAttemptStatus.Unknown;

    /// <summary>
    /// Optional message describing the result.
    /// </summary>
    public string? Message { get; set; } = null;

    /// <summary>
    /// Newly issued refresh token.
    /// </summary>
    public string? RefreshToken { get; set; } = null;

    /// <summary>
    /// Expiration time for the refresh token.
    /// </summary>
    public DateTime? RefreshExpiration { get; set; } = null;
}

