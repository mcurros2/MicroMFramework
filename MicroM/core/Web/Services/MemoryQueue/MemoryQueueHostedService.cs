using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace MicroM.Web.Services
{
    /// <summary>
    /// Represents the IMemoryQueueHostedService.
    /// </summary>
    public interface IMemoryQueueHostedService : IHostedService, IBackgroundTaskQueue { }


    /// <summary>
    /// Represents the MemoryQueueHostedService.
    /// </summary>
    public class MemoryQueueHostedService : IMemoryQueueHostedService, IDisposable
    {
        /// <summary>
        /// _taskQueue; member.
        /// </summary>
        private readonly BackgroundTaskQueue _taskQueue;
        /// <summary>
        /// Performs the new operation.
        /// </summary>
        private CancellationTokenSource _queueCts = new();

        /// <summary>
        /// _logger; member.
        /// </summary>
        private readonly ILogger<MemoryQueueHostedService> _logger;
        /// <summary>
        /// disposedValue; member.
        /// </summary>
        private bool disposedValue;

        /// <summary>
        /// _taskQueue.QueueCT; field.
        /// </summary>
        public CancellationToken QueueCT => _taskQueue.QueueCT;

        /// <summary>
        /// Performs the MemoryQueueHostedService operation.
        /// </summary>
        public MemoryQueueHostedService(ILogger<MemoryQueueHostedService> logger, ILogger<BackgroundTaskQueue> queueLogger)
        {
            /// <summary>
            /// logger; member.
            /// </summary>
            _logger = logger;
            /// <summary>
            /// Performs the BackgroundTaskQueue operation.
            /// </summary>
            _taskQueue = new BackgroundTaskQueue(maxConcurrency: 50, maxRetainedStatuses: 1000, queueLogger, _queueCts.Token);
        }

        /// <summary>
        /// Performs the StartAsync operation.
        /// </summary>
        public Task StartAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("MemoryQueueHostedService is starting.");
            cancellationToken.Register(() => _queueCts.Cancel());
            return Task.CompletedTask;
        }

        /// <summary>
        /// Performs the StopAsync operation.
        /// </summary>
        public Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("MemoryQueueHostedService is stopping.");
            // cancel all running tasks
            _queueCts.Cancel();
            _taskQueue.CancelAllTasks();

            return Task.CompletedTask;
        }

        /// <summary>
        /// Performs the Enqueue operation.
        /// </summary>
        public Guid Enqueue(string TaskName, Func<CancellationToken, Task<string>> workItem, bool singleInstance, TimeSpan? recurrenceInterval = null)
        {
            try
            {
                return _taskQueue.Enqueue(TaskName, workItem, singleInstance, recurrenceInterval);
            }
            catch (Exception ex)
            {
                _logger.LogError("MemoryQueueHostedService: Error queuing task {name}.\n{ex}", TaskName, ex.ToString());
            }
            return Guid.Empty;
        }

        /// <summary>
        /// Performs the CancelTask operation.
        /// </summary>
        public void CancelTask(Guid taskId)
        {
            _taskQueue.CancelTask(taskId);
        }

        /// <summary>
        /// Performs the GetQueueStatus operation.
        /// </summary>
        public QueueStatusInfo GetQueueStatus()
        {
            return _taskQueue.GetQueueStatus();
        }

        /// <summary>
        /// Performs the GetTasksStatus operation.
        /// </summary>
        public IDictionary<Guid, TaskStatusInfo> GetTasksStatus()
        {
            return _taskQueue.GetTasksStatus();
        }

        /// <summary>
        /// Performs the GetTaskStatus operation.
        /// </summary>
        public TaskStatusInfo GetTaskStatus(Guid taskId)
        {
            return _taskQueue.GetTaskStatus(taskId);
        }

        /// <summary>
        /// Performs the CancelAllTasks operation.
        /// </summary>
        public void CancelAllTasks()
        {
            _taskQueue.CancelAllTasks();
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    _queueCts.Cancel();
                    _queueCts.Dispose();
                    _taskQueue.Dispose();
                }

                // TODO: free unmanaged resources (unmanaged objects) and override finalizer
                // TODO: set large fields to null
                disposedValue = true;
            }
        }

        /// <summary>
        /// Performs the Dispose operation.
        /// </summary>
        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }



}
