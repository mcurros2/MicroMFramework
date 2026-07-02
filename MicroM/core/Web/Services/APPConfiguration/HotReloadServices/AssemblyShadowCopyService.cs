using MicroM.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace MicroM.Web.Services;

public sealed class AssemblyShadowCopyService : IAssemblyShadowCopyService
{
    private readonly string _storageRoot;
    private readonly ILogger<AssemblyShadowCopyService> _log;

    public AssemblyShadowCopyService(
        IOptions<MicroMOptions> options,
        ILogger<AssemblyShadowCopyService> log)
    {
        _log = log;
        _storageRoot = options.Value.EntitiesDLLStoragePath ?? ConfigurationDefaults.EntitiesDLLStoragePath;
    }

    private static void CopyFile(string sourcePath, string destPath, int maxRetries = 5, int millisecondsDelay = 200)
    {
        var retryCount = 0;
        var delay = TimeSpan.FromMilliseconds(millisecondsDelay);
        while (retryCount < maxRetries)
        {
            try
            {
                File.Copy(sourcePath, destPath, overwrite: true);
                break;
            }
            catch
            {
                retryCount++;
                if (retryCount >= maxRetries)
                {
                    throw;
                }
                Thread.Sleep(delay);
                delay *= 2;
            }
        }
    }

    public Task<AssemblyShadowCopyGeneration> CopyForReloadAsync(IReadOnlyCollection<AssemblyCopyRequest> assemblies, CancellationToken ct)
    {
        var generationId = $"{DateTime.UtcNow:yyyyMMddHHmmssfff}_{Guid.NewGuid():N}";
        var copied = new List<AssemblyCopiedFile>(assemblies.Count);
        var watchFolders = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var request in assemblies)
        {
            ct.ThrowIfCancellationRequested();

            var sourcePath = Path.GetFullPath(request.source_assembly_path);
            var sourceFolder = Path.GetDirectoryName(sourcePath);

            if (!string.IsNullOrWhiteSpace(sourceFolder))
            {
                watchFolders.Add(sourceFolder);

                var destFolder = Path.Combine(_storageRoot, request.app_id, generationId);
                Directory.CreateDirectory(destFolder);

                var destPath = Path.Combine(destFolder, Path.GetFileName(sourcePath));

                CopyFile(sourcePath, destPath);

                // copy test data if exists. Copy the complete folder and subfolders in ConfigurationDefaults.TestDataRootFolderName
                var testDataFolder = Path.Combine(sourceFolder, ConfigurationDefaults.TestDataRootFolderName);
                if (Directory.Exists(testDataFolder))
                {
                    var destTestDataFolder = Path.Combine(destFolder, ConfigurationDefaults.TestDataRootFolderName);
                    Directory.CreateDirectory(destTestDataFolder);
                    foreach (var file in Directory.EnumerateFiles(testDataFolder, "*.json", SearchOption.AllDirectories))
                    {
                        var relativePath = Path.GetRelativePath(testDataFolder, Path.GetDirectoryName(file) + "/");
                        var destFileDir = Path.Combine(destTestDataFolder, relativePath);
                        Directory.CreateDirectory(destFileDir);
                        var destFile = Path.Combine(destFileDir, Path.GetFileName(file));
                        CopyFile(file, destFile);
                    }
                }

                copied.Add(new AssemblyCopiedFile(request.app_id, sourcePath, destPath));
                _log.LogInformation("Copied assembly {source} => {dest}", sourcePath, destPath);
            }

        }

        return Task.FromResult(new AssemblyShadowCopyGeneration(generationId, copied, watchFolders));
    }

    public Task DeleteGenerationAsync(string generationId, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(generationId)) return Task.CompletedTask;
        if (!Directory.Exists(_storageRoot)) return Task.CompletedTask;

        foreach (var appDir in Directory.EnumerateDirectories(_storageRoot))
        {
            ct.ThrowIfCancellationRequested();

            var generationDir = Path.Combine(appDir, generationId);
            if (!Directory.Exists(generationDir)) continue;

            try
            {
                Directory.Delete(generationDir, recursive: true);
            }
            catch (Exception ex)
            {
                _log.LogWarning(ex, "Expected cleanup warning: generation folder still in use, skipping delete for now. Folder: {folder}", generationDir);
            }
        }

        return Task.CompletedTask;
    }

    public Task DeleteAllGenerationsAsync(CancellationToken ct)
    {
        if (!Directory.Exists(_storageRoot)) return Task.CompletedTask;

        foreach (var appDir in Directory.EnumerateDirectories(_storageRoot))
        {
            ct.ThrowIfCancellationRequested();

            try
            {
                Directory.Delete(appDir, recursive: true);
                _log.LogInformation("Deleted shadow-copy app folder {folder}", appDir);
            }
            catch (Exception ex)
            {
                _log.LogWarning(ex, "Expected cleanup warning: could not delete shadow-copy app folder {folder}", appDir);
            }
        }

        return Task.CompletedTask;
    }

    public Task DeleteAllGenerationsExceptAsync(string generationIdToKeep, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(generationIdToKeep)) return Task.CompletedTask;
        if (!Directory.Exists(_storageRoot)) return Task.CompletedTask;

        foreach (var appDir in Directory.EnumerateDirectories(_storageRoot))
        {
            ct.ThrowIfCancellationRequested();

            foreach (var generationDir in Directory.EnumerateDirectories(appDir))
            {
                ct.ThrowIfCancellationRequested();

                var generationId = Path.GetFileName(generationDir);
                if (generationId.Equals(generationIdToKeep, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                try
                {
                    Directory.Delete(generationDir, recursive: true);
                }
                catch (Exception ex)
                {
                    _log.LogWarning(ex, "Expected cleanup warning: generation folder still in use, skipping delete for now. Folder: {folder}", generationDir);
                }
            }
        }

        return Task.CompletedTask;
    }
}