namespace MicroM.Configuration
{
    /// <summary>
    /// Options for configuring core MicroM behaviors and defaults.
    /// </summary>
    public class MicroMOptions
    {
        /// <summary>
        /// Configuration section name used in application settings.
        /// </summary>
        public const string MicroM = nameof(MicroM);

        /// <summary>Default SQL connection timeout in seconds.</summary>
        public int DefaultConnectionTimeOutInSecs { get; set; } = -1;

        /// <summary>Default command timeout in minutes.</summary>
        public int DefaultCommandTimeOutInMins { get; set; } = -1;

        /// <summary>Default row limit for views.</summary>
        public int DefaultRowLimitForViews { get; set; } = -1;

        /// <summary>Configuration database server name.</summary>
        public string? ConfigSQLServer { get; set; } = null;
        /// <summary>Name of the configuration database.</summary>
        public string? ConfigSQLServerDB { get; set; } = null;
        /// <summary>Thumbprint of the certificate used for encryption.</summary>
        public string? CertificateThumbprint { get; set; } = null;

        /// <summary>Folder used for uploaded files.</summary>
        public string? UploadsFolder { get; set; } = null;

        /// <summary>Allowed file extensions for uploads.</summary>
        public string[]? AllowedUploadFileExtensions { get; set; } = null;

        /// <summary>Base path for the MicroM API.</summary>
        public string? MicroMAPIBaseRootPath { get; set; } = "microm";
        /// <summary>Cookie root path for the MicroM API.</summary>
        public string? MicroMAPICookieRootPath { get; set; } = "microm";

        /// <summary>Default SQL collation for new databases.</summary>
        public string? DefaultSQLDatabaseCollation { get; set; } = null;
    }
}
