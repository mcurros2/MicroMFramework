using MicroM.Configuration.CategoriesDefinitions;
using MicroM.Generators.ReactGenerator;
using MicroM.Web.Authentication.SSO;

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

    // Used only for IDPClient role
    public string? OIDCWellKnownURL { get; set; } = null;

    // Used at the IdP API tenant (wellknown and jwks endpoint) and
    // at the IDClient API tenant (for token signing) it should configured at the IdP when configuring authorized OIDC applications
    public string? OIDCCertificateUniqueID { get; set; } = null;
    public byte[]? OIDCCertificateBlob { get; set; } = null;
    public string OIDCCertificatePassword { get; set; } = "";

    // Used only for IDPServer role
    public string? OIDCIdPSubjectPepper { get; set; } = null;

    // Used by IdPServer and advertised in wellknown
    public OIDCSigningAlg? OIDCTokenSigningAlg { get; set; } = OIDCSigningAlg.RS512;
    public OIDCCodeChallengeMethod? OIDCTokenCodeChallengeMethod { get; set; } = OIDCCodeChallengeMethod.S256;

    // Allow advertising & accepting PKCE 'plain' (default false → only S256)
    public bool OIDCAllowPkcePlain { get; set; } = false;

    // Used only for IDPServer role
    public int OIDCRefreshTokenExpirationHours { get; set; } = 24 * 90;

    // Used only for IDPServer role, authorized OIDC clients
    public Dictionary<string, OIDCClientConfigurationOption>? OIDCClientConfiguration { get; set; } = null;

    public Dictionary<string, ADConfigurationOption>? ADConfiguration { get; set; } = null;

    public List<string> FrontendURLS { get; set; } = [];

    public AppDBSchemaConfiguration SchemaConfiguration { get; set; } = new("dbo", "dbo");

    public bool EnableDeveloperTools { get; set; } = false;

    public bool EnableSeedTestData { get; set; } = false;

    public bool EnableUpdateOnHotReload { get; set; } = false;

    // upload limit for file uploads in bytes, default 30MB
    public long UploadLimitBytes { get; set; } = DataDefaults.DefaultUploadFileSizeLimitBytes;
    public string? FileStorageType { get; set; } = nameof(FileStorageTypes.LocalFileStorage);

    // Developer tools configuration
    public string TypeScriptCategoriesFolder { get; set; } = "../Categories";
    public string TypeScriptDDCategoriesValuesClassName { get; set; } = "CategoriesValues";
    public string TypeScriptDDCategoriesValuesClassImport { get; set; } = TemplateValues.CONST_MICROM_LIB_PACKAGE;
    public string TypeScriptDDCategoryColumnName { get; set; } = "c_category_id";
    public string TypeScriptDDCategoryValueColumnName { get; set; } = "c_categoryvalue_id";
}
