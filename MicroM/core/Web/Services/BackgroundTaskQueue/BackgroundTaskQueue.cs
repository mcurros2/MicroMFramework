using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;
using static MicroM.Extensions.TimeExtensions;

namespace MicroM.Web.Services;

public enum QueueTaskStatus
{
    NotFound,
    Queued,
    Running,
    Completed,
    Failed,
    Cancelled
}

public class TaskStatusInfo
{
    public QueueTaskStatus Status { get; set; }
    public string? StatusMessage { get; set; }
    public DateTime Queued { get; set; }
    public DateTime? Started { get; set; }
    public DateTime? Finished { get; set; }
}

public class QueueStatusInfo
{
    public int QueuedCount { get; set; }
    public int RunningCount { get; set; }
}

public class QueueItem
{
    public Guid TaskID { get; init; }
    public string Name { get; init; } = null!;
    public Func<CancellationToken, Task<string>>? WorkItem { get; set; } // changed
    public CancellationTokenSource? CTS { get; set; }
    public TaskStatusInfo TaskStatus { get; set; } = null!;
    public TimeSpan? RecurrenceInterval { get; set; }
    public bool SingleInstance { get; set; } = false;
}

public class BackgroundTaskQueue(
    int maxConcurrency, int maxRetainedStatuses, ILogger<BackgroundTaskQueue> logger, CancellationToken queueCT
    ) : IBackgroundTaskQueue, IDisposable
{
    private readonly ConcurrentQueue<QueueItem> _workItems = new();

    private readonly ConcurrentDictionary<Guid, QueueItem> _statuses = new();
    private readonly ConcurrentDictionary<string, Guid> _singleInstanceNames = new();

    private readonly SemaphoreSlim _maxConcurrency = new(maxConcurrency, maxConcurrency);
    private readonly SemaphoreSlim _signal = new(0);

    private int _runningCount = 0;
    private bool _isProcessing = false;
    private bool disposedValue;


    public CancellationToken QueueCT => queueCT;

    /// <summary>
    /// Queues a task. TaskName string is used to identify if there are two instances of the same function running when singleInstance is true
    /// </summary>
    public Guid Enqueue(string TaskName, Func<CancellationToken, Task<string>> workItem, bool singleInstance, TimeSpan? recurrenceInterval = null)
    {
        if (singleInstance)
        {
            if (!_singleInstanceNames.TryAdd(TaskName, Guid.Empty))
            {
                logger.LogWarning("A single instance Task with name {ID} is already queued or running.", TaskName);
                return Guid.Empty;
            }
        }

        QueueItem item = new()
        {
            TaskID = Guid.NewGuid(),
            WorkItem = workItem,
            Name = TaskName,
            TaskStatus = new TaskStatusInfo { Status = QueueTaskStatus.Queued, Queued = DateTime.Now },
            RecurrenceInterval = recurrenceInterval,
            SingleInstance = singleInstance
        };

        if (singleInstance)
        {
            _singleInstanceNames[TaskName] = item.TaskID;
        }

        _workItems.Enqueue(item);
        _statuses[item.TaskID] = item;
        _signal.Release();

        if (Interlocked.CompareExchange(ref _isProcessing, true, false) == false)
        {
            Task.Run(() => ProcessQueueAsync(), queueCT);
        }

        TrimOldestStatus();

        logger.LogInformation("Task {name} with ID {id} has been enqueued. Tasks Queued: {queued} Tasks running {running}", item.Name, item.TaskID, _workItems.Count, _runningCount);
        return item.TaskID;
    }

    private void MaxConcurrencyRelease()
    {
        try
        {
            _maxConcurrency.Release();
        }
        catch (SemaphoreFullException)
        {
            logger.LogWarning("Semaphore full. MaxConcurrency: {maxConcurrency}", _maxConcurrency.CurrentCount);
        }
    }

    private async Task ProcessQueueAsync()
    {
        logger.LogInformation("Initiated processing queue");

        try
        {
            while (true)
            {
                try
                {
                    await _signal.WaitAsync(queueCT);
                }
                catch (OperationCanceledException)
                {
                    logger.LogInformation("Queue processing cancelled.");
                    break;
                }

                while (_workItems.TryDequeue(out var item))
                {
                    await _maxConcurrency.WaitAsync(queueCT);

                    // Check if another instance is running (for singleInstance tasks)
                    if (item.SingleInstance)
                    {
                        if (_statuses.Values.Any(q =>
                            q.Name == item.Name && q.TaskStatus.Status == QueueTaskStatus.Running && q.TaskID != item.TaskID))
                        {
                            _maxConcurrency.Release();
                            logger.LogWarning("Task {name} with ID {id} is already running. Skipping.", item.Name, item.TaskID);
                            _singleInstanceNames.TryRemove(item.Name, out _);
                            continue;
                        }
                    }

                    if (!_statuses.TryGetValue(item.TaskID, out var queuedItem))
                    {
                        _maxConcurrency.Release();
                        logger.LogWarning("Task {name} with ID {id} has no status found, can't be processed", item.Name, item.TaskID);
                        continue;
                    }

                    queuedItem.TaskStatus.Status = QueueTaskStatus.Running;
                    queuedItem.TaskStatus.Started = DateTime.Now;

                    Interlocked.Increment(ref _runningCount);

                    item.CTS = CancellationTokenSource.CreateLinkedTokenSource(queueCT);

                    logger.LogInformation("Task {name} with ID {id} changed status to Running", item.Name, item.TaskID);

                    var workItem = item.WorkItem;
                    if (workItem == null)
                    {
                        queuedItem.TaskStatus.Status = QueueTaskStatus.Failed;
                        queuedItem.TaskStatus.StatusMessage = $"Task {item.TaskID} has no work item.";
                        queuedItem.TaskStatus.Finished = DateTime.Now;

                        Interlocked.Decrement(ref _runningCount);
                        ReleaseRetainedReferences(item);
                        MaxConcurrencyRelease();

                        if (item.SingleInstance)
                            _singleInstanceNames.TryRemove(item.Name, out _);

                        continue;
                    }

                    _ = Task.Run(async () =>
                    {
                        try
                        {
                            queuedItem.TaskStatus.StatusMessage = await workItem(item.CTS!.Token);
                            queuedItem.TaskStatus.Status = QueueTaskStatus.Completed;
                            queuedItem.TaskStatus.Finished = DateTime.Now;

                            var duration = queuedItem.TaskStatus.Finished - queuedItem.TaskStatus.Started;
                            var waited = queuedItem.TaskStatus.Started - queuedItem.TaskStatus.Queued;

                            logger.LogInformation("Task {name} with ID {id} changed status to Completed. Waited: {waited} Duration {duration}, result: {result}, recurrence: {recurrence}",
                                item.Name, item.TaskID, waited?.ToHumanDuration(), duration?.ToHumanDuration(), item.TaskStatus.StatusMessage, item.RecurrenceInterval?.ToHumanDuration());


                            Interlocked.Decrement(ref _runningCount);
                            ReleaseRetainedReferences(item);
                            MaxConcurrencyRelease();

                            if (item.SingleInstance)
                                _singleInstanceNames.TryRemove(item.Name, out _);

                            TrimOldestStatus();

                            // recurrence first (uses local workItem, not item.WorkItem)
                            if (item.RecurrenceInterval.HasValue)
                            {
                                try
                                {
                                    await Task.Delay(item.RecurrenceInterval.Value, queueCT);
                                    logger.LogInformation("Task {name} re-queued by recurring interval every {recurrence}.", item.Name, item.RecurrenceInterval?.ToHumanDuration());
                                    Enqueue(item.Name, workItem, item.SingleInstance, recurrenceInterval: item.RecurrenceInterval);
                                }
                                catch (OperationCanceledException)
                                {
                                    logger.LogInformation("Task {name} recurrence skipped due to queue cancellation.", item.Name);
                                }
                            }

                        }
                        catch (OperationCanceledException)
                        {
                            queuedItem.TaskStatus.Status = QueueTaskStatus.Cancelled;
                            queuedItem.TaskStatus.StatusMessage = $"Task {item.TaskID} was cancelled.";
                            queuedItem.TaskStatus.Finished = DateTime.Now;

                            Interlocked.Decrement(ref _runningCount);
                            ReleaseRetainedReferences(item);
                            MaxConcurrencyRelease();

                            if (item.SingleInstance)
                                _singleInstanceNames.TryRemove(item.Name, out _);

                            logger.LogInformation("Task {name} with ID {id} was cancelled", item.Name, item.TaskID);
                            TrimOldestStatus();
                        }
                        catch (Exception ex)
                        {
                            queuedItem.TaskStatus.Status = QueueTaskStatus.Failed;
                            queuedItem.TaskStatus.StatusMessage = $"Task {item.TaskID} Error: {ex.Message}";
                            queuedItem.TaskStatus.Finished = DateTime.Now;

                            Interlocked.Decrement(ref _runningCount);
                            ReleaseRetainedReferences(item);
                            MaxConcurrencyRelease();

                            if (item.SingleInstance)
                                _singleInstanceNames.TryRemove(item.Name, out _);

                            logger.LogError("ERROR: Task {name} with ID {id}\n. Error {}", item.Name, item.TaskID, ex.ToString());
                            TrimOldestStatus();
                        }
                    }, queueCT);


                }

            }
        }
        finally
        {
            Interlocked.Exchange(ref _isProcessing, false);
            logger.LogInformation("Finished processing queue");
        }
    }

    public void CancelTask(Guid taskId)
    {
        if (_statuses.TryGetValue(taskId, out var item))
        {
            logger.LogInformation("Cancelling Task {name} TaskID {taskID}", item.Name, item.TaskID);
            if (item.CTS == null)
            {
                if (item.TaskStatus != null && item.TaskStatus.Status == QueueTaskStatus.Running)
                {
                    logger.LogWarning("Failed to cancel TaskID {taskID}. CTS is null.", item.TaskID);
                }
            }
            else
            {
                item.CTS?.Cancel();
                item.CTS?.Dispose();
                item.CTS = null;
            }
        }
        else
        {
            logger.LogWarning("TaskID ID {taskID} not cancelled. Reason: Not found in statuses.", taskId);
        }
    }

    public void CancelAllTasks()
    {
        foreach (var item in _statuses.Values)
        {
            CancelTask(item.TaskID);
        }
    }

    public QueueStatusInfo GetQueueStatus()
    {
        return new QueueStatusInfo
        {
            QueuedCount = _workItems.Count,
            RunningCount = _runningCount
        };
    }

    public IDictionary<Guid, TaskStatusInfo> GetTasksStatus()
    {
        var dict = new Dictionary<Guid, TaskStatusInfo>();
        foreach (var keyValuePair in _statuses)
        {
            dict[keyValuePair.Key] = keyValuePair.Value.TaskStatus ?? new TaskStatusInfo { Status = QueueTaskStatus.NotFound };
        }
        return dict;
    }

    public TaskStatusInfo GetTaskStatus(Guid taskId)
    {
        if (_statuses.TryGetValue(taskId, out var item))
        {
            return item.TaskStatus ?? new TaskStatusInfo { Status = QueueTaskStatus.NotFound };
        }
        return new TaskStatusInfo { Status = QueueTaskStatus.NotFound };
    }

    private void TrimOldestStatus()
    {
        if (_statuses.Count <= maxRetainedStatuses) return;

        var terminal = _statuses
            .Where(kv => kv.Value.TaskStatus.Status is QueueTaskStatus.Completed
                                               or QueueTaskStatus.Failed
                                               or QueueTaskStatus.Cancelled)
            .OrderBy(kv => kv.Value.TaskStatus.Queued)
            .Select(kv => kv.Key)
            .FirstOrDefault();

        if (terminal != Guid.Empty && _statuses.TryRemove(terminal, out _))
        {
            logger.LogInformation("Trimming old terminal status. Oldest key {key}", terminal);
        }
    }

    private static void ReleaseRetainedReferences(QueueItem item)
    {
        item.CTS?.Dispose();
        item.CTS = null;
        item.WorkItem = null; // critical: release delegate closure
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!disposedValue)
        {
            if (disposing)
            {
                _maxConcurrency?.Dispose();
                _signal?.Dispose();
                foreach (var item in _statuses.Values)
                {
                    item.CTS?.Dispose();
                }
                _statuses.Clear();
                _workItems.Clear();
                _singleInstanceNames.Clear();
            }

            disposedValue = true;
        }
    }

    public void Dispose()
    {
        // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }
}
