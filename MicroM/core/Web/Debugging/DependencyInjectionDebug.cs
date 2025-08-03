using System.Collections.ObjectModel;
using System.Diagnostics.Tracing;

namespace MicroM.Web.Debug;

public sealed class DependencyInjectionDebug() : EventListener
{
    protected override void OnEventSourceCreated(EventSource src)
    {
        if (src.Name == "Microsoft-Extensions-DependencyInjection")
            EnableEvents(src, EventLevel.Verbose);
    }

    protected override void OnEventWritten(EventWrittenEventArgs e)
    {
        if (e.EventName == "EventCounters")
            return; // skip EventCounters events
        var msg = $"{e.EventName}: {string.Join(", ", e.Payload ?? new ReadOnlyCollection<object?>([]))}";
        Console.WriteLine("[DI] {0}", msg);
    }
}
