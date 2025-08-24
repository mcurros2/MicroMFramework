using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;
using static MicroM.Extensions.TimeExtensions;

namespace MicroM.Web.Services
{

    /// <summary>
    /// Represents the QueueTaskStatus.
    /// </summary>
    public enum QueueTaskStatus
    {
        NotFound,
        Queued,
        Running,
        Completed,
        Failed,
        Cancelled
    }

    /// <summary>
    /// Represents the TaskStatusInfo.
    /// </summary>
    public class TaskStatusInfo
    {
        /// <summary>
        /// Gets or sets the current status of the task within the queue.
        /// </summary>
        public QueueTaskStatus Status { get; set; }
        /// <summary>
        /// Gets or sets an optional human-readable message describing the task status.
        /// </summary>
        public string? StatusMessage { get; set; }
        /// <summary>
        /// Gets or sets the time at which the task was queued.
        /// </summary>
        public DateTime Queued { get; set; }
        /// <summary>
        /// Gets or sets the time the task began execution, if it has started.
        /// </summary>
        public DateTime? Started { get; set; }
        /// <summary>
        /// Gets or sets the time the task finished processing, if it has completed.
        /// </summary>
        public DateTime? Finished { get; set; }
    }

    /// <summary>
    /// Represents the QueueStatusInfo.
    /// </summary>
    public class QueueStatusInfo
    {
        /// <summary>
        /// Gets or sets the number of tasks currently waiting in the queue.
        /// </summary>
        public int QueuedCount { get; set; }
        /// <summary>
        /// Gets or sets the number of tasks that are presently executing.
        /// </summary>
        public int RunningCount { get; set; }
    }

    /// <summary>
    /// Represents the IBackgroundTaskQueue.
    /// </summary>
    public interface IBackgroundTaskQueue
    {
        /// <summary>
        /// Adds a new work item to the background queue.
        /// </summary>
        /// <param name="TaskName">Friendly name used to identify the task.</param>
        /// <param name="workItem">The asynchronous operation to execute.</param>
        /// <param name="singleInstance">When <c>true</c>, prevents queuing another task with the same name while one is running or queued.</param>
        /// <param name="recurrence">Optional interval to automatically requeue the task after completion.</param>
        /// <returns>The unique identifier assigned to the enqueued task, or <see cref="Guid.Empty"/> if rejected.</returns>
        public Guid Enqueue(string TaskName, Func<CancellationToken, Task<string>> workItem, bool singleInstance, TimeSpan? recurrence = null);
        /// <summary>
        /// Retrieves the current counts of queued and running tasks.
        /// </summary>
        /// <returns>A <see cref="QueueStatusInfo"/> describing the queue.</returns>
        public QueueStatusInfo GetQueueStatus();
        /// <summary>
        /// Retrieves status information for all tasks.
        /// </summary>
        /// <returns>A dictionary mapping task identifiers to their status information.</returns>
        public IDictionary<Guid, TaskStatusInfo> GetTasksStatus();
        /// <summary>
        /// Retrieves status information for a specific task.
        /// </summary>
        /// <param name="taskId">The identifier of the task whose status is requested.</param>
        /// <returns>A <see cref="TaskStatusInfo"/> representing the task status.</returns>
        public TaskStatusInfo GetTaskStatus(Guid taskId);
        /// <summary>
        /// Cancels a queued or running task by its identifier.
        /// </summary>
        /// <param name="taskId">The identifier of the task to cancel.</param>
        public void CancelTask(Guid taskId);
        /// <summary>
        /// Gets the token used to signal cancellation of queue processing.
        /// </summary>
        public CancellationToken QueueCT { get; }
        /// <summary>
        /// Cancels all queued and running tasks.
        /// </summary>
        public void CancelAllTasks();
    }

    /// <summary>
    /// Represents the QueueItem.
    /// </summary>
    public class QueueItem
    {
        /// <summary>
        /// Gets the unique identifier of the task.
        /// </summary>
        public Guid TaskID { get; init; }
        /// <summary>
        /// Gets the descriptive name of the task.
        /// </summary>
        public string Name { get; init; } = null!;
        /// <summary>
        /// Gets the work item delegate to execute for this task.
        /// </summary>
        public Func<CancellationToken, Task<string>> WorkItem { get; init; } = null!;
        /// <summary>
        /// Gets or sets the cancellation token source linked to this task.
        /// </summary>
        public CancellationTokenSource? CTS { get; set; }
        /// <summary>
        /// Gets or sets the current status information for the task.
        /// </summary>
        public TaskStatusInfo TaskStatus { get; set; } = null!;
        /// <summary>
        /// Gets or sets the interval used to requeue the task after completion, if any.
        /// </summary>
        public TimeSpan? RecurrenceInterval { get; set; }
        /// <summary>
        /// Gets or sets a value indicating whether only one instance of the task may run at a time.
        /// </summary>
        public bool SingleInstance { get; set; } = false;
    }

