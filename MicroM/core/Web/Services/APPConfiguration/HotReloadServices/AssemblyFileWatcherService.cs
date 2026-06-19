using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;

namespace MicroM.Web.Services;

public sealed class AssemblyFileWatcherService : IAssemblyFileWatcherService
{
    private sealed record WatcherRegistration(FileSystemWatcher Watcher, HashSet<string> WatchedFiles);

    private readonly ConcurrentDictionary<string, WatcherRegistration> _watchers = new(StringComparer.OrdinalIgnoreCase);
    private readonly ILogger<AssemblyFileWatcherService> _log;
    private bool _disposed;

    public AssemblyFileWatcherService(ILogger<AssemblyFileWatcherService> log) { _log = log; }

    public void ResetWatchers(IEnumerable<string> sourceAssemblyPaths, Func<string, Task> onAssemblyChangedAsync)
    {
        if (_disposed)
        {
            _log.LogError("Attempted to reset assembly file watchers after the service has been disposed.");
            return;
        }
        StopAll();

        var normalized = sourceAssemblyPaths
            .Where(p => !string.IsNullOrWhiteSpace(p))
            .Select(Path.GetFullPath)
            .Distinct(StringComparer.OrdinalIgnoreCase);

        foreach (var assemblyPath in normalized)
        {
            var folder = Path.GetDirectoryName(assemblyPath);
            if (string.IsNullOrWhiteSpace(folder)) continue;

            if (!_watchers.TryGetValue(folder, out var registration))
            {
                var watcher = new FileSystemWatcher(folder, "*.dll")
                {
                    NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.CreationTime | NotifyFilters.FileName,
                    EnableRaisingEvents = false
                };

                registration = new WatcherRegistration(
                    watcher,
                    new HashSet<string>(StringComparer.OrdinalIgnoreCase));

                watcher.Changed += (_, e) => _ = HandleChangeAsync(e.FullPath, onAssemblyChangedAsync);
                watcher.Created += (_, e) => _ = HandleChangeAsync(e.FullPath, onAssemblyChangedAsync);
                watcher.Renamed += (_, e) => _ = HandleChangeAsync(e.FullPath, onAssemblyChangedAsync);

                _watchers[folder] = registration;
            }

            registration.WatchedFiles.Add(assemblyPath);
        }

        foreach (var registration in _watchers.Values)
        {
            registration.Watcher.EnableRaisingEvents = true;
        }
    }

    private async Task HandleChangeAsync(string fullPath, Func<string, Task> callback)
    {
        if (_disposed) return;

        var folder = Path.GetDirectoryName(fullPath);
        if (string.IsNullOrWhiteSpace(folder)) return;

        if (!_watchers.TryGetValue(folder, out var registration)) return;
        if (!registration.WatchedFiles.Contains(fullPath)) return;

        try
        {
            await callback(fullPath);
        }
        catch (Exception ex)
        {
            _log.LogError(ex, "Error while processing assembly file watcher callback for {path}", fullPath);
        }
    }

    public void StopAll()
    {
        foreach (var kvp in _watchers.ToArray())
        {
            try
            {
                kvp.Value.Watcher.EnableRaisingEvents = false;
                kvp.Value.Watcher.Dispose();
            }
            catch (Exception ex)
            {
                _log.LogWarning(ex, "Could not stop watcher for folder {folder}", kvp.Key);
            }
        }

        _watchers.Clear();
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        StopAll();
        GC.SuppressFinalize(this);
    }

}