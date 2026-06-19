namespace MicroM.Web.Services;

public interface IAssemblyFileWatcherService : IDisposable
{
    void ResetWatchers(IEnumerable<string> sourceAssemblyPaths, Func<string, Task> onAssemblyChangedAsync);

    void StopAll();
}