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
    /// Gets or sets the }.
    /// </summary>
    public ILogger log { get; }
    /// <summary>
    /// Gets or sets the }.
    /// </summary>
    public IMicroMEncryption encryptor { get; }
    /// <summary>
    /// Gets or sets the }.
    /// </summary>
    public IMicroMAppConfiguration app_config { get; }
    /// <summary>
    /// Gets or sets the }.
    /// </summary>
    public IBackgroundTaskQueue backgroundTaskQueue { get; }
    /// <summary>
    /// Gets or sets the }.
    /// </summary>
    public IFileUploadService upload { get; }
    /// <summary>
    /// Gets or sets the }.
    /// </summary>
    public IEmailService emailService { get; }
    /// <summary>
    /// Gets or sets the }.
    /// </summary>
    public ISecurityService securityService { get; }
    /// <summary>
    /// Gets or sets the }.
    /// </summary>
    public IDeviceIdService deviceIdService { get; }
    /// <summary>
    /// Gets or sets the }.
    /// </summary>
    public IEntitiesService entitiesService { get; }
    /// <summary>
    /// Gets or sets the }.
    /// </summary>
    public IAuthenticationService authenticationService { get; }
}
