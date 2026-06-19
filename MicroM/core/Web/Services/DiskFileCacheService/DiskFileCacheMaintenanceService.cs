using MicroM.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace MicroM.Web.Services;

public class DiskFileCacheMaintenanceService(
    IDiskFileCacheService disk_cache, IBackgroundTaskQueue task_queue, ILogger<DiskFileCacheMaintenanceService> log,
    IOptions<MicroMOptions> options, IMemoryEventsService bus
    ) : IDiskFileCacheMaintenanceService, IHostedService
{
    private CancellationTokenSource? _serviceCTS;
    private MicroMOptions _options = options.Value;

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        _serviceCTS = CancellationTokenSource.CreateLinkedTokenSource(task_queue.QueueCT, cancellationToken);

        log.LogInformation(
            "DiskFileCacheMaintenanceService starting. Cache root: {cacheRoot}. Temp folder: {tempFolder}. Cache size: {cacheSize}, trim ratio: {targetRatio}",
            _options.DiskFileCacheOptions?.RootPath, _options.DiskFileCacheOptions?.TempFolderName, _options.DiskFileCacheOptions?.MaxCacheSizeBytes, _options.DiskFileCacheOptions?.CacheTrimTargetRatio
            );

        bus.Subscribe<DiskFileCacheEntryAddedEvent>(async _ =>
        {
            log.LogDebug("Clearing application certificate cache due to MicroMConfigurationReloaded");
            await StartTrimming(_serviceCTS.Token);
        });

        await StartReconcileCache(_serviceCTS.Token);

        log.LogInformation("DiskFileCacheMaintenanceService is started.");
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        log.LogInformation("DiskFileCacheMaintenanceService is stopping.");
        _serviceCTS?.Cancel();
    }

    private Task StartTrimming(CancellationToken cancellationToken)
    {
        task_queue.Enqueue("DiskFileCacheMaintenanceService.TrimCache", async ct =>
        {
            try
            {
                var trimmed_size = disk_cache.TrimCache(ct); // Example: trim to 8GB if over 10GB
                if (trimmed_size > -1)
                {
                    log.LogInformation("DiskFileCacheMaintenanceService trimmed cache. Current size: {size} bytes", trimmed_size);
                    return $"Trimmed cache to {trimmed_size} bytes";
                }
            }
            catch (OperationCanceledException)
            {
                log.LogDebug("DiskFileCacheMaintenanceService trimming operation was canceled.");
            }
            catch (Exception ex)
            {
                log.LogError(ex, "An error occurred during DiskFileCacheMaintenanceService trimming operation.");
            }
            return "";
        }, singleInstance: true, delayedStart: TimeSpan.FromMinutes(5));

        return Task.CompletedTask;
    }

    private Task StartReconcileCache(CancellationToken cancellationToken)
    {
        task_queue.Enqueue("DiskFileCacheMaintenanceService.ReconcileCache", async ct =>
        {
            try
            {
                if (string.IsNullOrEmpty(_options.DiskFileCacheOptions?.RootPath))
                {
                    log.LogWarning(
                        "DiskFileCacheMaintenanceService reconciliation skipped because cache root path is not configured. Please set DiskFileCacheOptions.RootPath in configuration to enable cache maintenance."
                        );
                    return "Reconciliation skipped due to missing cache root path configuration";
                }

                if (!Directory.Exists(_options.DiskFileCacheOptions.RootPath))
                {
                    log.LogWarning(
                        "DiskFileCacheMaintenanceService reconciliation skipped because cache root path '{cacheRoot}' does not exist. Please ensure the directory exists and is accessible.",
                        _options.DiskFileCacheOptions.RootPath
                        );
                    return "Reconciliation skipped due to non-existent cache root directory";
                }

                var result = await disk_cache.ReconcileCache(ct);

                if (result != null)
                {
                    log.LogInformation(
                        "DiskFileCacheMaintenanceService reconciliation completed: {result.FilesScanned} files scanned, {result.TempFilesDeleted} temp files removed, {result.EntriesAdded} files added to cache, Entries found or added to cache {result.EntriesAlreadyKnown} {result.KnownSizeBytes} actual cache size.",
                        result.FilesScanned, result.TempFilesDeleted, result.EntriesAdded, result.EntriesAlreadyKnown, result.KnownSizeBytes
                        );
                    await StartTrimming(ct);
                }
                return "Reconciliation completed";
            }
            catch (OperationCanceledException)
            {
                log.LogDebug("DiskFileCacheMaintenanceService reconciliation operation was canceled.");
            }
            catch (Exception ex)
            {
                log.LogError(ex, "An error occurred during DiskFileCacheMaintenanceService reconciliation operation.");
            }
            return "";
        }, singleInstance: true);

        return Task.CompletedTask;
    }
}
