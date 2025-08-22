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
        /// Performs the DebugRoutesMiddleware operation.
        /// </summary>
        public DebugRoutesMiddleware(RequestDelegate next, string debugRoutesURL)
        {
            _next = next;
            _debugRoutesURL = debugRoutesURL;
        }

        /// <summary>
        /// Performs the Invoke operation.
        /// </summary>
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
