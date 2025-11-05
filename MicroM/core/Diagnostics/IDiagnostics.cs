namespace MicroM.Diagnostics;

public interface IDiagnostics<T>
{
    Task<Dictionary<string, List<DiagnosticResult>>> RunAllDiagnosticsAsync(T diagnostics_context, CancellationToken ct);
}
