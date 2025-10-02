using Microsoft.AspNetCore.Authorization;

namespace MicroM.Web.Services.Security
{
    public class MicroMPermissionsConstants
    {
        public string? MicroMPermissionsPolicy;
        public string? IdPClientPolicy;
    }

    public class MicroMPermissionsRequirement : IAuthorizationRequirement
    {
    }
}
