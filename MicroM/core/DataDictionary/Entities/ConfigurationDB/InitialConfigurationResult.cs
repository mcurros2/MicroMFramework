using MicroM.Core;

namespace MicroM.DataDictionary
{
    public record InitialConfigurationResult : EntityActionResult
    {
        //public bool ConfigSQLServerValid = false;
        public bool ConfigDBValid { get; set; } = false;
        public bool ConfigUserameValid { get; set; } = false;
        public bool ConfigPasswordValid { get; set; } = false;

        public bool ConfigDBExists { get; set; } = false;
        public bool ConfigUserExists { get; set; } = false;

        public bool AdminUserHasRights { get; set; } = false;

        public string? CertificateThumbprint { get; set; } = null;
        public string? CertificatePath { get; set; } = null;
        public string? CertificatePassword { get; set; } = null;

        public string? ConfigSQLServer { get; set; } = null;
        public string? ConfigSQLServerDB { get; set; } = null;
        public string? ConfigSQLUser { get; set; } = null;
        public string? ConfigSQLPassword { get; set; } = null;
    }
}