    /// <summary>
    /// Represents the BackgroundTaskQueue.
    /// </summary>
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
        private readonly object _lock_queue = new();
        private bool _isProcessing = false;
        private bool disposedValue;


        /// <summary>
        /// Gets the <see cref="CancellationToken"/> that signals when the queue should stop processing.
        /// </summary>
        public CancellationToken QueueCT => queueCT;

        /// <summary>
        /// Enqueues a new task for background processing.
        /// </summary>
        /// <param name="TaskName">Friendly name used to identify the task.</param>
        /// <param name="workItem">The asynchronous operation to execute.</param>
        /// <param name="singleInstance">When <c>true</c>, prevents the task from running concurrently with another task of the same name.</param>
        /// <param name="recurrenceInterval">Optional interval to automatically requeue the task after it completes.</param>
        /// <returns>The unique identifier assigned to the enqueued task, or <see cref="Guid.Empty"/> if rejected.</returns>
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

            if (!_isProcessing)
            {
                _isProcessing = true;
                Task.Run(() => ProcessQueueAsync(), queueCT);
            }

            TrimOldestStatus();

            logger.LogInformation("Task {name} with ID {id} has been enqueued. Tasks Queued: {queued} Tasks running {running}", item.Name, item.TaskID, _workItems.Count, _runningCount);
            return item.TaskID;
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
            var runningTasks = new List<Task>();

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

                        var task = Task.Run(async () =>
                        {
                            try
                            {
                                queuedItem.TaskStatus.StatusMessage = await item.WorkItem(item.CTS.Token);
                                queuedItem.TaskStatus.Status = QueueTaskStatus.Completed;
                                queuedItem.TaskStatus.Finished = DateTime.Now;
                                var duration = queuedItem.TaskStatus.Finished - queuedItem.TaskStatus.Started;
                                var waited = queuedItem.TaskStatus.Started - queuedItem.TaskStatus.Queued;
                                logger.LogInformation("Task {name} with ID {id} changed status to Completed. Waited: {waited} Duration {duration}, result {result}", item.Name, item.TaskID, waited?.ToHumanDuration(), duration?.ToHumanDuration(), item.TaskStatus.StatusMessage);

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
                                item.CTS?.Dispose();
                                MaxConcurrencyRelease();

                                if (item.SingleInstance)
                                    _singleInstanceNames.TryRemove(item.Name, out _);
                            }
                        }, queueCT);

                        runningTasks.Add(task);

                        // If we've reached maxConcurrency, wait for any to finish before starting more
                        if (runningTasks.Count >= _maxConcurrency.CurrentCount)
                        {
                            var finished = await Task.WhenAny(runningTasks);
                            runningTasks.Remove(finished);
                        }
                    }

                }
            }
            finally
            {
                _isProcessing = false;
                logger.LogInformation("Finished processing queue");
            }
        }

        /// <summary>
        /// Cancels a queued or running task.
        /// </summary>
        /// <param name="taskId">Identifier of the task to cancel.</param>
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
                    item.CTS.Dispose();
                    item.CTS = null;
                }
            }
            else
            {
                logger.LogWarning("TaskID ID {taskID} not cancelled. Reason: Not found in statuses.", taskId);
            }
        }

        /// <summary>
        /// Cancels all tasks currently queued or running.
        /// </summary>
        public void CancelAllTasks()
        {
            foreach (var item in _statuses.Values)
            {
                CancelTask(item.TaskID);
            }
        }

        /// <summary>
        /// Retrieves the counts of queued and running tasks.
        /// </summary>
        /// <returns>A <see cref="QueueStatusInfo"/> representing queue statistics.</returns>
        public QueueStatusInfo GetQueueStatus()
        {
            return new QueueStatusInfo
            {
                QueuedCount = _workItems.Count,
                RunningCount = _runningCount
            };
        }

        /// <summary>
        /// Retrieves status information for all tasks in the queue.
        /// </summary>
        /// <returns>A dictionary mapping each task identifier to its <see cref="TaskStatusInfo"/>.</returns>
        public IDictionary<Guid, TaskStatusInfo> GetTasksStatus()
        {
            var dict = new Dictionary<Guid, TaskStatusInfo>();
            foreach (var keyValuePair in _statuses)
            {
                dict[keyValuePair.Key] = keyValuePair.Value.TaskStatus ?? new TaskStatusInfo { Status = QueueTaskStatus.NotFound };
            }
            return dict;
        }

        /// <summary>
        /// Retrieves status information for a specific task.
        /// </summary>
        /// <param name="taskId">The identifier of the task to query.</param>
        /// <returns>The <see cref="TaskStatusInfo"/> for the requested task.</returns>
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
                    _signal.Dispose();
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

        /// <summary>
        /// Releases all resources used by the <see cref="BackgroundTaskQueue"/>.
        /// </summary>
        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }


}
