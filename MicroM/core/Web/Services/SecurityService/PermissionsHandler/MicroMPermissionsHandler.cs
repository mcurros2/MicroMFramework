using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;

namespace MicroM.Web.Services.Security
{
    public class MicroMPermissionsHandler(ISecurityService securityService, IHttpContextAccessor http_context_accesor): AuthorizationHandler<MicroMPermissionsRequirement>
    {
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
