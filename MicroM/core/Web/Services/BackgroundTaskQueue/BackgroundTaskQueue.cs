using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;
using static MicroM.Extensions.TimeExtensions;

namespace MicroM.Web.Services;

public enum QueueTaskStatus
{
    NotFound,
    Queued,
    Waiting,
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
    public int WaitingCount { get; set; }
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
    public DateTime? StartOn { get; set; }
    public bool SingleInstance { get; set; } = false;
}

public class BackgroundTaskQueue(
    int maxConcurrency, int maxRetainedStatuses, ILogger<BackgroundTaskQueue> log, CancellationToken queueCT
    ) : IBackgroundTaskQueue, IDisposable
{
    private readonly ConcurrentQueue<QueueItem> _readyItems = new();
    private readonly ConcurrentDictionary<Guid, QueueItem> _delayedItems = new();

    private readonly ConcurrentDictionary<Guid, QueueItem> _statuses = new();
    private readonly ConcurrentDictionary<string, Guid> _singleInstanceNames = new();

    private readonly SemaphoreSlim _maxConcurrency = new(maxConcurrency, maxConcurrency);
    private readonly SemaphoreSlim _signal = new(0);

    private int _runningCount = 0;
    private int _isProcessing = 0;
    private bool disposedValue;

    private readonly SemaphoreSlim _delaySignal = new(0);
    private int _isDelayProcessing = 0;

    public CancellationToken QueueCT => queueCT;

    /// <summary>
    /// Queues a task. TaskName string is used to identify if there are two instances of the same function running when singleInstance is true
    /// </summary>
    public Guid Enqueue(string TaskName, Func<CancellationToken, Task<string>> workItem, bool singleInstance, TimeSpan? recurrenceInterval = null, TimeSpan? delayedStart = null)
    {
        var taskGUID = Guid.NewGuid();
        if (singleInstance)
        {
            if (!_singleInstanceNames.TryAdd(TaskName, taskGUID))
            {
                log.LogDebug("A single instance Task with name {ID} is already queued or running.", TaskName);
                return Guid.Empty;
            }
        }

        if (delayedStart.HasValue && delayedStart.Value < TimeSpan.Zero)
        {
            log.LogWarning("Delayed start value {delay} is invalid. It must be a positive TimeSpan. Defaulting to no delay.", delayedStart);
            delayedStart = null;
        }

        var now = DateTime.UtcNow;

        QueueItem item = new()
        {
            TaskID = taskGUID,
            WorkItem = workItem,
            Name = TaskName,
            TaskStatus = new TaskStatusInfo { Status = delayedStart.HasValue ? QueueTaskStatus.Waiting : QueueTaskStatus.Queued, Queued = now },
            RecurrenceInterval = recurrenceInterval,
            SingleInstance = singleInstance,
            StartOn = delayedStart.HasValue ? now + delayedStart.Value : null,
            CTS = CancellationTokenSource.CreateLinkedTokenSource(queueCT)
        };

        _statuses[item.TaskID] = item;

        if (delayedStart.HasValue)
        {
            _delayedItems[item.TaskID] = item;
            _delaySignal.Release();

            if (Interlocked.CompareExchange(ref _isDelayProcessing, 1, 0) == 0)
            {
                _ = Task.Run(() => ProcessDelayedItems(), queueCT);
            }
        }
        else
        {
            _readyItems.Enqueue(item);
            _signal.Release();

            if (Interlocked.CompareExchange(ref _isProcessing, 1, 0) == 0)
            {
                _ = Task.Run(() => ProcessQueueAsync(), queueCT);
            }
        }

        TrimOldestStatus();

        log.LogInformation("Task {name} with ID {id} has been enqueued. Tasks Queued: {queued} Tasks running {running}", item.Name, item.TaskID, _readyItems.Count, _runningCount);
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
            log.LogWarning("Semaphore full. MaxConcurrency: {maxConcurrency}", _maxConcurrency.CurrentCount);
        }
    }

    private void ReleaseSingleInstanceName(QueueItem item)
    {
        if (!item.SingleInstance) return;

        if (_singleInstanceNames.TryGetValue(item.Name, out var taskId) &&
            taskId == item.TaskID)
        {
            _singleInstanceNames.TryRemove(item.Name, out _);
        }
    }

    private async Task ProcessDelayedItems()
    {
        log.LogInformation("Initiated delayed task processing");

        try
        {
            while (!queueCT.IsCancellationRequested)
            {
                if (_delayedItems.IsEmpty)
                {
                    try
                    {
                        await _delaySignal.WaitAsync(queueCT);
                    }
                    catch (OperationCanceledException)
                    {
                        break;
                    }
                }

                while (!queueCT.IsCancellationRequested)
                {
                    var now = DateTime.UtcNow;

                    var dueItems = _delayedItems.Values
                        .Where(item => item.StartOn <= now)
                        .OrderBy(item => item.StartOn)
                        .ToList();

                    if (dueItems.Count > 0)
                    {
                        foreach (var item in dueItems)
                        {
                            if (!_delayedItems.TryRemove(item.TaskID, out var dueItem))
                            {
                                continue;
                            }

                            if (dueItem.CTS?.IsCancellationRequested == true)
                            {
                                dueItem.TaskStatus.Status = QueueTaskStatus.Cancelled;
                                dueItem.TaskStatus.StatusMessage = $"Task {dueItem.TaskID} was cancelled before start.";
                                dueItem.TaskStatus.Finished = DateTime.UtcNow;

                                ReleaseRetainedReferences(dueItem);

                                if (dueItem.SingleInstance)
                                {
                                    ReleaseSingleInstanceName(dueItem);
                                }

                                continue;
                            }

                            dueItem.TaskStatus.Status = QueueTaskStatus.Queued;
                            dueItem.StartOn = null;

                            _readyItems.Enqueue(dueItem);
                            _signal.Release();

                            log.LogInformation("Delayed task {name} with ID {id} is now queued.", dueItem.Name, dueItem.TaskID);
                        }

                        if (Interlocked.CompareExchange(ref _isProcessing, 1, 0) == 0)
                        {
                            _ = Task.Run(() => ProcessQueueAsync(), queueCT);
                        }

                        continue;
                    }

                    var nextStart = _delayedItems.Values
                        .Where(item => item.StartOn.HasValue)
                        .Select(item => item.StartOn!.Value)
                        .OrderBy(x => x)
                        .FirstOrDefault();

                    if (nextStart == default)
                    {
                        break;
                    }

                    var delay = nextStart - DateTime.UtcNow;
                    if (delay < TimeSpan.Zero)
                    {
                        delay = TimeSpan.Zero;
                    }

                    try
                    {
                        using var waitCts = CancellationTokenSource.CreateLinkedTokenSource(queueCT);

                        var delayTask = Task.Delay(delay, waitCts.Token);
                        var signalTask = _delaySignal.WaitAsync(waitCts.Token);

                        var completed = await Task.WhenAny(delayTask, signalTask);

                        waitCts.Cancel();

                        if (completed == signalTask)
                        {
                            await signalTask;
                        }
                    }
                    catch (OperationCanceledException) when (queueCT.IsCancellationRequested)
                    {
                        break;
                    }
                    catch (OperationCanceledException)
                    {
                        // Expected when cancelling the losing wait.
                    }

                }
            }
        }
        finally
        {
            Interlocked.Exchange(ref _isDelayProcessing, 0);

            if (!_delayedItems.IsEmpty && Interlocked.CompareExchange(ref _isDelayProcessing, 1, 0) == 0)
            {
                _ = Task.Run(() => ProcessDelayedItems(), queueCT);
            }

            log.LogInformation("Finished delayed task processing");
        }
    }

    private async Task ProcessQueueAsync()
    {
        log.LogInformation("Initiated processing queue");

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
                    log.LogInformation("Queue processing cancelled.");
                    break;
                }

                while (_readyItems.TryDequeue(out var item))
                {
                    if (!_statuses.TryGetValue(item.TaskID, out var queuedItem))
                    {
                        log.LogWarning("Task {name} with ID {id} has no status found, can't be processed", item.Name, item.TaskID);
                        continue;
                    }

                    if (item.TaskStatus.Status == QueueTaskStatus.Cancelled || item.CTS?.IsCancellationRequested == true)
                    {
                        log.LogDebug("Skipping cancelled task {name} with ID {id}.", item.Name, item.TaskID);
                        continue;
                    }

                    item.CTS ??= CancellationTokenSource.CreateLinkedTokenSource(queueCT);

                    await _maxConcurrency.WaitAsync(queueCT);

                    // Check if another instance is running (for singleInstance tasks)
                    if (item.SingleInstance)
                    {
                        if (_statuses.Values.Any(q =>
                            q.Name == item.Name && q.TaskStatus.Status == QueueTaskStatus.Running && q.TaskID != item.TaskID))
                        {
                            queuedItem.TaskStatus.Status = QueueTaskStatus.Failed;
                            queuedItem.TaskStatus.StatusMessage = $"Task {item.TaskID} skipped because another instance of '{item.Name}' is already running.";
                            queuedItem.TaskStatus.Finished = DateTime.UtcNow;

                            MaxConcurrencyRelease();
                            ReleaseRetainedReferences(item);
                            ReleaseSingleInstanceName(item);

                            log.LogError("Invariant violation: single-instance task {name} with ID {id} reached execution while another instance is already running.", item.Name, item.TaskID);

                            TrimOldestStatus();
                            continue;
                        }
                    }

                    queuedItem.TaskStatus.Status = QueueTaskStatus.Running;
                    queuedItem.TaskStatus.Started = DateTime.UtcNow;

                    Interlocked.Increment(ref _runningCount);

                    log.LogInformation("Task {name} with ID {id} changed status to Running", item.Name, item.TaskID);

                    var workItem = item.WorkItem;
                    if (workItem == null)
                    {
                        queuedItem.TaskStatus.Status = QueueTaskStatus.Failed;
                        queuedItem.TaskStatus.StatusMessage = $"Task {item.TaskID} has no work item.";
                        queuedItem.TaskStatus.Finished = DateTime.UtcNow;

                        Interlocked.Decrement(ref _runningCount);
                        ReleaseRetainedReferences(item);
                        MaxConcurrencyRelease();

                        if (item.SingleInstance) ReleaseSingleInstanceName(item);

                        continue;
                    }

                    _ = Task.Run(async () =>
                    {
                        try
                        {
                            queuedItem.TaskStatus.StatusMessage = await workItem(item.CTS!.Token);
                            queuedItem.TaskStatus.Status = QueueTaskStatus.Completed;
                            queuedItem.TaskStatus.Finished = DateTime.UtcNow;

                            var duration = queuedItem.TaskStatus.Finished - queuedItem.TaskStatus.Started;
                            var waited = queuedItem.TaskStatus.Started - queuedItem.TaskStatus.Queued;

                            Interlocked.Decrement(ref _runningCount);
                            ReleaseRetainedReferences(item);
                            MaxConcurrencyRelease();

                            TrimOldestStatus();

                            log.LogInformation("Task {name} with ID {id} changed status to Completed. Waited: {waited} Duration {duration}, result: {result}, recurrence: {recurrence}",
                                item.Name, item.TaskID, waited?.ToHumanDuration(), duration?.ToHumanDuration(), item.TaskStatus.StatusMessage, item.RecurrenceInterval?.ToHumanDuration());

                            if (item.SingleInstance) ReleaseSingleInstanceName(item);

                            if (item.RecurrenceInterval.HasValue)
                            {
                                log.LogInformation("Task {name} re-queued by recurring interval every {recurrence}.", item.Name, item.RecurrenceInterval?.ToHumanDuration());
                                Enqueue(item.Name, workItem, item.SingleInstance, recurrenceInterval: item.RecurrenceInterval, delayedStart: item.RecurrenceInterval);
                            }

                        }
                        catch (OperationCanceledException)
                        {
                            queuedItem.TaskStatus.Status = QueueTaskStatus.Cancelled;
                            queuedItem.TaskStatus.StatusMessage = $"Task {item.TaskID} was cancelled.";
                            queuedItem.TaskStatus.Finished = DateTime.UtcNow;

                            Interlocked.Decrement(ref _runningCount);
                            ReleaseRetainedReferences(item);
                            MaxConcurrencyRelease();

                            if (item.SingleInstance) ReleaseSingleInstanceName(item);

                            log.LogInformation("Task {name} with ID {id} was cancelled", item.Name, item.TaskID);
                            TrimOldestStatus();
                        }
                        catch (Exception ex)
                        {
                            queuedItem.TaskStatus.Status = QueueTaskStatus.Failed;
                            queuedItem.TaskStatus.StatusMessage = $"Task {item.TaskID} Error: {ex.Message}";
                            queuedItem.TaskStatus.Finished = DateTime.UtcNow;

                            Interlocked.Decrement(ref _runningCount);
                            ReleaseRetainedReferences(item);
                            MaxConcurrencyRelease();

                            if (item.SingleInstance) ReleaseSingleInstanceName(item);

                            log.LogError("ERROR: Task {name} with ID {id}\n. Error {}", item.Name, item.TaskID, ex.ToString());
                            TrimOldestStatus();
                        }
                    }, queueCT);


                }

            }
        }
        finally
        {
            Interlocked.Exchange(ref _isProcessing, 0);

            if (!_readyItems.IsEmpty && Interlocked.CompareExchange(ref _isProcessing, 1, 0) == 0)
            {
                _ = Task.Run(() => ProcessQueueAsync(), queueCT);
            }

            log.LogInformation("Finished processing queue");
        }
    }

    public void CancelTask(Guid taskId)
    {
        if (!_statuses.TryGetValue(taskId, out var item))
        {
            log.LogWarning("TaskID ID {taskID} not cancelled. Reason: Not found in statuses.", taskId);
            return;
        }

        log.LogInformation("Cancelling Task {name} TaskID {taskID}", item.Name, item.TaskID);
        item.CTS?.Cancel();

        if (item.TaskStatus.Status == QueueTaskStatus.Waiting)
        {
            _delayedItems.TryRemove(taskId, out _);

            item.TaskStatus.Status = QueueTaskStatus.Cancelled;
            item.TaskStatus.StatusMessage = $"Task {item.TaskID} was cancelled before start.";
            item.TaskStatus.Finished = DateTime.UtcNow;

            ReleaseRetainedReferences(item);

            if (item.SingleInstance) ReleaseSingleInstanceName(item);

            TrimOldestStatus();
            return;
        }

        if (item.TaskStatus.Status == QueueTaskStatus.Queued)
        {
            item.TaskStatus.Status = QueueTaskStatus.Cancelled;
            item.TaskStatus.StatusMessage = $"Task {item.TaskID} was cancelled before running.";
            item.TaskStatus.Finished = DateTime.UtcNow;

            ReleaseRetainedReferences(item);

            if (item.SingleInstance) ReleaseSingleInstanceName(item);

            TrimOldestStatus();
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
            QueuedCount = _statuses.Values.Count(x => x.TaskStatus.Status == QueueTaskStatus.Queued),
            WaitingCount = _statuses.Values.Count(x => x.TaskStatus.Status == QueueTaskStatus.Waiting),
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
            log.LogDebug("Trimming old terminal status. Oldest key {key}", terminal);
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
                _delaySignal?.Dispose();
                foreach (var item in _statuses.Values)
                {
                    item.CTS?.Dispose();
                }
                _statuses.Clear();
                _readyItems.Clear();
                _delayedItems.Clear();
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
