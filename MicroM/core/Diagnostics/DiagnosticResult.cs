using MicroM.Core;

namespace MicroM.Diagnostics;

public record DiagnosticResult(
    string TestName,
    bool IsSuccess = false,
    string? Result = null,
    List<ErrorResult>? Errors = null
);
