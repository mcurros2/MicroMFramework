using MicroM.Configuration;
using MicroM.Data;
using MicroM.Web.Services;

namespace MicroM.Web.Authentication;

/// <summary>
/// Resolves authenticators and assists with claim handling for applications.
/// </summary>
public interface IAuthenticationProvider
{
    /// <summary>
    /// Retrieves an <see cref="IAuthenticator"/> implementation based on the application configuration.
    /// </summary>
    /// <param name="app_config">Application options describing the authentication scheme.</param>
    /// <returns>The matching authenticator instance, or <see langword="null"/> if none is available.</returns>
    IAuthenticator? GetAuthenticator(ApplicationOption app_config);

    /// <summary>
    /// Obtains application configuration data and decrypts any supplied server claims.
    /// </summary>
    /// <param name="app_config">Access to configured application options.</param>
    /// <param name="app_id">The identifier of the application.</param>
    /// <param name="parms">The request parameters where decrypted claims will be stored.</param>
    /// <param name="claims">Encrypted server claims to be decrypted.</param>
    /// <returns>The application configuration, or <see langword="null"/> if the application cannot be found.</returns>
    ApplicationOption? GetAppAndUnencryptClaims(IMicroMAppConfiguration app_config, string app_id, DataWebAPIRequest parms, Dictionary<string, object>? claims);
}
