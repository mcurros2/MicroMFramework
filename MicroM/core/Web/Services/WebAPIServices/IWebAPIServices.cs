using MicroM.Web.Authentication;
using MicroM.Web.Services.Security;
using Microsoft.Extensions.Logging;

namespace MicroM.Web.Services;

public interface IWebAPIServices
{
    public ILogger log { get; }
    public IMicroMEncryption encryptor { get; }
    public IMicroMAppConfiguration app_config { get; }
    public IBackgroundTaskQueue backgroundTaskQueue { get; }
    public IFileUploadService upload { get; }
    public IEmailService emailService { get; }
    public ISecurityService securityService { get; }
    public IDeviceIdService deviceIdService { get; }
    public IEntitiesService entitiesService { get; }
    public IAuthenticationService authenticationService { get; }
}
