namespace MicroM.Configuration;

public class ConfigurationDefaults
{
    public const string SQLConfigDatabaseName = "microm_configuration";
    public const string SQLConfigUser = "microm_config";
    public const string ControlPanelAppID = "micromcp";
    public const string CertificateSubjectName = "MicroMEncryptionCertificate";
    public const double EtagCacheDurationSeconds = 86400; // 1 day
    public const double JwksCacheDurationSeconds = 3600; // 1 hour
    public static string SecretsFilename { get; set; } = "microm_config.cry";
    public static string MicroMCommonID { get; set; } = "MicroM";
    public static string SecretsFilePath { get; set; } = Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData);
    public const string UploadsFolder = "uploads";

    public const string HTTPClientOidcName = "oidc";
    public const string HTTPClientOidcUserAgent = "MicroM.OIDC/1.0";
    public const string HTTPClientJwksName = "jwks";
    public const string HTTPClientJwksUserAgent = "MicroM.OIDC-JWKS/1.0";

    public static string[] AllowedFileUploadExtensions { get; set; } = [
        ".doc",
        ".docx",
        ".pdf",
        ".xls",
        ".xlsx",
        ".csv",
        ".jpg",
        ".jpeg",
        ".png",
        ".webp",
        ".zip"
    ];

    public static AppDBSchemaConfiguration SchemaConfiguration { get; set; } = new(APPSchema: "dbo", DDSchema: "dbo");
}
