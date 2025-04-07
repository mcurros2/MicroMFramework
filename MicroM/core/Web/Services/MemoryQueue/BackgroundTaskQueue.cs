using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;
using static MicroM.Extensions.TimeExtensions;

namespace MicroM.Web.Services
{

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

    public interface IBackgroundTaskQueue
    {
        public Guid Enqueue(string TaskName, Func<CancellationToken, Task<string>> workItem, bool singleInstance, TimeSpan? recurrence = null);
        public QueueStatusInfo GetQueueStatus();
        public IDictionary<Guid, TaskStatusInfo> GetTasksStatus();
        public TaskStatusInfo GetTaskStatus(Guid taskId);
        public void CancelTask(Guid taskId);
        public CancellationToken QueueCT { get; set; }
        public void CancelAllTasks();
    }

    public class QueueItem
    {
        public Guid TaskID { get; init; }
        public string Name { get; init; } = null!;
        public Func<CancellationToken, Task<string>> WorkItem { get; init; } = null!;
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
        private readonly SemaphoreSlim _maxConcurrency = new(maxConcurrency, maxConcurrency);
        private readonly ConcurrentDictionary<Guid, QueueItem> _statuses = new();
        private int _runningCount = 0;
        private readonly object _lock_queue = new();
        private bool _isProcessing = false;
        private bool disposedValue;

        public CancellationToken QueueCT { get { return queueCT; } set { queueCT = value; } }

        /// <summary>
        /// Queues a task. TaskName string is used to identify if there are two instances of the same function running when singleInstance is true
        /// </summary>
        /// <param name="FunctionID"></param>
        /// <param name="workItem"></param>
        /// <param name="singleInstance"></param>
        /// <returns></returns>
        public Guid Enqueue(string TaskName, Func<CancellationToken, Task<string>> workItem, bool singleInstance, TimeSpan? recurrenceInterval = null)
        {
            lock (_lock_queue)
            {
                if (singleInstance)
                {
                    foreach (var existingItem in _statuses.Values)
                    {
                        if (existingItem.Name == TaskName &&
                            (existingItem.TaskStatus.Status == QueueTaskStatus.Queued ||
                             existingItem.TaskStatus.Status == QueueTaskStatus.Running))
                        {
                            logger.LogWarning("A single instance Task with name {ID} is already queued or running.", existingItem.Name);
                            return Guid.Empty;
                        }
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

                _workItems.Enqueue(item);
                _statuses[item.TaskID] = item;

                if (!_isProcessing)
                {
                    _isProcessing = true;
                    Task.Run(() => ProcessQueueAsync(), queueCT);
                }

                TrimOldestStatus();

                logger.LogInformation("Task {name} with ID {id} has been enqueued. Tasks Queued: {queued} Tasks running {running}", item.Name, item.TaskID, _workItems.Count, _runningCount);
                return item.TaskID;
            }

        }

        private readonly object _lock_status = new();

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
            while (true)
            {
                if (queueCT.IsCancellationRequested)
                {
                    logger.LogInformation("Queue processing cancelled.");
                    break;
                }

                if (_workItems.TryDequeue(out var item))
                {
                    lock (_lock_status)
                    {
                        bool isAlreadyRunning = _statuses.Values.Any(q =>
                            q.Name == item.Name && q.TaskStatus.Status == QueueTaskStatus.Running);

                        if (isAlreadyRunning)
                        {
                            logger.LogWarning("Task {name} with ID {id} is already running. Skipping.", item.Name, item.TaskID);
                            continue;
                        }
                    }

                    await _maxConcurrency.WaitAsync(queueCT);

                    if (!_statuses.TryGetValue(item.TaskID, out var queuedItem))
                    {
                        _maxConcurrency.Release();
                        logger.LogWarning("Task {name} with ID {id} has no status found, can't be processed", item.Name, item.TaskID);
                        continue;
                    }

                    // Update the TaskStatusInfo within the QueueItem.
                    queuedItem.TaskStatus.Status = QueueTaskStatus.Running;
                    queuedItem.TaskStatus.Started = DateTime.Now;

                    Interlocked.Increment(ref _runningCount);

                    item.CTS = CancellationTokenSource.CreateLinkedTokenSource(queueCT);

                    logger.LogInformation("Task {name} with ID {id} changed status to Running", item.Name, item.TaskID);

                    try
                    {
                        queuedItem.TaskStatus.StatusMessage = await item.WorkItem(item.CTS.Token);
                        queuedItem.TaskStatus.Status = QueueTaskStatus.Completed;
                        queuedItem.TaskStatus.Finished = DateTime.Now;
                        var duration = queuedItem.TaskStatus.Finished - queuedItem.TaskStatus.Started;
                        var waited = queuedItem.TaskStatus.Started - queuedItem.TaskStatus.Queued;
                        logger.LogInformation("Task {name} with ID {id} changed status to Completed. Waited: {waited} Duration {duration}, result {result}", item.Name, item.TaskID, waited?.ToHumanDuration(), duration?.ToHumanDuration(), item.TaskStatus.StatusMessage);

                        // Recurrent task
                        if (item.RecurrenceInterval.HasValue)
                        {
                            await Task.Delay(item.RecurrenceInterval.Value, queueCT);
                            logger.LogInformation("Task {name} re-queued by recurring interval every {recurrence}.", item.Name, item.RecurrenceInterval?.ToHumanDuration());
                            Enqueue(item.Name, item.WorkItem, item.SingleInstance, recurrenceInterval: item.RecurrenceInterval);
                        }
                    }
                    catch (OperationCanceledException)
                    {
                        queuedItem.TaskStatus.Status = QueueTaskStatus.Cancelled;
                        queuedItem.TaskStatus.StatusMessage = $"Task {item.TaskID} was cancelled.";
                        queuedItem.TaskStatus.Finished = DateTime.Now;
                        logger.LogInformation("Task {name} with ID {id} was cancelled", item.Name, item.TaskID);
                    }
                    catch (Exception ex)
                    {
                        queuedItem.TaskStatus.Status = QueueTaskStatus.Failed;
                        queuedItem.TaskStatus.StatusMessage = $"Task {item.TaskID} Error: {ex.Message}";
                        queuedItem.TaskStatus.Finished = DateTime.Now;
                        logger.LogError("ERROR: Task {name} with ID {id}\n. Error {}", item.Name, item.TaskID, ex.ToString());
                    }
                    finally
                    {
                        Interlocked.Decrement(ref _runningCount);
                        item.CTS.Dispose();
                        MaxConcurrencyRelease();
                    }

                }
                else
                {
                    lock (_lock_queue)
                    {
                        // Check again in case new items have been added
                        // since the last TryDequeue call.
                        if (_workItems.IsEmpty)
                        {
                            _isProcessing = false;
                            logger.LogInformation("Finished processing queue");
                            break;
                        }

                    }
                }
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
                    item.CTS.Cancel();
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
            if (_statuses.Count > maxRetainedStatuses)
            {
                var oldestKey = _statuses.Keys.OrderBy(key => _statuses[key].TaskStatus.Queued).FirstOrDefault();
                logger.LogInformation("Trimming old status. Oldest key {key}", oldestKey);
                _statuses.TryRemove(oldestKey, out _);
            }
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    _maxConcurrency.Dispose();
                }

                // TODO: free unmanaged resources (unmanaged objects) and override finalizer
                // TODO: set large fields to null
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


}
