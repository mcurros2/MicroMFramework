using MicroM.Configuration;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace MicroM.Web.Services
{
    /// <summary>
    /// Cookie manager that scopes authentication cookies per application tenant.
    /// </summary>
    public class MicroMCookieManager : ICookieManager
    {
        private readonly ChunkingCookieManager _inner = new();
        private readonly IOptions<MicroMOptions> _config;
        private readonly ILogger<MicroMCookieManager> _log;
        private readonly PathString _basePathString;

        /// <summary>
        /// Initializes a new instance of the <see cref="MicroMCookieManager"/> class.
        /// </summary>
        /// <param name="microm_config">Configuration options for the MicroM application.</param>
        /// <param name="logger">Logger used to emit diagnostic messages.</param>
        public MicroMCookieManager(IOptions<MicroMOptions> microm_config, ILogger<MicroMCookieManager> logger)
        {
            _config = microm_config;
            _log = logger;

            var raw = microm_config?.Value.MicroMAPICookieRootPath ?? string.Empty;
            var trimmed = raw.Trim().Trim('/');

            if (string.IsNullOrEmpty(trimmed))
            {
                _basePathString = PathString.Empty;
            }
            else
            {
                _basePathString = new PathString("/" + trimmed);
            }
        }


        private string? GetTenantPath(HttpContext context)
        {
            if (_basePathString == PathString.Empty)
            {
                _log.LogWarning("MicroMAPIBaseRootPath is not configured.");
                return null;
            }

            var fullPath = context.Request.PathBase.Add(context.Request.Path);

            if (fullPath.StartsWithSegments(_basePathString, StringComparison.OrdinalIgnoreCase, out var remainingPath))
            {
                if (string.IsNullOrEmpty(remainingPath.Value))
                {
                    _log.LogWarning("No APP_ID found in path {path}", fullPath);
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
                    _log.LogWarning("No APP_ID found in path {path}", fullPath);
                    return null;
                }

                // Assemble tenant fullPath
                var tenantPath = $"{_basePathString.Value}/{appId}/";
                return tenantPath;
            }

            _log.LogWarning("The path {path} does not match the configured base path {basePath}", fullPath, _basePathString.Value);
            return null;

        }

        /// <summary>
        /// Retrieves a cookie from the incoming request.
        /// </summary>
        /// <param name="context">The HTTP context of the current request.</param>
        /// <param name="key">The cookie key.</param>
        /// <returns>The cookie value, or <see langword="null"/> if not found.</returns>
        public string? GetRequestCookie(HttpContext context, string key)
        {
            return _inner.GetRequestCookie(context, key);
        }

        /// <summary>
        /// Appends a cookie to the outgoing response, adjusting the path for the tenant when applicable.
        /// </summary>
        /// <param name="context">The HTTP context of the current request.</param>
        /// <param name="key">The cookie key.</param>
        /// <param name="value">The cookie value.</param>
        /// <param name="options">Options controlling cookie creation.</param>
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

        /// <summary>
        /// Deletes a cookie from the response, adjusting the path for the tenant when applicable.
        /// </summary>
        /// <param name="context">The HTTP context of the current request.</param>
        /// <param name="key">The cookie key.</param>
        /// <param name="options">Options controlling cookie deletion.</param>
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
