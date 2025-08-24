using MicroM.Web.Authentication;
using MicroM.Web.Services.Security;
using Microsoft.Extensions.Logging;

namespace MicroM.Web.Services;

/// <summary>
/// Concrete implementation of <see cref="IWebAPIServices"/> that aggregates the
/// dependencies required by the web API.  Wrapper methods use these services to
/// perform HTTP calls and standardize how responses are processed and returned
/// to clients.
/// </summary>
/// <param name="log_service">Logger used to capture diagnostics for wrapper
/// methods.</param>
/// <param name="encryptor_service">Service that encrypts and decrypts payloads
/// involved in HTTP calls.</param>
/// <param name="app_config_service">Provides configuration values that control
/// wrapper method behavior.</param>
/// <param name="backgroundTaskQueue_service">Queue used to schedule tasks
/// spawned from HTTP requests.</param>
/// <param name="upload_service">Handles file uploads initiated through API
/// calls.</param>
/// <param name="emailService_service">Sends application emails generated during
/// response handling.</param>
/// <param name="securityService_service">Performs authorization checks for
/// incoming requests.</param>
/// <param name="deviceIdService_service">Resolves device identifiers for
/// auditing HTTP interactions.</param>
/// <param name="entitiesService_service">Provides operations for manipulating
/// entities referenced by API calls.</param>
/// <param name="authenticationService_service">Manages authentication flows for
/// API consumers.</param>
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
    /// Gets the application-wide logger used by HTTP wrapper methods to record
    /// diagnostic information.
    /// </summary>
    /// <returns>The <see cref="ILogger"/> instance.</returns>
    public ILogger log => log_service;

    /// <summary>
    /// Gets the encryption utility that secures request and response data.
    /// </summary>
    /// <returns>The <see cref="IMicroMEncryption"/> implementation.</returns>
    public IMicroMEncryption encryptor => encryptor_service;

    /// <summary>
    /// Gets application configuration values consumed by wrapper methods.
    /// </summary>
    /// <returns>The <see cref="IMicroMAppConfiguration"/> instance.</returns>
    public IMicroMAppConfiguration app_config => app_config_service;

    /// <summary>
    /// Gets the background task queue used to run deferred work spawned from
    /// HTTP requests.
    /// </summary>
    /// <returns>The <see cref="IBackgroundTaskQueue"/> implementation.</returns>
    public IBackgroundTaskQueue backgroundTaskQueue => backgroundTaskQueue_service;

    /// <summary>
    /// Gets the service that manages file uploads from API consumers.
    /// </summary>
    /// <returns>The <see cref="IFileUploadService"/> instance.</returns>
    public IFileUploadService upload => upload_service;

    /// <summary>
    /// Gets the email service used to send notifications during response
    /// handling.
    /// </summary>
    /// <returns>The <see cref="IEmailService"/> implementation.</returns>
    public IEmailService emailService => emailService_service;

    /// <summary>
    /// Gets the security service responsible for authorization checks.
    /// </summary>
    /// <returns>The <see cref="ISecurityService"/> instance.</returns>
    public ISecurityService securityService => securityService_service;

    /// <summary>
    /// Gets the service that resolves client device identifiers.
    /// </summary>
    /// <returns>The <see cref="IDeviceIdService"/> implementation.</returns>
    public IDeviceIdService deviceIdService => deviceIdService_service;

    /// <summary>
    /// Gets the entities service used to manipulate domain objects.
    /// </summary>
    /// <returns>The <see cref="IEntitiesService"/> instance.</returns>
    public IEntitiesService entitiesService => entitiesService_service;

    /// <summary>
    /// Gets the authentication service that manages authentication operations.
    /// </summary>
    /// <returns>The <see cref="IAuthenticationService"/> implementation.</returns>
    public IAuthenticationService authenticationService => authenticationService_service;
}
