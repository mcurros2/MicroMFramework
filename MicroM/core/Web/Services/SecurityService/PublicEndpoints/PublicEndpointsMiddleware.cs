using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace MicroM.Web.Services.Security;

public class PublicEndpointsMiddleware(RequestDelegate next, IMicroMAppConfiguration config, ILogger<PublicEndpointsMiddleware> log)
{
    private readonly RequestDelegate _next = next;
    private readonly IMicroMAppConfiguration _config = config;

    public async Task InvokeAsync(HttpContext context)
    {
        var endpoint = context.GetEndpoint();
        CancellationToken ct = context.RequestAborted;

        if (endpoint != null)
        {
            var hasPublicRouteAttribute = endpoint.Metadata.GetMetadata<PublicEndpointAttribute>() != null;

            if (hasPublicRouteAttribute)
            {
                if (!context.Request.RouteValues.TryGetValue("app_id", out var appIdObj) || appIdObj == null)
                {
                    context.Response.StatusCode = StatusCodes.Status403Forbidden;
                    log.LogTrace("App ID missing in public route {app_id} {route}", appIdObj, context.Request.Path);
                    return;
                }

                string app_id = (string)appIdObj;

                string? routePath = context.Request.Path.Value;

                if (string.IsNullOrEmpty(routePath) || string.IsNullOrEmpty(app_id))
                {
                    context.Response.StatusCode = StatusCodes.Status403Forbidden;
                    log.LogTrace("App ID missing or route empty in public route {app_id} {route}", app_id, routePath);
                    return;
                }

                var allowed = _config.GetPublicAccessAllowedRoutes(app_id);

                if (allowed == null)
                {
                    context.Response.StatusCode = StatusCodes.Status403Forbidden;
                    log.LogTrace("Public route not configured for app {app_id} {route}", app_id, routePath);
                    return;
                }

                if (allowed.AllowedRoutes == null)
                {
                    context.Response.StatusCode = StatusCodes.Status403Forbidden;
                    log.LogTrace("Public route allowed routes null for app {app_id} {route}", app_id, routePath);
                    return;
                }

                if (!allowed.AllowedRoutes.Contains(routePath, StringComparer.OrdinalIgnoreCase))
                {
                    context.Response.StatusCode = StatusCodes.Status403Forbidden;
                    log.LogTrace("Public route not allowed for app {app_id} {route}", app_id, routePath);
                    return;
                }

            }
        }


        await _next(context);
    }
}
