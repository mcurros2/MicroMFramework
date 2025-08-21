namespace MicroM.DataDictionary.Entities.MicromUsers;

/// <summary>
/// Represents authentication and status information retrieved for a user.
/// </summary>
public record LoginData
{
    /// <summary>
    /// Unique identifier of the user.
    /// </summary>
    public string user_id { get; set; } = "";

    /// <summary>
    /// Indicates whether the account is locked.
    /// </summary>
    public bool locked { get; set; } = true;

    /// <summary>
    /// Hash of the user's password.
    /// </summary>
    public string pwhash { get; set; } = "";

    /// <summary>
    /// Number of failed login attempts.
    /// </summary>
    public int badlogonattempts { get; set; } = 0;

    /// <summary>
    /// Minutes remaining before the lockout expires.
    /// </summary>
    public int locked_minutes_remaining { get; set; } = 0;

    /// <summary>
    /// Email address associated with the account.
    /// </summary>
    public string? email { get; set; }

    /// <summary>
    /// Username for the account.
    /// </summary>
    public string username { get; set; } = "";

    /// <summary>
    /// Indicates whether the account is disabled.
    /// </summary>
    public bool disabled { get; set; } = true;

    /// <summary>
    /// Refresh token issued to the user.
    /// </summary>
    public string? refresh_token { get; set; }

    /// <summary>
    /// Indicates if the refresh token has expired.
    /// </summary>
    public bool refresh_expired { get; set; } = true;

    /// <summary>
    /// Identifier of the user type.
    /// </summary>
    public string? usertype_id { get; set; }

    /// <summary>
    /// Name of the user type.
    /// </summary>
    public string? usertype_name { get; set; }

    /// <summary>
    /// JSON array string of groups the user belongs to.
    /// </summary>
    public string? user_groups { get; set; }
}

