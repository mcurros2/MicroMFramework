using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Options;

namespace MicroM.Web.Authentication
{
    public class WebAPIJwtPostConfigurationOptions(WebAPIJsonWebTokenHandler handler) : IPostConfigureOptions<JwtBearerOptions>
    {
        private readonly WebAPIJsonWebTokenHandler _handler = handler;

        public void PostConfigure(string? name, JwtBearerOptions options)
        {
            options.TokenHandlers.Clear();
            options.TokenHandlers.Add(_handler);
        }
    }
}
