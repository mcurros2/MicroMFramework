using MicroM.Web.Authentication;
using MicroM.Web.Services.Security;
using Microsoft.Extensions.Logging;

namespace MicroM.Web.Services;

public class WebAPIServices(
    ILogger<WebAPIServices> log_service,
    IMicroMEncryption encryptor_service,
    IMicroMAppConfiguration app_config_service,
    IBackgroundTaskQueue backgroundTaskQueue_service,
    IFileUploadService upload_service,
    IEmailService emailService_service,
    ISecurityService securityService_service,
    IDeviceIdService deviceIdService_service,
    IEntitiesService entitiesService_service,
    IAuthenticationService authenticationService_service
    ) : IWebAPIServices
{
    public ILogger log => log_service;
    public IMicroMEncryption encryptor => encryptor_service;
    public IMicroMAppConfiguration app_config => app_config_service;
    public IBackgroundTaskQueue backgroundTaskQueue => backgroundTaskQueue_service;
    public IFileUploadService upload => upload_service;
    public IEmailService emailService => emailService_service;
    public ISecurityService securityService => securityService_service;
    public IDeviceIdService deviceIdService => deviceIdService_service;
    public IEntitiesService entitiesService => entitiesService_service;
    public IAuthenticationService authenticationService => authenticationService_service;
}
