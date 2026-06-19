namespace MicroM.Web.Services;

public sealed record DiskFileCacheEntrySnapshot(string Key, string CachedFilePath, long SizeBytes, DateTimeOffset LastAccessed);