namespace MicroM.Configuration
{
    public record SecretsOptions
    {
        public string? ConfigSQLUser { get; set; } = null;
        public string? ConfigSQLPassword { get; set; } = null;
    }
}
