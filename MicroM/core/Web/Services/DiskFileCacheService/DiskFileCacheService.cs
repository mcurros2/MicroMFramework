using MicroM.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Collections.Concurrent;
using static MicroM.Web.Services.DiskFileCacheProvider;

namespace MicroM.Web.Services;


public sealed class DiskFileCacheService : IDiskFileCacheService
{
    private readonly MicroMOptions _options;
    private readonly DiskFileCacheOptions _cacheOptions;
    private readonly ConcurrentDictionary<string, DiskFileCacheEntry> _entries = new(StringComparer.OrdinalIgnoreCase);

    private long _knownSizeBytes;

    private readonly string _cacheRoot;
    private readonly string _cacheTempRoot;

    private readonly ILogger<DiskFileCacheService> _log;
    private readonly IMemoryEventsService _bus;
    private readonly IMicroMAppConfiguration _appConfig;

    const string tmpExtension = ".__tmp";

    public DiskFileCacheService(IMicroMAppConfiguration appConfig, IOptions<MicroMOptions> options, ILogger<DiskFileCacheService> log, IMemoryEventsService event_bus)
    {
        _options = options.Value;
        ArgumentNullException.ThrowIfNull(_options.DiskFileCacheOptions);
        _cacheOptions = _options.DiskFileCacheOptions;

        if (string.IsNullOrWhiteSpace(_cacheOptions.RootPath)) throw new ArgumentException("RootPath is required.", nameof(options));

        _cacheRoot = Path.GetFullPath(Path.Join(_cacheOptions.RootPath, Path.DirectorySeparatorChar.ToString()));
        _cacheTempRoot = Path.Combine(_cacheRoot, _cacheOptions.TempFolderName);
        _log = log;
        _bus = event_bus;
        _appConfig = appConfig;

        Directory.CreateDirectory(_cacheRoot);
        Directory.CreateDirectory(_cacheTempRoot);
    }

    public long KnownSizeBytes => Interlocked.Read(ref _knownSizeBytes);

    public IReadOnlyList<DiskFileCacheEntry> GetEntriesSnapshot()
    {
        return [.. _entries.Values.OrderBy(x => x.lastAccessed ?? DateTimeOffset.MinValue)];
    }

    public DiskFileCacheGetEntryResult? GetEntry(string app_id, FileDetails sourceFileDetails)
    {
        ValidateSourceFileDetails(sourceFileDetails);

        var key = $"{app_id}:{sourceFileDetails.vc_fileguid}";

        try
        {
            if (_entries.TryGetValue(key, out var existingEntry))
            {
                FileStream? cachedFileStream = TryOpenReadStream(existingEntry.cachedFilePath, _cacheOptions.ReadBufferSize, _log);
                if (cachedFileStream is not null)
                {
                    var lastAccessedEntry = existingEntry with
                    {
                        lastAccessed = DateTimeOffset.UtcNow
                    };

                    _entries[key] = lastAccessedEntry;
                    return new DiskFileCacheGetEntryResult(lastAccessedEntry, cachedFileStream);
                }

                RemoveEntry(key);
                return null;
            }

            var finalPath = GetFinalPath(app_id, sourceFileDetails, _cacheRoot);

            // Check to see if the file exists before failing the cache
            if (!UsableCacheFileExists(finalPath, tmpExtension))
            {
                return null;
            }

            var existingCachedFileEntry = CreateEntryFromSourceFileDetails(app_id, sourceFileDetails, finalPath, DateTimeOffset.UtcNow, _log);

            UpsertEntry(key, existingCachedFileEntry);

            var stream = TryOpenReadStream(finalPath, _cacheOptions.ReadBufferSize, _log);

            if (stream is null)
            {
                RemoveEntry(key);
                return null;
            }

            return new DiskFileCacheGetEntryResult(existingCachedFileEntry, stream);
        }
        catch (Exception ex)
        {
            _log.LogError(ex, "Error getting cache entry for file '{fileId}'", key);
            return null;
        }

    }

