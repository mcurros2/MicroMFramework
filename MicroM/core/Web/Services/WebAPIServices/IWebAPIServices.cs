using MicroM.Web.Authentication;
using MicroM.Web.Services.Security;
using Microsoft.Extensions.Logging;

namespace MicroM.Web.Services;

/// <summary>
/// Provides centralized access to core services used by the web API and the
/// wrapper methods that perform HTTP calls and coordinate response handling.
/// </summary>
public interface IWebAPIServices
{
    /// <summary>
    /// Gets the application-wide logger used by HTTP wrapper methods to record
    /// diagnostics and trace information.
    /// </summary>
    /// <returns>An <see cref="ILogger"/> instance for writing log entries.</returns>
    public ILogger log { get; }

    /// <summary>
    /// Gets the encryption utility leveraged by wrapper methods to protect
    /// sensitive request and response payloads.
    /// </summary>
    /// <returns>The <see cref="IMicroMEncryption"/> implementation.</returns>
    public IMicroMEncryption encryptor { get; }

    /// <summary>
    /// Gets application configuration values that guide HTTP call behavior.
    /// </summary>
    /// <returns>The <see cref="IMicroMAppConfiguration"/> instance.</returns>
    public IMicroMAppConfiguration app_config { get; }

    /// <summary>
    /// Gets the queue used by wrappers to schedule background work triggered by
    /// HTTP requests.
    /// </summary>
    /// <returns>The <see cref="IBackgroundTaskQueue"/> implementation.</returns>
    public IBackgroundTaskQueue backgroundTaskQueue { get; }

    /// <summary>
    /// Gets the file upload service supporting endpoints that accept file
    /// content.
    /// </summary>
    /// <returns>The <see cref="IFileUploadService"/> instance.</returns>
    public IFileUploadService upload { get; }

    /// <summary>
    /// Gets the email service used to send notifications as part of response
    /// handling.
    /// </summary>
    /// <returns>The <see cref="IEmailService"/> implementation.</returns>
    public IEmailService emailService { get; }

    /// <summary>
    /// Gets the security service responsible for authorization checks invoked
    /// by HTTP call wrappers.
    /// </summary>
    /// <returns>The <see cref="ISecurityService"/> instance.</returns>
    public ISecurityService securityService { get; }

    /// <summary>
    /// Gets the device identifier service that resolves and tracks client
    /// devices for auditing HTTP interactions.
    /// </summary>
    /// <returns>The <see cref="IDeviceIdService"/> implementation.</returns>
    public IDeviceIdService deviceIdService { get; }

    /// <summary>
    /// Gets the service that performs operations on entities when processing
    /// HTTP requests.
    /// </summary>
    /// <returns>The <see cref="IEntitiesService"/> instance.</returns>
    public IEntitiesService entitiesService { get; }

    /// <summary>
    /// Gets the authentication service that manages authentication flows for
    /// incoming API requests.
    /// </summary>
    /// <returns>The <see cref="IAuthenticationService"/> implementation.</returns>
    public IAuthenticationService authenticationService { get; }
}
