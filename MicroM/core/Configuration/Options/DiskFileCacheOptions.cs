namespace MicroM.Configuration;

public sealed class DiskFileCacheOptions
{
    public string RootPath { get; set; } = "";

    public string TempFolderName { get; set; } = "_tmp";

    public int CopyBufferSize { get; set; } = 1024 * 128;

    public int ReadBufferSize { get; set; } = 1024 * 128;

    public int WriteBufferSize { get; set; } = 1024 * 128;

    public long MaxCacheSizeBytes { get; set; } = 100L * 1024 * 1024 * 1024; // 100 GB

    public double CacheTrimTargetRatio { get; set; } = 0.8; // Trim to 80% of max size when trimming

}