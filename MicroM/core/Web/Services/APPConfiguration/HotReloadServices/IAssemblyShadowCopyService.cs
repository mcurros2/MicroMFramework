namespace MicroM.Web.Services;

public sealed record AssemblyCopyRequest(string app_id, string source_assembly_path, string assembly_id);

public sealed record AssemblyCopiedFile(string app_id, string source_assembly_path, string copied_assembly_path);

public sealed record AssemblyShadowCopyGeneration(string generation_id, IReadOnlyList<AssemblyCopiedFile> files, IReadOnlyCollection<string> source_folders_to_watch);

public interface IAssemblyShadowCopyService
{
    Task<AssemblyShadowCopyGeneration> CopyForReloadAsync(IReadOnlyCollection<AssemblyCopyRequest> assemblies, CancellationToken ct);

    Task DeleteGenerationAsync(string generation_id, CancellationToken ct);

    Task DeleteAllGenerationsAsync(CancellationToken ct);

    Task DeleteAllGenerationsExceptAsync(string generation_id_to_keep, CancellationToken ct);
}