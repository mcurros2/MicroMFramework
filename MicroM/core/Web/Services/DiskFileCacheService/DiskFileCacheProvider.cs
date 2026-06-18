using Microsoft.Extensions.Logging;

namespace MicroM.Web.Services;

internal static class DiskFileCacheProvider
{

    public static DiskFileCacheEntry CreateEntryFromSourceFileDetails(string app_id, FileDetails sourceFileDetails, string finalPath, DateTimeOffset lastAccessed, ILogger log)
    {
        var info = new FileInfo(finalPath);
        var cloned = CloneFileDetails(sourceFileDetails);

        if (info.Length != cloned.bi_filesize)
        {
            log.LogDebug("File size mismatch for cache entry. Expected {ExpectedSize} bytes but found {ActualSize} bytes. Corrected to actual size.", cloned.bi_filesize, info.Length);
            cloned.bi_filesize = info.Length;
        }

        return new DiskFileCacheEntry(cloned, app_id, finalPath, lastAccessed);
    }

    internal static string GetFinalPath(string app_id, FileDetails sourceFileDetails, string cacheRoot)
    {
        var root = Path.Combine(cacheRoot, app_id);
        var cachedFilePath = Path.GetFullPath(Path.Combine(root, sourceFileDetails.vc_filefolder, sourceFileDetails.vc_fileguid));

        if (!cachedFilePath.StartsWith(root, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Resolved cache path escapes the cache root.");
        }

        return cachedFilePath;
    }

    internal static string GetTempFilePath(string app_id, FileDetails sourceFileDetails, string cacheTempRoot, string tmpExtension)
    {
        var tempRoot = Path.Combine(cacheTempRoot, app_id);

        var tempFileName = $"{sourceFileDetails.vc_fileguid.Replace('.', '_')}-{Guid.NewGuid():N}{tmpExtension}";

        var tempPath = Path.GetFullPath(Path.Combine(tempRoot, tempFileName));
        if (!tempPath.StartsWith(tempRoot, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Resolved temp cache path escapes the cache root.");
        }
        return tempPath;
    }

    internal static FileStream? TryOpenReadStream(string path, int readBufferSize, ILogger log)
    {
        try
        {
            return new FileStream(
                path,
                FileMode.Open, FileAccess.Read, FileShare.Read,
                readBufferSize,
                FileOptions.Asynchronous | FileOptions.SequentialScan
                );

        }
        catch (Exception ex)
        {
            log.LogDebug(ex, "Failed to open cache file for reading: {path}", path);
            return null;
        }

    }

    internal static FileStream OpenWriteTempStream(string path, int writeBufferSize)
    {
        return new FileStream(
            path,
            FileMode.CreateNew,
            FileAccess.Write,
            FileShare.None,
            writeBufferSize,
            FileOptions.Asynchronous | FileOptions.SequentialScan
            );
    }

    internal static void TryDeleteFile(string? path)
    {
        if (string.IsNullOrWhiteSpace(path)) return;

        try
        {
            if (File.Exists(path)) File.Delete(path);
        }
        catch
        {
            // Best-effort cache cleanup.
            // IO races are expected; background trimming can retry later.
        }
    }

    internal static bool UsableCacheFileExists(string? path, string tmpExtension)
    {
        if (string.IsNullOrWhiteSpace(path)) return false;

        if (path.EndsWith(tmpExtension, StringComparison.OrdinalIgnoreCase)) return false;

        return File.Exists(path);
    }

    internal static void ValidateSourceFileDetails(FileDetails fileDetails)
    {
        ArgumentNullException.ThrowIfNull(fileDetails);

        if (string.IsNullOrWhiteSpace(fileDetails.vc_fileguid)) throw new ArgumentException("FileDetails.vc_fileguid is required.", nameof(fileDetails));

        if (string.IsNullOrWhiteSpace(fileDetails.vc_filefolder)) throw new ArgumentException("FileDetails.vc_filefolder is required.", nameof(fileDetails));
    }

    internal static FileDetails CloneFileDetails(FileDetails source)
    {
        return new FileDetails
        {
            c_file_id = source.c_file_id,
            c_fileprocess_id = source.c_fileprocess_id,
            vc_filename = source.vc_filename,
            vc_filefolder = source.vc_filefolder,
            vc_fileguid = source.vc_fileguid,
            bi_filesize = source.bi_filesize,
            vc_file_tag = source.vc_file_tag,
            c_filestoragetype_id = source.c_filestoragetype_id,
            c_fileuploadstatus_id = source.c_fileuploadstatus_id,
            fullPath = source.fullPath
        };
    }


}
