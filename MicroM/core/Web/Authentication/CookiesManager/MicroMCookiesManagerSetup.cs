using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace MicroM.Web.Services
{
    /// <summary>
    /// Applies the <see cref="MicroMCookieManager"/> to cookie authentication options after configuration.
    /// </summary>
    /// <param name="cookieManager">The cookie manager instance to apply.</param>
    /// <param name="logger">Logger used for diagnostic messages.</param>
    public class MicroMCookiesManagerSetup(ICookieManager cookieManager, ILogger<MicroMCookiesManagerSetup> logger) : IPostConfigureOptions<CookieAuthenticationOptions>
    {
        private readonly ICookieManager _cookieManager = cookieManager;
        private readonly ILogger<MicroMCookiesManagerSetup> _log = logger;

        /// <summary>
        /// Sets the custom cookie manager on the provided options.
        /// </summary>
        /// <param name="name">The name of the options instance being configured.</param>
        /// <param name="options">The cookie authentication options to post configure.</param>
        public void PostConfigure(string? name, CookieAuthenticationOptions options)
        {
            options.CookieManager = _cookieManager;
        }
    }
}
