using MicroM.Web.Authentication;
using MicroM.Web.Services.Security;
using Microsoft.Extensions.Logging;

namespace MicroM.Web.Services;

/// <summary>
/// Represents the WebAPIServices.
/// </summary>
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
    /// <summary>
    /// log_service; field.
    /// </summary>
    public ILogger log => log_service;
    /// <summary>
    /// encryptor_service; field.
    /// </summary>
    public IMicroMEncryption encryptor => encryptor_service;
    /// <summary>
    /// app_config_service; field.
    /// </summary>
    public IMicroMAppConfiguration app_config => app_config_service;
    /// <summary>
    /// backgroundTaskQueue_service; field.
    /// </summary>
    public IBackgroundTaskQueue backgroundTaskQueue => backgroundTaskQueue_service;
    /// <summary>
    /// upload_service; field.
    /// </summary>
    public IFileUploadService upload => upload_service;
    /// <summary>
    /// emailService_service; field.
    /// </summary>
    public IEmailService emailService => emailService_service;
    /// <summary>
    /// securityService_service; field.
    /// </summary>
    public ISecurityService securityService => securityService_service;
    /// <summary>
    /// deviceIdService_service; field.
    /// </summary>
    public IDeviceIdService deviceIdService => deviceIdService_service;
    /// <summary>
    /// entitiesService_service; field.
    /// </summary>
    public IEntitiesService entitiesService => entitiesService_service;
    /// <summary>
    /// authenticationService_service; field.
    /// </summary>
    public IAuthenticationService authenticationService => authenticationService_service;
}
