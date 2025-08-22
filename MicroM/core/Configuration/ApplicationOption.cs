using MicroM.DataDictionary.CategoriesDefinitions;

namespace MicroM.Configuration;

public class ApplicationOption
{

    public string ApplicationID { get; set; } = "";
    public string ApplicationName { get; set; } = "";

    public string SQLServer { get; set; } = "";
    public string SQLUser { get; set; } = "";
    public string SQLPassword { get; set; } = "";
    public string SQLDB { get; set; } = "";

    public string JWTIssuer { get; set; } = "";
    public string? JWTAudience { get; set; } = "";
    public string JWTKey { get; set; } = "";
    public int JWTTokenExpirationMinutes { get; set; } = 15;
    public int JWTRefreshExpirationHours { get; set; } = 1;
    public int AccountLockoutMinutes { get; set; } = 15;
    public int MaxBadLogonAttempts { get; set; } = 10;
    public int MaxRefreshTokenAttempts { get; set; } = 5;

    public string? AuthenticationType { get; set; } = nameof(AuthenticationTypes.MicroMAuthentication);

    public string? IdentityProviderRoleType { get; set; } = nameof(IdentityProviderRole.IDPDisabled);

    public List<string> IdentityProviderClients { get; set; } = [];

    public List<string> FrontendURLS { get; set; } = [];
}
