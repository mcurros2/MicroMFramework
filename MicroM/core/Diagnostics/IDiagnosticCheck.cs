namespace MicroM.Diagnostics;

public interface IDiagnosticCheck<T>
{
    string DiagnosticId { get; }
    Task<List<DiagnosticResult>> RunCheckAsync(T diagnostics_context, CancellationToken ct);
}
