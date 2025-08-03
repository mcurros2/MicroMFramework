using MicroM.Configuration;
using MicroM.Data;
using MicroM.Web.Services;

namespace MicroM.Web.Authentication
{
    public interface IAuthenticationProvider
    {
        public abstract IAuthenticator? GetAuthenticator(ApplicationOption app_config);
        ApplicationOption? GetAppAndUnencryptClaims(IMicroMAppConfiguration app_config, string app_id, DataWebAPIRequest parms, Dictionary<string, object>? claims);

    }
}
