using Microsoft.AspNetCore.Http;

namespace MicroM.Web.Services.Security
{
    /// <summary>
    /// Middleware that validates requests to endpoints marked with
    /// <see cref="PublicEndpointAttribute"/>. It ensures that only routes
    /// explicitly configured for public access are served without
    /// authentication.
    /// </summary>
    /// <param name="next">Next component in the pipeline.</param>
    /// <param name="config">Application configuration used to obtain allowed public routes.</param>
    public class PublicEndpointsMiddleware(RequestDelegate next, IMicroMAppConfiguration config)
    {
        private readonly RequestDelegate _next = next;
        private readonly IMicroMAppConfiguration _config = config;

        /// <summary>
        /// Validates that a request targeting a public endpoint is configured
        /// to allow anonymous access. Requests to endpoints that are not
        /// listed in configuration result in a 403 response.
        /// </summary>
        /// <param name="context">Current HTTP context.</param>
        /// <returns>A task that completes when the middleware finishes processing.</returns>
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
                        await context.Response.WriteAsync(".1", ct);
                        return;
                    }

                    string app_id = (string)appIdObj;

                    string? routePath = context.Request.Path.Value;

                    if (string.IsNullOrEmpty(routePath) || string.IsNullOrEmpty(app_id))
                    {
                        context.Response.StatusCode = StatusCodes.Status403Forbidden;
                        await context.Response.WriteAsync(".2", ct);
                        return;
                    }

                    var allowed = _config.GetPublicAccessAllowedRoutes(app_id);

                    if (allowed == null)
                    {
                        context.Response.StatusCode = StatusCodes.Status403Forbidden;
                        await context.Response.WriteAsync(".3", ct);
                        return;
                    }

                    if (allowed.AllowedRoutes == null)
                    {
                        context.Response.StatusCode = StatusCodes.Status403Forbidden;
                        await context.Response.WriteAsync(".4", ct);
                        return;
                    }

                    if (!allowed.AllowedRoutes.Contains(routePath, StringComparer.OrdinalIgnoreCase))
                    {
                        context.Response.StatusCode = StatusCodes.Status403Forbidden;
                        await context.Response.WriteAsync("Invalid route", ct);
                        return;
                    }

                }
            }


            await _next(context);
        }
    }

}
