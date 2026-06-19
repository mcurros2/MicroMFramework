using System.Reflection;

namespace MicroM.Web.Services;

public sealed record PreparedAssemblyGeneration(string generation_id, IReadOnlyDictionary<string, Assembly> assemblies_by_key, IReadOnlyCollection<string> source_folders_to_watch);

public interface IAppAssemblyRuntimeManager
{
    Task<PreparedAssemblyGeneration> PrepareGenerationAsync(IReadOnlyCollection<AssemblyCopyRequest> assemblies, CancellationToken ct);

    Task CommitGenerationAsync(string generation_id, CancellationToken ct);

    Task RollbackGenerationAsync(string generation_id, CancellationToken ct);

    void EnableFileWatchers(string generation_id, Func<string, Task> onAssemblyChangedAsync);

    void DisableFileWatchers();
}