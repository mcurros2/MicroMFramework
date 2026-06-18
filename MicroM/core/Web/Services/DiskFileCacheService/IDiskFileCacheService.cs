namespace MicroM.Web.Services;

public interface IDiskFileCacheService
{
    long KnownSizeBytes { get; }
    IReadOnlyList<DiskFileCacheEntry> GetEntriesSnapshot();
    DiskFileCacheGetEntryResult? GetEntry(string app_id, FileDetails sourceFileDetails);
    Task<Stream?> AddEntry(string app_id, FileDetails sourceFileDetails, Stream sourceStream, CancellationToken ct);
    void RemoveEntry(string app_id, FileDetails sourceFileDetails);
    void AddExitingFileEntry(string app_id, FileDetails sourceFileDetails);
    long TrimCache(CancellationToken ct);
    Task<DiskFileCacheReconciliationResult> ReconcileCache(CancellationToken ct);

}
