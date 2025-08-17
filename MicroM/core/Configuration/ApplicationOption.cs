using MicroM.DataDictionary.CategoriesDefinitions;

namespace MicroM.Configuration;

/// <summary>
/// Configuration settings for a MicroM application including database and authentication options.
/// </summary>
public class ApplicationOption
{
    /// <summary>Unique identifier for the application.</summary>
    public string ApplicationID { get; set; } = "";

    /// <summary>Display name for the application.</summary>
    public string ApplicationName { get; set; } = "";

    /// <summary>SQL Server connection information.</summary>
    public string SQLServer { get; set; } = "";
    /// <summary>User name for the SQL Server connection.</summary>
    public string SQLUser { get; set; } = "";
    /// <summary>Password for the SQL Server connection.</summary>
    public string SQLPassword { get; set; } = "";
    /// <summary>Database name to connect to.</summary>
    public string SQLDB { get; set; } = "";

    /// <summary>JWT issuer and audience settings.</summary>
    public string JWTIssuer { get; set; } = "";
    /// <summary>JWT token audience.</summary>
    public string? JWTAudience { get; set; } = "";
    /// <summary>Symmetric key used to sign JWT tokens.</summary>
    public string JWTKey { get; set; } = "";
    /// <summary>Access token expiration in minutes.</summary>
    public int JWTTokenExpirationMinutes { get; set; } = 15;
    /// <summary>Refresh token expiration in hours.</summary>
    public int JWTRefreshExpirationHours { get; set; } = 1;
    /// <summary>Lockout duration after repeated failed logins.</summary>
    public int AccountLockoutMinutes { get; set; } = 15;
    /// <summary>Maximum failed login attempts before lockout.</summary>
    public int MaxBadLogonAttempts { get; set; } = 10;
    /// <summary>Maximum refresh token attempts before requiring re-login.</summary>
    public int MaxRefreshTokenAttempts { get; set; } = 5;

    /// <summary>Authentication mechanism used by the application.</summary>
    public string? AuthenticationType { get; set; } = nameof(AuthenticationTypes.MicroMAuthentication);
    /// <summary>Role type expected from identity provider.</summary>
    public string? IdentityProviderRoleType { get; set; } = nameof(IdentityProviderRole.IDPDisabled);
    /// <summary>Valid identity provider client identifiers.</summary>
    public List<string> IdentityProviderClients { get; set; } = [];

    /// <summary>Allowed frontend URLs for cross-origin requests.</summary>
    public List<string> FrontendURLS { get; set; } = [];
}
