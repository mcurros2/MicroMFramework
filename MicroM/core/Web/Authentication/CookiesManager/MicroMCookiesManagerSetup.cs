using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace MicroM.Web.Services
{
    /// <summary>
    /// Represents the MicroMCookiesManagerSetup.
    /// </summary>
    public class MicroMCookiesManagerSetup(ICookieManager cookieManager, ILogger<MicroMCookiesManagerSetup> logger) : IPostConfigureOptions<CookieAuthenticationOptions>
    {
        private readonly ICookieManager _cookieManager = cookieManager;
        private readonly ILogger<MicroMCookiesManagerSetup> _log = logger;

        /// <summary>
        /// Performs the PostConfigure operation.
        /// </summary>
        public void PostConfigure(string? name, CookieAuthenticationOptions options)
        {
            options.CookieManager = _cookieManager;
        }
    }
}
