using MicroM.Core;
using Microsoft.Extensions.Logging;

namespace MicroM.Web.Services;

public sealed class FilePhysicalCleanupService(IDiskFileCacheService diskCache, IThumbnailService thumbnailService, ILogger<FilePhysicalCleanupService> log) : IFilePhysicalCleanupService
{
    public bool TryCleanup(string appId, FileDetails fileDetails)
    {
        var cleaned = diskCache.RemoveEntry(appId, fileDetails);

        try
        {
            cleaned &= FilesProvider.TryDeleteFile(fileDetails.fullPath);

            var thumb = thumbnailService.GetThumbnailFilename(fileDetails.fullPath);

            cleaned &= FilesProvider.TryDeleteFile(thumb.fullDestinationPath);
        }
        catch (Exception ex)
        {
            log.LogWarning(ex, "Physical cleanup failed for file {FileGuid}", fileDetails.vc_fileguid);
            return false;
        }

        return cleaned;
    }


}
