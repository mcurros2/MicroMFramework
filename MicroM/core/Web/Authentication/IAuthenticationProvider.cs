using MicroM.Configuration;

namespace MicroM.Web.Authentication
{
    public interface IAuthenticationProvider
    {
        public abstract IAuthenticator? GetAuthenticator(ApplicationOption app_config);

    }
}
