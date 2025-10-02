using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace MicroM.Web.Services
{
    public class MicroMCookieManager : ICookieManager
    {
        private readonly ChunkingCookieManager _inner = new();
        private readonly ILogger<MicroMCookieManager> _log;
        private readonly IMicroMAppConfiguration _app_config;

        public MicroMCookieManager(IMicroMAppConfiguration app_config, ILogger<MicroMCookieManager> logger)
        {
            _log = logger;
            _app_config = app_config;
        }

        public string? GetRequestCookie(HttpContext context, string key)
        {
            return _inner.GetRequestCookie(context, key);
        }

        public void AppendResponseCookie(HttpContext context, string key, string? value, CookieOptions options)
        {
            var tenantPath = _app_config.GetTenantPath(context);

            if (tenantPath == null)
            {
                _log.LogWarning("No tenant path found for context {context}. Setting cookie {key} with default path.", context.TraceIdentifier, key);
                _inner.AppendResponseCookie(context, key, value, options);
                return;
            }

            var tenantOptions = new CookieOptions(options) { Path = tenantPath };
            _inner.AppendResponseCookie(context, key, value, tenantOptions);
        }

        public void DeleteCookie(HttpContext context, string key, CookieOptions options)
        {
            var tenantPath = _app_config.GetTenantPath(context);

            if (tenantPath == null)
            {
                _inner.DeleteCookie(context, key, options);
                return;
            }

            var tenantOptions = new CookieOptions(options) { Path = tenantPath };
            _inner.DeleteCookie(context, key, tenantOptions);
        }

    }
}
