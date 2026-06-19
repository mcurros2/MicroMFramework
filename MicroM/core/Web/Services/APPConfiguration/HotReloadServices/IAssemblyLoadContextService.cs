using System.Reflection;

namespace MicroM.Web.Services;

public sealed record AssemblyLoadContextGeneration(string GenerationId, IReadOnlyDictionary<string, Assembly> AssembliesByKey);

public interface IAssemblyLoadContextService
{
    Task<AssemblyLoadContextGeneration> LoadGenerationAsync(AssemblyShadowCopyGeneration shadowCopyGeneration, CancellationToken ct);

    Task UnloadGenerationAsync(string generationId, CancellationToken ct);
}