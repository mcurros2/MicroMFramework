using Microsoft.Extensions.Logging;

namespace MicroM.Web.Services;

public interface IMicroMApplicationServices
{
    public Task InitiateStartupServices(IBackgroundTaskQueue queue, IMicroMAppConfiguration app_config, ILogger<MicroMAppConfigurationProvider> log);

    public Task StopStartupServices(IBackgroundTaskQueue queue, IMicroMAppConfiguration app_config, ILogger<MicroMAppConfigurationProvider> log);
}
