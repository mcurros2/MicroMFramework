using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using System.Text;
using System.Text.Json;

namespace MicroM.Web.Middleware
{
    /// <summary>
    /// Represents the DebugRoutesMiddleware.
    /// </summary>
    public class DebugRoutesMiddleware
    {
        private readonly RequestDelegate _next;
        private string _debugRoutesURL;
        private static readonly JsonSerializerOptions _jsonSerializerOptions = new JsonSerializerOptions() { WriteIndented = true };

        /// <summary>
        /// Initializes a new instance of the <see cref="DebugRoutesMiddleware"/> that exposes
        /// route information at a specified debugging endpoint.
        /// </summary>
        /// <param name="next">The next middleware delegate in the pipeline.</param>
        /// <param name="debugRoutesURL">The request path that triggers route debugging.</param>
        public DebugRoutesMiddleware(RequestDelegate next, string debugRoutesURL)
        {
            _next = next;
            _debugRoutesURL = debugRoutesURL;
        }

        /// <summary>
        /// Intercepts requests to the configured debug route URL and writes a JSON array
        /// describing all known MVC routes to the response; otherwise, the request is
        /// forwarded to the next middleware.
        /// </summary>
        /// <param name="context">The current HTTP context.</param>
        /// <param name="provider">Provides the collection of MVC action descriptors used to build the route listing.</param>
        /// <returns>A task that represents the asynchronous operation.</returns>
        public async Task Invoke(HttpContext context, IActionDescriptorCollectionProvider? provider)
        {
            if (context.Request.Path == _debugRoutesURL)
            {
                if (null != provider)
                {
                    var routes = provider.ActionDescriptors.Items.Select(x => new
                    {
                        Action = x.RouteValues.TryGetValue("Action", out string? action) ? action : null,
                        Controller = x.RouteValues.TryGetValue("Controller", out string? controller) ? controller : null,
                        Page = x.RouteValues.TryGetValue("Page", out string? page) ? page : null,
                        x.AttributeRouteInfo?.Name,
                        x.AttributeRouteInfo?.Template,
                        Contraint = x.ActionConstraints
                    }).ToArray();

                    var routesJson = JsonSerializer.Serialize(routes, _jsonSerializerOptions);

                    context.Response.ContentType = "application/json";
                    await context.Response.WriteAsync(routesJson, Encoding.UTF8);
                }
                else
                {
                    await context.Response.WriteAsync("IActionDescriptorCollectionProvider is null", Encoding.UTF8);
                }
            }
            else
            {
                await _next(context);
            }
        }
    }
}
