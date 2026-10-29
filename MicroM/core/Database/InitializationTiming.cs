using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace MicroM.Database;

internal readonly struct InitializationTiming(ILogger log, string stage, string? appId = null) : IDisposable
{
    private readonly Stopwatch timer = Stopwatch.StartNew();
    public void Dispose() => log.LogInformation("Initialization stage {Stage} for {AppID}: {ElapsedMs} ms", stage, appId, timer.ElapsedMilliseconds);
}
