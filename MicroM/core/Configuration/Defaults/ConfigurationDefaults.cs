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

    // Paths
    public static string MicroMConfigurationFolder { get; set; } = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData));

    public const string HTTPClientOidcName = "oidc";
    public const string HTTPClientOidcUserAgent = "MicroM.OIDC/1.0";
    public const string HTTPClientJwksName = "jwks";
    public const string HTTPClientJwksUserAgent = "MicroM.OIDC-JWKS/1.0";

    public static string[] AllowedFileUploadExtensions { get; set; } = [
        ".doc",
        ".docx",
        ".pdf",
        ".ppt",
        ".pptx",
        ".xls",
        ".xlsx",
        ".csv",
        ".jpg", ".jpeg", ".png", ".webp", ".gif", ".bmp", ".tiff", ".svg",
        ".txt",
        ".log", ".json", ".xml",
        ".mp3", ".wav", ".flac", ".aac", ".ogg",
        ".mp4", ".avi", ".mkv", ".mov", ".wmv", ".flv",
        ".rar", ".7z", ".zip", ".tar", ".gz", ".bz2", ".xz"
    ];

    public static AppDBSchemaConfiguration SchemaConfiguration { get; set; } = new(APPSchema: "dbo", DDSchema: "dbo");

}
