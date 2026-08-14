using MicroM.Configuration;
using MicroM.Data;
using MicroM.DataDictionary.Entities;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace MicroM.Web.Services;

public sealed class FileStoreCleanupService(
    IBackgroundTaskQueue taskQueue, IMicroMAppConfiguration appConfiguration, IFileUploadService fileUploadService,
    ILogger<FileStoreCleanupService> log) : IHostedService
{
    private const string TaskName = "FileStoreCleanupService.CleanupTerminalFiles";

    public Task StartAsync(CancellationToken cancellationToken)
    {
        taskQueue.Enqueue(TaskName, CleanupAllApplications, singleInstance: true, recurrence: TimeSpan.FromHours(24));
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    private async Task<string> CleanupAllApplications(CancellationToken ct)
    {
        var cleaned = 0;
        var failed = 0;

        foreach (var appId in appConfiguration.GetAppIDs())
        {
            if (ct.IsCancellationRequested) break;

            var app = appConfiguration.GetAppConfiguration(appId);
            using var databaseClient = appConfiguration.GetDatabaseClient(appId);
            if (app == null || databaseClient == null)
            {
                failed++;
                log.LogWarning("FileStore cleanup could not create application services for {AppId}", appId);
                continue;
            }

            try
            {
                var result = await CleanupTerminalFiles(app, databaseClient, ct);
                cleaned += result.Cleaned;
                failed += result.Failed;
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                failed++;
                log.LogWarning(ex, "FileStore cleanup failed for application {AppId}; it will retry in 24 hours", appId);
            }
            finally
            {
                await databaseClient.Disconnect();
            }
        }

        return $"FileStore cleanup completed. Cleaned: {cleaned}, failed: {failed}.";
    }

    internal async Task<FileCleanupResult> CleanupTerminalFiles(ApplicationOption app, IEntityClient ec, CancellationToken ct)
    {
        var cleaned = 0;
        var failed = 0;
        var store = new FileStore(ec, schema_name: app.SchemaConfiguration.DDSchema);

        var files = await store.ExecuteProc<FileDetails>(store.Def.fst_qryTerminalFiles, ct, set_parms_from_columns: false, mode: AutoMapperMode.ByNameLaxNotThrow);

        foreach (var file in files)
        {
            ct.ThrowIfCancellationRequested();

            try
            {
                var deletion = await fileUploadService.DeleteFile(app, file.vc_fileguid, ec, ct);
                if (deletion.Failed)
                {
                    failed++;
                    log.LogWarning("FileStore cleanup could not delete file {FileGuid}; it will retry in 24 hours", file.vc_fileguid);
                    continue;
                }

                cleaned++;
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                failed++;
                log.LogWarning(ex, "Could not fully clean file {FileGuid}; it will retry in 24 hours", file.vc_fileguid);
            }
        }

        return new(cleaned, failed);
    }
}
