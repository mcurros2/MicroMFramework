using MicroM.Configuration;
using MicroM.Data;
using MicroM.Web.Services;

namespace MicroM.Web.Authentication;

/// <summary>
/// Represents the IAuthenticationProvider.
/// </summary>
public interface IAuthenticationProvider
{
    /// <summary>
    /// Performs the GetAuthenticator operation.
    /// </summary>
    public abstract IAuthenticator? GetAuthenticator(ApplicationOption app_config);
    /// <summary>
    /// Performs the GetAppAndUnencryptClaims operation.
    /// </summary>
    ApplicationOption? GetAppAndUnencryptClaims(IMicroMAppConfiguration app_config, string app_id, DataWebAPIRequest parms, Dictionary<string, object>? claims);

}
