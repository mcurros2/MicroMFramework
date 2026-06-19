using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;

namespace MicroM.Web.Services;

public sealed class AppAssemblyRuntimeManager : IAppAssemblyRuntimeManager, IAsyncDisposable
{
    private sealed record RuntimeState(AssemblyShadowCopyGeneration ShadowCopy, AssemblyLoadContextGeneration Loaded);

    private readonly IAssemblyShadowCopyService _shadowCopy;
    private readonly IAssemblyLoadContextService _loadContexts;
    private readonly IAssemblyFileWatcherService _watchers;
    private readonly ILogger<AppAssemblyRuntimeManager> _log;

    private readonly ConcurrentDictionary<string, RuntimeState> _pending = new(StringComparer.OrdinalIgnoreCase);
    private readonly ConcurrentDictionary<string, RuntimeState> _committed = new(StringComparer.OrdinalIgnoreCase);

    private readonly SemaphoreSlim _gate = new(1, 1);
    private string? _activeGenerationId;
    private bool _disposed;

    public AppAssemblyRuntimeManager(IAssemblyShadowCopyService shadowCopy, IAssemblyLoadContextService loadContexts, IAssemblyFileWatcherService watchers, ILogger<AppAssemblyRuntimeManager> log)
    {
        _shadowCopy = shadowCopy;
        _loadContexts = loadContexts;
        _watchers = watchers;
        _log = log;
    }

    public async Task<PreparedAssemblyGeneration> PrepareGenerationAsync(IReadOnlyCollection<AssemblyCopyRequest> assemblies, CancellationToken ct)
    {
        var shadow = await _shadowCopy.CopyForReloadAsync(assemblies, ct);
        try
        {
            var loaded = await _loadContexts.LoadGenerationAsync(shadow, ct);
            var state = new RuntimeState(shadow, loaded);
            _pending[shadow.generation_id] = state;

            return new PreparedAssemblyGeneration(shadow.generation_id, loaded.AssembliesByKey, shadow.source_folders_to_watch);
        }
        catch
        {
            await _shadowCopy.DeleteGenerationAsync(shadow.generation_id, ct);
            throw;
        }
    }

    public async Task CommitGenerationAsync(string generationId, CancellationToken ct)
    {
        await _gate.WaitAsync(ct);
        try
        {
            if (!_pending.TryRemove(generationId, out var state))
            {
                throw new InvalidOperationException($"Generation {generationId} is not pending.");
            }

            _committed[generationId] = state;

            var previousActive = _activeGenerationId;
            _activeGenerationId = generationId;

            if (!string.IsNullOrWhiteSpace(previousActive) && !previousActive.Equals(generationId, StringComparison.OrdinalIgnoreCase))
            {
                await CleanupCommittedGenerationAsync(previousActive, ct);
            }

            await _shadowCopy.DeleteAllGenerationsExceptAsync(generationId, ct);
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task RollbackGenerationAsync(string generationId, CancellationToken ct)
    {
        await _gate.WaitAsync(ct);
        try
        {
            if (_pending.TryRemove(generationId, out var pending))
            {
                await _loadContexts.UnloadGenerationAsync(pending.ShadowCopy.generation_id, ct);
                await _shadowCopy.DeleteGenerationAsync(pending.ShadowCopy.generation_id, ct);
                return;
            }

            if (_committed.TryRemove(generationId, out var committed))
            {
                if (_activeGenerationId?.Equals(generationId, StringComparison.OrdinalIgnoreCase) == true)
                {
                    _activeGenerationId = null;
                }

                await _loadContexts.UnloadGenerationAsync(committed.ShadowCopy.generation_id, ct);
                await _shadowCopy.DeleteGenerationAsync(committed.ShadowCopy.generation_id, ct);
            }
        }
        finally
        {
            _gate.Release();
        }
    }

    public void EnableFileWatchers(string generationId, Func<string, Task> onAssemblyChangedAsync)
    {
        if (_pending.TryGetValue(generationId, out var pending))
        {
            _watchers.ResetWatchers(
                pending.ShadowCopy.files.Select(f => f.source_assembly_path),
                onAssemblyChangedAsync);
            return;
        }

        if (_committed.TryGetValue(generationId, out var committed))
        {
            _watchers.ResetWatchers(
                committed.ShadowCopy.files.Select(f => f.source_assembly_path),
                onAssemblyChangedAsync);
            return;
        }

        _log.LogWarning("Generation {generationId} not found when enabling file watchers.", generationId);
    }

    public void DisableFileWatchers() => _watchers.StopAll();

    private async Task CleanupCommittedGenerationAsync(string generationId, CancellationToken ct)
    {
        if (!_committed.TryRemove(generationId, out var state)) return;

        await _loadContexts.UnloadGenerationAsync(state.ShadowCopy.generation_id, ct);
        await _shadowCopy.DeleteGenerationAsync(state.ShadowCopy.generation_id, ct);
    }


    public async ValueTask DisposeAsync()
    {
        if (_disposed) return;
        _disposed = true;

        _watchers.StopAll();

        string[] pendingIds;
        string[] committedIds;

        await _gate.WaitAsync(CancellationToken.None);
        try
        {
            pendingIds = _pending.Keys.ToArray();
            committedIds = _committed.Keys.ToArray();
            _activeGenerationId = null;
        }
        finally
        {
            _gate.Release();
        }

        foreach (var id in pendingIds)
        {
            try
            {
                await RollbackGenerationAsync(id, CancellationToken.None);
            }
            catch (Exception ex)
            {
                _log.LogWarning(ex, "Error rolling back pending generation {generationId} during dispose.", id);
            }
        }

        foreach (var id in committedIds)
        {
            try
            {
                await RollbackGenerationAsync(id, CancellationToken.None);
            }
            catch (Exception ex)
            {
                _log.LogWarning(ex, "Error rolling back committed generation {generationId} during dispose.", id);
            }
        }

        _gate.Dispose();
        GC.SuppressFinalize(this);
    }
}