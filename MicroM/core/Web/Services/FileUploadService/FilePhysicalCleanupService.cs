using MicroM.Core;
using Microsoft.Extensions.Logging;

namespace MicroM.Web.Services;

public sealed class FilePhysicalCleanupService(IDiskFileCacheService diskCache, ILogger<FilePhysicalCleanupService> log) : IFilePhysicalCleanupService
{
    public bool TryCleanup(string appId, FileDetails fileDetails)
    {
        var cleaned = diskCache.RemoveEntry(appId, fileDetails);

        try
        {
            cleaned &= FilesProvider.TryDeleteFile(fileDetails.fullPath);

            var directory = Path.GetDirectoryName(fileDetails.fullPath);
            if (!string.IsNullOrWhiteSpace(directory) && Directory.Exists(directory))
            {
                var baseName = Path.GetFileNameWithoutExtension(fileDetails.fullPath);
                var extension = Path.GetExtension(fileDetails.fullPath);
                foreach (var candidate in Directory.EnumerateFiles(directory))
                {
                    var name = Path.GetFileName(candidate);
                    if (name.StartsWith($"{baseName}-thmb-", StringComparison.OrdinalIgnoreCase)
                        && name.EndsWith(extension, StringComparison.OrdinalIgnoreCase))
                    {
                        cleaned &= FilesProvider.TryDeleteFile(candidate);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            log.LogWarning(ex, "Physical cleanup failed for file {FileGuid}", fileDetails.vc_fileguid);
            return false;
        }

        return cleaned;
    }


}
