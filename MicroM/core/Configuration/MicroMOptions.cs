namespace MicroM.Configuration
{
    public class MicroMOptions
    {
        public const string MicroM = nameof(MicroM);

        public int DefaultConnectionTimeOutInSecs { get; set; } = -1;
        public int DefaultCommandTimeOutInMins { get; set; } = -1;
        public int DefaultRowLimitForViews { get; set; } = -1;

        public string? ConfigSQLServer { get; set; } = null;
        public string? ConfigSQLServerDB { get; set; } = null;
        public string? CertificateThumbprint { get; set; } = null;

        public string? UploadsFolder { get; set; } = null;

        public string[]? AllowedUploadFileExtensions { get; set; } = null;

        public string? MicroMAPIBaseRootPath { get; set; } = "microm";
        public string? MicroMAPICookieRootPath { get; set; } = "microm";

        public string? DefaultSQLDatabaseCollation { get; set; } = null;
    }

}
