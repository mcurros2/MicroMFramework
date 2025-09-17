using MicroM.DataDictionary.CategoriesDefinitions;
using MicroM.Web.Authentication.SSO;

namespace MicroM.Configuration;

public enum ApplicationOptionCacheKeys
{
    OIDCWellKnown,
    OIDCJwks
}

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

    internal string? IdentityProviderRoleType { get; set; } = nameof(IdentityProviderRole.IDPDisabled);
    internal string? OIDCWellKnownURL { get; set; } = null;
    internal string? OIDCCertificateUniqueID { get; set; } = null;
    internal byte[]? OIDCCertificateBlob { get; set; } = null;
    internal string OIDCCertificatePassword { get; set; } = "";

    internal OIDCSigningAlg? OIDCTokenSigningAlg { get; set; } = OIDCSigningAlg.RS512;
    internal OIDCCodeChallengeMethod? OIDCTokenCodeChallengeMethod { get; set; } = OIDCCodeChallengeMethod.S256;

    internal Dictionary<string, OIDCClientConfigurationOption>? OIDCClientConfiguration { get; set; } = null;


    public List<string> FrontendURLS { get; set; } = [];
}
