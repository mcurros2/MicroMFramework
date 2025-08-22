using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace MicroM.Web.Services
{
    public interface IMemoryQueueHostedService : IHostedService, IBackgroundTaskQueue { }


    public class MemoryQueueHostedService : IMemoryQueueHostedService, IDisposable
    {
        private readonly BackgroundTaskQueue _taskQueue;
        private CancellationTokenSource _queueCts = new();

        private readonly ILogger<MemoryQueueHostedService> _logger;
        private bool disposedValue;

        public CancellationToken QueueCT => _taskQueue.QueueCT;

        public MemoryQueueHostedService(ILogger<MemoryQueueHostedService> logger, ILogger<BackgroundTaskQueue> queueLogger)
        {
            _logger = logger;
            _taskQueue = new BackgroundTaskQueue(maxConcurrency: 50, maxRetainedStatuses: 1000, queueLogger, _queueCts.Token);
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("MemoryQueueHostedService is starting.");
            cancellationToken.Register(() => _queueCts.Cancel());
            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("MemoryQueueHostedService is stopping.");
            // cancel all running tasks
            _queueCts.Cancel();
            _taskQueue.CancelAllTasks();

            return Task.CompletedTask;
        }

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

        public void CancelTask(Guid taskId)
        {
            _taskQueue.CancelTask(taskId);
        }

        public QueueStatusInfo GetQueueStatus()
        {
            return _taskQueue.GetQueueStatus();
        }

        public IDictionary<Guid, TaskStatusInfo> GetTasksStatus()
        {
            return _taskQueue.GetTasksStatus();
        }

        public TaskStatusInfo GetTaskStatus(Guid taskId)
        {
            return _taskQueue.GetTaskStatus(taskId);
        }

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

        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }



}
