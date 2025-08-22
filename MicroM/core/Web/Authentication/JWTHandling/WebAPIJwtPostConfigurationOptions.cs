using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Options;

namespace MicroM.Web.Authentication
{
    /// <summary>
    /// Represents the WebAPIJwtPostConfigurationOptions.
    /// </summary>
    public class WebAPIJwtPostConfigurationOptions(WebAPIJsonWebTokenHandler handler) : IPostConfigureOptions<JwtBearerOptions>
    {
        private readonly WebAPIJsonWebTokenHandler _handler = handler;

        /// <summary>
        /// Performs the PostConfigure operation.
        /// </summary>
        public void PostConfigure(string? name, JwtBearerOptions options)
        {
            options.TokenHandlers.Clear();
            options.TokenHandlers.Add(_handler);
        }
    }
}
