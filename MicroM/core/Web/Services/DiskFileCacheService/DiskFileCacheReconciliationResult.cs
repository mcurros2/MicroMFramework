namespace MicroM.Web.Services;

public sealed record DiskFileCacheReconciliationResult(long KnownSizeBytes, int FilesScanned, int EntriesAdded, int EntriesAlreadyKnown, int TempFilesDeleted);