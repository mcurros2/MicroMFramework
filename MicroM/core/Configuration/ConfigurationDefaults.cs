namespace MicroM.Configuration
{
    public class ConfigurationDefaults
    {
        public const string SQLConfigDatabaseName = "microm_configuration";
        public const string SQLConfigUser = "microm_config";
        public const string ControlPanelAppID = "micromcp";
        public const string CertificateSubjectName = "MicroMEncryptionCertificate";
        public static string SecretsFilename = "microm_config.cry";
        public static string MicroMCommonID = "MicroM";
        public static string SecretsFilePath = Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData);
        public const string UploadsFolder = "uploads";
        public static string[] AllowedFileUploadExtensions = [
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
    }
}
