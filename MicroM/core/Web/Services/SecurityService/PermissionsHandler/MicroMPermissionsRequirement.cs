using Microsoft.AspNetCore.Authorization;

namespace MicroM.Web.Services.Security
{
    /// <summary>
    /// Represents the MicroMPermissionsConstants.
    /// </summary>
    public class MicroMPermissionsConstants
    {
        /// <summary>
        /// ""; field.
        /// </summary>
        public string MicroMPermissionsPolicy = "";
    }

    /// <summary>
    /// Represents the MicroMPermissionsRequirement.
    /// </summary>
    public class MicroMPermissionsRequirement : IAuthorizationRequirement
    {
    }
}
