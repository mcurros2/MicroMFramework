namespace MicroM.Web.Services;

public record DiskFileCacheGetEntryResult(DiskFileCacheEntry Entry, Stream fileStream);
