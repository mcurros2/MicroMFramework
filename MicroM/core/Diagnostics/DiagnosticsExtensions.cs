namespace MicroM.Diagnostics;

public static class DiagnosticsExtensions
{
    public static bool isSuccess(this List<DiagnosticResult>? result)
    {
        if (result == null) return false;
        foreach (var res in result)
        {
            if (!res.IsSuccess) return false;
        }
        return true;
    }
}
