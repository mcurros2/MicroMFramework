namespace MicroM.Web.Services;

public record DiskFileCacheEntry(FileDetails FileDetails, string app_id, string cachedFilePath, DateTimeOffset? lastAccessed);

