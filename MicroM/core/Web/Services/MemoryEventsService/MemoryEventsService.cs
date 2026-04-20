using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;

namespace MicroM.Web.Services;

public class MemoryEventsService(ILogger<MemoryEventsService> log) : IMemoryEventsService
{
    private readonly ConcurrentDictionary<Type, List<Delegate>> _subscribers = [];

    // Tracks currently running event types to detect cycles in the same async context
    private readonly AsyncLocal<HashSet<Type>> _running = new();

    public void Publish<TEvent>(TEvent @event) where TEvent : class
    {
        var eventType = typeof(TEvent);
        if (_subscribers.TryGetValue(eventType, out var handlers))
        {
            var running = _running.Value ??= [];
            if (!running.Add(eventType))
            {
                log.LogError("Detected possible event bus cycle for event type {EventType}", eventType.FullName);
                return;
            }

            try
            {
                Delegate[] snapshot;
                lock (handlers)
                {
                    snapshot = [.. handlers];
                }

                foreach (var handler in snapshot.Cast<Action<TEvent>>())
                {
                    try
                    {
                        handler(@event);
                    }
                    catch (Exception ex)
                    {
                        log.LogError(ex, "Error while handling event of type {EventType}", eventType.FullName);
                    }
                }

            }
            catch (Exception ex)
            {
                log.LogError(ex, "Error while publishing event of type {EventType}", eventType.FullName);
            }
            finally
            {
                running.Remove(eventType);
            }
        }
    }

    public void Subscribe<TEvent>(Action<TEvent> handler) where TEvent : class
    {
        var eventType = typeof(TEvent);
        var list = _subscribers.GetOrAdd(eventType, _ => []);

        lock (list)
        {
            list.Add(handler);
        }
    }

    public void Unsubscribe<TEvent>(Action<TEvent> handler) where TEvent : class
    {
        var eventType = typeof(TEvent);
        if (_subscribers.TryGetValue(eventType, out var handlers))
        {
            lock (handlers)
            {
                handlers.Remove(handler);
                if (handlers.Count == 0)
                {
                    _subscribers.TryRemove(eventType, out _);
                }
            }
        }
    }
}
