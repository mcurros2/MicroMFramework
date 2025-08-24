using MicroM.Configuration;
using MicroM.Data;
using MicroM.DataDictionary.CategoriesDefinitions;
using MicroM.Web.Services;
using Microsoft.Extensions.Logging;

namespace MicroM.Web.Authentication;

/// <summary>
/// Provides access to different <see cref="IAuthenticator"/> implementations based on
/// application configuration and assists with claim handling.
/// </summary>
/// <param name="log">Logger used for diagnostic messages.</param>
/// <param name="services">Collection of registered authenticators.</param>
public class MicroMAuthenticationProvider(ILogger<MicroMAuthenticationProvider> log, IEnumerable<IAuthenticator> services) : IAuthenticationProvider
{
    private IEnumerable<IAuthenticator> _services = services;
    private readonly ILogger<MicroMAuthenticationProvider> _log = log;

    /// <summary>
    /// Retrieves an authenticator based on the application's configured authentication type.
    /// </summary>
    /// <param name="app_config">Application options describing the authentication scheme.</param>
    /// <returns>The matching authenticator instance, or <see langword="null"/> if none is found.</returns>
    public IAuthenticator? GetAuthenticator(ApplicationOption app_config)
    {

        switch (app_config.AuthenticationType)
        {
            case nameof(AuthenticationTypes.MicroMAuthentication):
                return GetService<MicroMAuthenticator>();

            case nameof(AuthenticationTypes.SQLServerAuthentication):
                return GetService<SQLServerAuthenticator>();

            default:
                return null;
        }
    }

    private IAuthenticator? GetService<T>()
    {
        return _services.FirstOrDefault(x => x.GetType() == typeof(T));
    }

    /// <summary>
    /// Retrieves application configuration by identifier and decrypts any provided server claims.
    /// </summary>
    /// <param name="app_config">Access to configured application options.</param>
    /// <param name="app_id">Identifier of the application.</param>
    /// <param name="parms">Request parameters where decrypted claims will be stored.</param>
    /// <param name="claims">Encrypted server claims to be decrypted.</param>
    /// <returns>The application configuration, or <see langword="null"/> if not found.</returns>
    public ApplicationOption? GetAppAndUnencryptClaims(IMicroMAppConfiguration app_config, string app_id, DataWebAPIRequest parms, Dictionary<string, object>? claims)
    {
        ApplicationOption? app = app_config.GetAppConfiguration(app_id);
        if (app != null && claims != null)
        {
            parms.ServerClaims = claims;
            GetAuthenticator(app)?.UnencryptClaims(parms.ServerClaims);
        }
        return app;
    }

}
