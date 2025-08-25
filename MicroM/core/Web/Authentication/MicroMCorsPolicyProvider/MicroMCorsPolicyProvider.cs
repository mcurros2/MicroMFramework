using MicroM.Configuration;
using MicroM.Web.Services;
using Microsoft.AspNetCore.Cors.Infrastructure;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace MicroM.Web.Authentication;

/// <summary>
/// Provides CORS policies for MicroM authentication endpoints, limiting access
/// to origins configured for each application.
/// </summary>
public class MicroMCorsPolicyProvider : ICorsPolicyProvider
{

    private readonly IMicroMAppConfiguration app_config;
    private readonly ILogger<MicroMCorsPolicyProvider> log;
    private readonly IOptions<MicroMOptions> config;

    private string _rootPath;

    /// <summary>
    /// Initializes a new instance of the <see cref="MicroMCorsPolicyProvider"/> class.
    /// </summary>
    /// <param name="app_config">Service used to retrieve application configuration.</param>
    /// <param name="log">Logger for diagnostic messages.</param>
    /// <param name="config">Options providing API root path information.</param>
    public MicroMCorsPolicyProvider(IMicroMAppConfiguration app_config, ILogger<MicroMCorsPolicyProvider> log, IOptions<MicroMOptions> config)
    {
        this.app_config = app_config;
        this.log = log;
        this.config = config;
        _rootPath = config.Value.MicroMAPIBaseRootPath?.Trim('/') ?? string.Empty;
    }

    /// <summary>
    /// Retrieves a CORS policy for the given request context.
    /// </summary>
    /// <param name="context">Current HTTP context.</param>
    /// <param name="policyName">Ignored policy name.</param>
    /// <returns>The resolved <see cref="CorsPolicy"/>.</returns>
    public Task<CorsPolicy?> GetPolicyAsync(HttpContext context, string? policyName)
    {
        var originHeader = context.Request.Headers.Origin.ToString();
        if (string.IsNullOrEmpty(originHeader))
        {
            return Task.FromResult<CorsPolicy?>(new CorsPolicyBuilder().AllowAnyHeader().AllowAnyMethod().AllowCredentials().Build());
        }

        string? appId = TryGetAppIdFromPath(context.Request.Path, _rootPath);

        var r = config.Value.MicroMAPIBaseRootPath;

        var policy = new CorsPolicyBuilder()
            .SetIsOriginAllowed(o =>
            {
                if (string.IsNullOrEmpty(o)) return false;
                var result = app_config.IsCORSOriginAllowed(appId, o);
                if (!result) log.LogWarning("CORS Origin Check FAILED. app_id: {app_id}, origin: {origin}", appId, o);
                return result;
            })
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials()
            .Build();

        return Task.FromResult<CorsPolicy?>(policy);
    }

    private static string? TryGetAppIdFromPath(PathString path, string? root)
    {
        var segs = (path.Value ?? string.Empty)
            .Split('/', StringSplitOptions.RemoveEmptyEntries);

        if (segs.Length == 0)
            return null;

        if (root == null)
        {
            return segs[0];
        }

        if (segs.Length >= 2 && segs[0].Equals(root, StringComparison.OrdinalIgnoreCase))
        {
            return segs[1];
        }

        return null;
    }

}
