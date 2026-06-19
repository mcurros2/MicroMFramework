namespace MicroM.Web.Services;

public interface IBackgroundTaskQueue
{
    public Guid Enqueue(string TaskName, Func<CancellationToken, Task<string>> workItem, bool singleInstance, TimeSpan? recurrence = null, TimeSpan? delayedStart = null);
    public QueueStatusInfo GetQueueStatus();
    public IDictionary<Guid, TaskStatusInfo> GetTasksStatus();
    public TaskStatusInfo GetTaskStatus(Guid taskId);
    public void CancelTask(Guid taskId);
    public CancellationToken QueueCT { get; }
    public void CancelAllTasks();
}