    public async Task<Stream?> AddEntry(string app_id, FileDetails sourceFileDetails, Stream sourceStream, CancellationToken ct)
    {
        ValidateSourceFileDetails(sourceFileDetails);
        ArgumentNullException.ThrowIfNull(sourceStream);

        var key = $"{app_id}:{sourceFileDetails.vc_fileguid}";

        string? tempPath = null;

        string finalPath = "";
        try
        {
            finalPath = GetFinalPath(app_id, sourceFileDetails, _cacheRoot);
            var finalDirectory = Path.GetDirectoryName(finalPath) ?? throw new InvalidOperationException($"Invalid cache path for file '{key}'.");

            if (UsableCacheFileExists(finalPath, tmpExtension))
            {
                var existingEntry = CreateEntryFromSourceFileDetails(app_id, sourceFileDetails, finalPath, DateTimeOffset.UtcNow, _log);

                UpsertEntry(key, existingEntry);
                return TryOpenReadStream(finalPath, _cacheOptions.ReadBufferSize, _log);
            }

            tempPath = GetTempFilePath(app_id, sourceFileDetails, _cacheTempRoot, tmpExtension);

            Directory.CreateDirectory(Path.Combine(_cacheTempRoot, app_id));

            await using (var output = OpenWriteTempStream(tempPath, _cacheOptions.WriteBufferSize))
            {
                await sourceStream.CopyToAsync(output, _cacheOptions.CopyBufferSize, ct);
                await output.FlushAsync(ct);
            }

            Directory.CreateDirectory(finalDirectory);

            try
            {
                File.Move(tempPath, finalPath, overwrite: false);
            }
            catch (IOException) when (File.Exists(finalPath))
            {
                // Another request/process published it first.
            }

            var newEntry = CreateEntryFromSourceFileDetails(app_id, sourceFileDetails, finalPath, DateTimeOffset.UtcNow, _log);

            UpsertEntry(key, newEntry);

            return TryOpenReadStream(finalPath, _cacheOptions.ReadBufferSize, _log);
        }
        catch (Exception ex)
        {
            _log.LogError(ex, "Error adding cache entry for file '{fileId}'", key);
        }
        finally
        {
            TryDeleteFile(tempPath);
            if (KnownSizeBytes > _cacheOptions.MaxCacheSizeBytes)
            {
                _bus.Publish(new DiskFileCacheEntryAddedEvent());
            }
        }

        return null;
    }

    public void AddExitingFileEntry(string app_id, FileDetails sourceFileDetails)
    {
        var key = $"{app_id}:{sourceFileDetails.vc_fileguid}";
        UpsertEntry(key, CreateEntryFromSourceFileDetails(app_id, sourceFileDetails, GetFinalPath(app_id, sourceFileDetails, _cacheRoot), DateTimeOffset.UtcNow, _log));
    }

    public long TrimCache(CancellationToken ct)
    {
        return DiskFileCacheMaintenanceProvider.TrimCache(this, _cacheOptions.MaxCacheSizeBytes, _cacheOptions.CacheTrimTargetRatio, ct);
    }

    public Task<DiskFileCacheReconciliationResult> ReconcileCache(CancellationToken ct)
    {
        return DiskFileCacheMaintenanceProvider.ReconcileCache(this, _appConfig, _cacheRoot, _cacheTempRoot, tmpExtension, _log, ct);
    }

    public void RemoveEntry(string app_id, FileDetails sourceFileDetails)
    {
        ValidateSourceFileDetails(sourceFileDetails);

        var key = $"{app_id}:{sourceFileDetails.vc_fileguid}";

        RemoveEntry(key);
    }


    private void UpsertEntry(string key, DiskFileCacheEntry newEntry)
    {
        _entries.AddOrUpdate(
            key,
            _ =>
            {
                Interlocked.Add(ref _knownSizeBytes, newEntry.FileDetails.bi_filesize);
                return newEntry;
            },
            (_, oldEntry) =>
            {
                Interlocked.Add(ref _knownSizeBytes, newEntry.FileDetails.bi_filesize - oldEntry.FileDetails.bi_filesize);
                return newEntry;
            });
    }

    private void RemoveEntry(string key)
    {
        if (_entries.TryRemove(key, out var existingEntry))
        {
            Interlocked.Add(ref _knownSizeBytes, -existingEntry.FileDetails.bi_filesize);
            TryDeleteFile(existingEntry.cachedFilePath);
        }
    }

}
