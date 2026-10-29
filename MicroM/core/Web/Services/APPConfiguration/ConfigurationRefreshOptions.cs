namespace MicroM.Web.Services;

public sealed record ConfigurationRefreshOptions
{
    public bool SkipDatabaseUpdates { get; init; }
    public bool SkipSeedTestData { get; init; }
}
