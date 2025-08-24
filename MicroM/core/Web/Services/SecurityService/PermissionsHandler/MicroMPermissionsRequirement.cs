using Microsoft.AspNetCore.Authorization;

namespace MicroM.Web.Services.Security
{
    /// <summary>
    /// Holds constants for configuring the MicroM permission system.
    /// </summary>
    public class MicroMPermissionsConstants
    {
        /// <summary>
        /// Name of the policy that activates <see cref="MicroMPermissionsRequirement"/>.
        /// Applications configure this policy to require MicroM route permission checks.
        /// </summary>
        public string MicroMPermissionsPolicy = "";
    }

    /// <summary>
    /// Authorization requirement that signals MicroM's custom permission check.
    /// It is evaluated by <see cref="MicroMPermissionsHandler"/> to determine
    /// whether the current user is authorized for the requested route.
    /// </summary>
    public class MicroMPermissionsRequirement : IAuthorizationRequirement
    {
    }
}
