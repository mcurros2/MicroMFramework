using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;

namespace MicroM.Web.Services.Security
{
    /// <summary>
    /// ASP.NET Core authorization handler that validates a request against the
    /// <see cref="ISecurityService"/>. The handler inspects the current
    /// <see cref="HttpContext"/> to extract route information and delegates
    /// the authorization decision to <see cref="ISecurityService.IsAuthorized"/>.
    /// It is registered with the authorization middleware and participates in
    /// the pipeline for every request guarded by <see cref="MicroMPermissionsRequirement"/>.
    /// </summary>
    /// <param name="securityService">Service used to evaluate route permissions.</param>
    /// <param name="http_context_accesor">Accessor for obtaining the current <see cref="HttpContext"/>.</param>
    public class MicroMPermissionsHandler(ISecurityService securityService, IHttpContextAccessor http_context_accesor) : AuthorizationHandler<MicroMPermissionsRequirement>
    {
        /// <summary>
        /// Evaluates the authorization requirement for the current request.
        /// The method gathers the route path and application identifier from
        /// the <paramref name="context"/> and succeeds only when the security
        /// service confirms access is allowed.
        /// </summary>
        /// <param name="context">Authorization context containing the user and request information.</param>
        /// <param name="requirement">The requirement being evaluated.</param>
        /// <returns>A completed task once the requirement has been evaluated.</returns>
        protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, MicroMPermissionsRequirement requirement)
        {
            var httpContext = http_context_accesor.HttpContext;
            if (httpContext == null)
            {
                context.Fail();
                return Task.CompletedTask;
            }

            if (!httpContext.Request.RouteValues.TryGetValue("app_id", out var appIdObj) || appIdObj == null)
            {
                context.Fail();
                return Task.CompletedTask;
            }

            string app_id = (string)appIdObj;

            string? routePath = httpContext.Request.Path.Value;

            if(string.IsNullOrEmpty(routePath) || string.IsNullOrEmpty(app_id))
            {
                context.Fail();
                return Task.CompletedTask;
            }

            var server_claims = context.User.Claims.ToDictionary(c => c.Type, c => (object?)c.Value, StringComparer.OrdinalIgnoreCase);

            if(server_claims == null)
            {
                context.Fail();
                return Task.CompletedTask;
            }

            if (securityService.IsAuthorized(app_id, routePath, server_claims))
            {
                context.Succeed(requirement);
            }
            else
            {
                context.Fail();
            }

            return Task.CompletedTask;
        }
    }
}
