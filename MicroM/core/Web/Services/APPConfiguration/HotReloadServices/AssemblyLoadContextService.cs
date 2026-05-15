using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;
using System.Reflection;
using System.Runtime.Loader;

namespace MicroM.Web.Services;

public sealed class AssemblyLoadContextService : IAssemblyLoadContextService, IAsyncDisposable
{
    private sealed class GenerationLoadContext : AssemblyLoadContext
    {
        private readonly Dictionary<string, string> _assemblyPathBySimpleName;

        public GenerationLoadContext(string generationId, Dictionary<string, string> assemblyPathBySimpleName) : base($"MicroM.Entities.{generationId}", isCollectible: true)
        {
            _assemblyPathBySimpleName = assemblyPathBySimpleName;
        }

        protected override Assembly? Load(AssemblyName assemblyName)
        {
            if (_assemblyPathBySimpleName.TryGetValue(assemblyName.Name ?? string.Empty, out var path))
            {
                return LoadFromAssemblyPath(path);
            }

            return null;
        }
    }

    private sealed record GenerationState(GenerationLoadContext Context);

    private readonly ConcurrentDictionary<string, GenerationState> _generations = new(StringComparer.OrdinalIgnoreCase);
    private readonly ILogger<AssemblyLoadContextService> _log;
    private readonly SemaphoreSlim _gate = new(1, 1);
    private bool _disposed;

    public AssemblyLoadContextService(ILogger<AssemblyLoadContextService> log) { _log = log; }

    public Task<AssemblyLoadContextGeneration> LoadGenerationAsync(AssemblyShadowCopyGeneration shadowCopyGeneration, CancellationToken ct)
    {
        var simpleNameMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var file in shadowCopyGeneration.files)
        {
            ct.ThrowIfCancellationRequested();
            var simpleName = Path.GetFileNameWithoutExtension(file.copied_assembly_path);
            simpleNameMap.TryAdd(simpleName, file.copied_assembly_path);
        }

        var context = new GenerationLoadContext(shadowCopyGeneration.generation_id, simpleNameMap);
        var assembliesByKey = new Dictionary<string, Assembly>(StringComparer.OrdinalIgnoreCase);

        foreach (var file in shadowCopyGeneration.files)
        {
            ct.ThrowIfCancellationRequested();
            var assembly = context.LoadFromAssemblyPath(file.copied_assembly_path);
            assembliesByKey[$"{file.app_id}|{file.source_assembly_path}"] = assembly;
        }

        _generations[shadowCopyGeneration.generation_id] = new GenerationState(context);

        return Task.FromResult(new AssemblyLoadContextGeneration(
            shadowCopyGeneration.generation_id,
            assembliesByKey));
    }

    public async Task UnloadGenerationAsync(string generationId, CancellationToken ct)
    {
        if (!_generations.TryRemove(generationId, out var state)) return;
        await UnloadStateAsync(state, ct);
    }

    private async Task UnloadStateAsync(GenerationState state, CancellationToken ct)
    {
        try
        {
            state.Context.Unload();

            for (var i = 0; i < 3; i++)
            {
                ct.ThrowIfCancellationRequested();
                GC.Collect();
                GC.WaitForPendingFinalizers();
                await Task.Delay(50, ct);
            }
        }
        catch (Exception ex)
        {
            _log.LogWarning(ex, "Could not unload generation context.");
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_disposed) return;
        _disposed = true;

        await _gate.WaitAsync(CancellationToken.None);
        try
        {
            var all = _generations.ToArray();
            _generations.Clear();

            foreach (var item in all)
            {
                await UnloadStateAsync(item.Value, CancellationToken.None);
            }
        }
        finally
        {
            _gate.Release();
            _gate.Dispose();
        }

        GC.SuppressFinalize(this);
    }
}