namespace MicroM.Configuration
{
    /// <summary>
    /// Options for storing sensitive configuration values such as database credentials.
    /// </summary>
    public record SecretsOptions
    {
        /// <summary>SQL user for the configuration database.</summary>
        public string? ConfigSQLUser { get; set; } = null;

        /// <summary>Password for the configuration database user.</summary>
        public string? ConfigSQLPassword { get; set; } = null;
    }
}
