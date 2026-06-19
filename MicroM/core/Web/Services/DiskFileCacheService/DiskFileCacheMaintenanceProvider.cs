using Microsoft.Extensions.Logging;

namespace MicroM.Web.Services;

internal static class DiskFileCacheMaintenanceProvider
{
    internal static long TrimCache(IDiskFileCacheService cacheService, long maxCacheSizeBytes, double cacheTrimTargetRatio, CancellationToken ct)
    {
        var maxBytes = maxCacheSizeBytes;
        var targetRatio = cacheTrimTargetRatio;

        ct.ThrowIfCancellationRequested();

        if (cacheService.KnownSizeBytes <= maxBytes) return -1;

        var targetBytes = (long)(maxBytes * targetRatio);

        var candidates = cacheService.GetEntriesSnapshot();

        foreach (var victim in candidates)
        {
            ct.ThrowIfCancellationRequested();

            if (cacheService.KnownSizeBytes <= targetBytes) break;

            cacheService.RemoveEntry(victim.app_id, victim.FileDetails);
        }

        return cacheService.KnownSizeBytes;
    }

    internal static Task<DiskFileCacheReconciliationResult> ReconcileCache(IDiskFileCacheService cacheService, IMicroMAppConfiguration appConfig, string cacheRoot, string tmpRoot, string tmpExtension, ILogger log, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        var tempFilesDeleted = DeleteTempFilesBestEffort(tmpRoot, tmpExtension, log);

        var filesScanned = 0;
        var entriesAdded = 0;
        var entriesAlreadyKnown = 0;

        foreach (var filePath in EnumerateExistingCacheFiles(cacheRoot, tmpRoot, tmpExtension, log))
        {
            ct.ThrowIfCancellationRequested();

            filesScanned++;

            var fileInfo = new FileInfo(filePath);

            if (!fileInfo.Exists) continue;

            // extract app_id from fileInfo.FullName using .net Path functions. app_id is the first directory under cacheRoot.
            var relativePath = Path.GetRelativePath(cacheRoot, fileInfo.FullName);
            var app_id = relativePath.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)[0];

            if (string.IsNullOrEmpty(app_id))
            {
                log.LogWarning("Disk file cache entry found with no app_id: {file}. You will need to delete the file manually.", fileInfo.FullName);
                continue;
            }

            var appCacheRoot = Path.Combine(cacheRoot, app_id);

            var fileDetails = new FileDetails
            {
                vc_fileguid = fileInfo.Name,
                vc_filefolder = GetRelativeCacheFolder(fileInfo.FullName, appCacheRoot),
                bi_filesize = fileInfo.Length,
                fullPath = fileInfo.FullName
            };

            var get_result = cacheService.GetEntry(app_id, fileDetails);

            if (get_result != null)
            {
                entriesAlreadyKnown++;
                get_result.fileStream.Dispose();
                continue;
            }

            if (appConfig.GetAppConfiguration(app_id) != null)
            {
                cacheService.AddExitingFileEntry(app_id, fileDetails);
                entriesAdded++;
            }
            else
            {
                log.LogWarning("Disk file cache entry found for unknown app_id: {app_id}, file: {file}. You will need to delete the file manually.", app_id, fileInfo.FullName);
            }

        }

        return Task.FromResult(new DiskFileCacheReconciliationResult(cacheService.KnownSizeBytes, filesScanned, entriesAdded, entriesAlreadyKnown, tempFilesDeleted));
    }

    private static IEnumerable<string> EnumerateExistingCacheFiles(string cacheRoot, string tmpRoot, string tmpExtension, ILogger log)
    {
        if (!Directory.Exists(cacheRoot)) yield break;

        IEnumerable<string> files;

        try
        {
            files = Directory.EnumerateFiles(cacheRoot, "*", SearchOption.AllDirectories);
        }
        catch (Exception ex)
        {
            log.LogDebug(ex, "Failed to enumerate disk file cache root: {path}", cacheRoot);
            yield break;
        }

        foreach (var path in files)
        {
            if (IsTempPath(path, tmpRoot)) continue;

            if (path.EndsWith(tmpExtension, StringComparison.OrdinalIgnoreCase)) continue;

            if (!File.Exists(path)) continue;

            yield return path;
        }
    }

    private static int DeleteTempFilesBestEffort(string tmpRoot, string tmpExtension, ILogger log)
    {
        if (!Directory.Exists(tmpRoot)) return 0;

        var deleted = 0;

        IEnumerable<string> tempFiles;

        try
        {
            tempFiles = Directory.EnumerateFiles(tmpRoot, "*" + tmpExtension, SearchOption.AllDirectories);
        }
        catch (Exception ex)
        {
            log.LogDebug(ex, "Failed to enumerate disk file cache temp root: {path}", tmpRoot);
            return 0;
        }

        foreach (var path in tempFiles)
        {
            try
            {
                if (!File.Exists(path))
                    continue;

                File.Delete(path);
                deleted++;
            }
            catch (Exception ex)
            {
                log.LogDebug(ex, "Failed to delete disk file cache temp file: {path}", path);
            }
        }

        return deleted;
    }

    private static bool IsTempPath(string path, string cacheTempRoot)
    {
        var fullPath = Path.GetFullPath(path);

        var tempRoot = cacheTempRoot.EndsWith(Path.DirectorySeparatorChar)
            ? cacheTempRoot
            : cacheTempRoot + Path.DirectorySeparatorChar;

        return fullPath.StartsWith(tempRoot, StringComparison.OrdinalIgnoreCase);
    }

    private static string GetRelativeCacheFolder(string fullPath, string cacheRoot)
    {
        var directory = Path.GetDirectoryName(Path.GetFullPath(fullPath));

        if (string.IsNullOrWhiteSpace(directory)) return "";

        var relativeDirectory = Path.GetRelativePath(cacheRoot, directory);

        if (relativeDirectory == "." || relativeDirectory == "..") return "";

        return relativeDirectory.Replace('\\', '/');
    }

}
