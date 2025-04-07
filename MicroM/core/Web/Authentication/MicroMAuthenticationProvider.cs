using MicroM.Configuration;
using MicroM.DataDictionary.CategoriesDefinitions;
using Microsoft.Extensions.Logging;

namespace MicroM.Web.Authentication
{
    public class MicroMAuthenticationProvider(ILogger<MicroMAuthenticationProvider> log, IEnumerable<IAuthenticator> services) : IAuthenticationProvider
    {
        private IEnumerable<IAuthenticator> _services = services;
        private readonly ILogger<MicroMAuthenticationProvider> _log = log;

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
    }
}
