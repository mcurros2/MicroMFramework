using MicroM.Configuration;
using MicroM.Data;
using MicroM.DataDictionary.CategoriesDefinitions;
using MicroM.Web.Services;
using Microsoft.Extensions.Logging;

namespace MicroM.Web.Authentication;

/// <summary>
/// Represents the MicroMAuthenticationProvider.
/// </summary>
public class MicroMAuthenticationProvider(ILogger<MicroMAuthenticationProvider> log, IEnumerable<IAuthenticator> services) : IAuthenticationProvider
{
    private IEnumerable<IAuthenticator> _services = services;
    private readonly ILogger<MicroMAuthenticationProvider> _log = log;

    /// <summary>
    /// Performs the GetAuthenticator operation.
    /// </summary>
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
    /// Performs the GetAppAndUnencryptClaims operation.
    /// </summary>
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
