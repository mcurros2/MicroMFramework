using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Options;

namespace MicroM.Web.Authentication
{
    /// <summary>
    /// Post-configures JWT bearer options to use the custom <see cref="WebAPIJsonWebTokenHandler"/>.
    /// </summary>
    /// <param name="handler">Token handler that processes incoming JWTs.</param>
    public class WebAPIJwtPostConfigurationOptions(WebAPIJsonWebTokenHandler handler) : IPostConfigureOptions<JwtBearerOptions>
    {
        private readonly WebAPIJsonWebTokenHandler _handler = handler;

        /// <summary>
        /// Registers the custom token handler after the default options have been configured.
        /// </summary>
        /// <param name="name">The name of the options instance being configured.</param>
        /// <param name="options">The <see cref="JwtBearerOptions"/> to adjust.</param>
        public void PostConfigure(string? name, JwtBearerOptions options)
        {
            options.TokenHandlers.Clear();
            options.TokenHandlers.Add(_handler);
        }
    }
}
