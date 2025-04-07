using MicroM.Configuration;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace MicroM.Web.Services
{
    public class MicroMCookieManager : ICookieManager
    {
        private readonly ChunkingCookieManager _inner = new();
        private readonly IOptions<MicroMOptions> _config;
        private readonly ILogger<MicroMCookieManager> _log;
        private readonly PathString _basePathString;

        public MicroMCookieManager(IOptions<MicroMOptions> microm_config, ILogger<MicroMCookieManager> logger)
        {
            _config = microm_config;
            _log = logger;
            var basePath = microm_config?.Value.MicroMAPICookieRootPath?.Trim('/') ?? string.Empty;
            _basePathString = new PathString("/" + basePath);
        }


        private string? GetTenantPath(HttpContext context)
        {
            var path = context.Request.Path;

            if (_basePathString == PathString.Empty)
            {
                _log.LogWarning("MicroMAPIBaseRootPath is not configured.");
                return null;
            }

            if (path.StartsWithSegments(_basePathString, StringComparison.OrdinalIgnoreCase, out var remainingPath))
            {
                if (string.IsNullOrEmpty(remainingPath.Value))
                {
                    _log.LogWarning("No APP_ID found in path {path}", path);
                    return null;
                }

                // remainingPath starts with "/APP_ID/..." o "/APP_ID"
                var remaining = remainingPath.Value.TrimStart('/');

                // Get APP_ID, from first segment in remainingPath
                var appIdEnd = remaining.IndexOf('/');
                string appId;

                if (appIdEnd == -1)
                {
                    appId = remaining; // last segment
                }
                else
                {
                    // Get appId from first segment
                    appId = remaining[..appIdEnd];
                }

                if (string.IsNullOrEmpty(appId))
                {
                    _log.LogWarning("No APP_ID found in path {path}", path);
                    return null;
                }

                // Assemble tenant path
                var tenantPath = $"{_basePathString.Value}/{appId}/";
                return tenantPath;
            }

            _log.LogWarning("The path {path} does not match the configured base path {basePath}", path, _basePathString.Value);
            return null;

        }

        public string? GetRequestCookie(HttpContext context, string key)
        {
            return _inner.GetRequestCookie(context, key);
        }

        public void AppendResponseCookie(HttpContext context, string key, string? value, CookieOptions options)
        {
            var tenantPath = GetTenantPath(context);

            if (tenantPath == null)
            {
                _inner.AppendResponseCookie(context, key, value, options);
                return;
            }

            var tenantOptions = new CookieOptions(options) { Path = tenantPath };
            _inner.AppendResponseCookie(context, key, value, tenantOptions);
        }

        public void DeleteCookie(HttpContext context, string key, CookieOptions options)
        {
            var tenantPath = GetTenantPath(context);

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
