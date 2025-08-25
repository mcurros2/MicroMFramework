using MicroM.Core;

namespace MicroM.DataDictionary
{
    /// <summary>
    /// Represents the result of the initial configuration validation process.
    /// </summary>
    public record InitialConfigurationResult : EntityActionResult
    {
        /// <summary>
        /// Gets or sets a value indicating whether the configuration database values are valid.
        /// </summary>
        public bool ConfigDBValid { get; set; } = false;
        /// <summary>
        /// Gets or sets a value indicating whether the configuration username is valid.
        /// </summary>
        public bool ConfigUserameValid { get; set; } = false;
        /// <summary>
        /// Gets or sets a value indicating whether the configuration password is valid.
        /// </summary>
        public bool ConfigPasswordValid { get; set; } = false;

        /// <summary>
        /// Gets or sets a value indicating whether the configuration database exists.
        /// </summary>
        public bool ConfigDBExists { get; set; } = false;
        /// <summary>
        /// Gets or sets a value indicating whether the configuration user exists.
        /// </summary>
        public bool ConfigUserExists { get; set; } = false;

        /// <summary>
        /// Gets or sets a value indicating whether the administrator user has rights.
        /// </summary>
        public bool AdminUserHasRights { get; set; } = false;

        /// <summary>
        /// Gets or sets the certificate thumbprint used for encryption.
        /// </summary>
        public string? CertificateThumbprint { get; set; } = null;
        /// <summary>
        /// Gets or sets the path to the certificate file.
        /// </summary>
        public string? CertificatePath { get; set; } = null;
        /// <summary>
        /// Gets or sets the password for the certificate.
        /// </summary>
        public string? CertificatePassword { get; set; } = null;

        /// <summary>
        /// Gets or sets the SQL Server instance configured for the application.
        /// </summary>
        public string? ConfigSQLServer { get; set; } = null;
        /// <summary>
        /// Gets or sets the configuration database name.
        /// </summary>
        public string? ConfigSQLServerDB { get; set; } = null;
        /// <summary>
        /// Gets or sets the SQL user name for configuration.
        /// </summary>
        public string? ConfigSQLUser { get; set; } = null;
        /// <summary>
        /// Gets or sets the SQL password for configuration.
        /// </summary>
        public string? ConfigSQLPassword { get; set; } = null;
    }
}
