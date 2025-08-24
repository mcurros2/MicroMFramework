using MicroM.Web.Authentication;
using MicroM.Web.Services.Security;
using Microsoft.Extensions.Logging;

namespace MicroM.Web.Services;

/// <summary>
/// Represents the IWebAPIServices.
/// </summary>
public interface IWebAPIServices
{
    /// <summary>
    /// Application-wide logger.
    /// </summary>
    public ILogger log { get; }
    /// <summary>
    /// Provides encryption utilities for the API.
    /// </summary>
    public IMicroMEncryption encryptor { get; }
    /// <summary>
    /// Supplies application configuration data.
    /// </summary>
    public IMicroMAppConfiguration app_config { get; }
    /// <summary>
    /// Queue for processing background tasks.
    /// </summary>
    public IBackgroundTaskQueue backgroundTaskQueue { get; }
    /// <summary>
    /// Handles file uploads.
    /// </summary>
    public IFileUploadService upload { get; }
    /// <summary>
    /// Sends application emails.
    /// </summary>
    public IEmailService emailService { get; }
    /// <summary>
    /// Provides authorization checks.
    /// </summary>
    public ISecurityService securityService { get; }
    /// <summary>
    /// Resolves and tracks device identifiers.
    /// </summary>
    public IDeviceIdService deviceIdService { get; }
    /// <summary>
    /// Performs entity-related operations.
    /// </summary>
    public IEntitiesService entitiesService { get; }
    /// <summary>
    /// Manages authentication operations.
    /// </summary>
    public IAuthenticationService authenticationService { get; }
}
