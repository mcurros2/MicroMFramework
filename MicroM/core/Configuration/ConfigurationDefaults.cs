namespace MicroM.Configuration
{
    /// <summary>
    /// Default configuration values used when no explicit settings are provided.
    /// </summary>
    public class ConfigurationDefaults
    {
        /// <summary>Name of the configuration database.</summary>
        public const string SQLConfigDatabaseName = "microm_configuration";
        /// <summary>Default user for the configuration database.</summary>
        public const string SQLConfigUser = "microm_config";
        /// <summary>Application identifier for the control panel.</summary>
        public const string ControlPanelAppID = "micromcp";
        /// <summary>Subject name used for the encryption certificate.</summary>
        public const string CertificateSubjectName = "MicroMEncryptionCertificate";
        /// <summary>Default filename for encrypted secrets.</summary>
        public static string SecretsFilename = "microm_config.cry";
        /// <summary>Common application identifier for MicroM.</summary>
        public static string MicroMCommonID = "MicroM";
        /// <summary>Path where the secrets file is stored.</summary>
        public static string SecretsFilePath = Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData);
        /// <summary>Folder name for uploaded files.</summary>
        public const string UploadsFolder = "uploads";
        /// <summary>Allowed file extensions for uploads.</summary>
        public static string[] AllowedFileUploadExtensions =
        {
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
        };
    }
}
