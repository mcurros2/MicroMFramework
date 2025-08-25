using MicroM.DataDictionary;
using MicroM.Extensions;

namespace MicroM.Web.Services.Security
{
    /// <summary>
    /// Enumerates framework endpoints that are always accessible without
    /// authentication. These routes are used by the authorization system to
    /// bypass security checks for common operations such as login refresh or
    /// file serving.
    /// </summary>
    public enum MicroMEveryoneAllowedRoutes
    {
        /// <summary>Route used to log the current user out.</summary>
        logoff,
        /// <summary>Endpoint that refreshes the authentication token.</summary>
        refresh,
        /// <summary>Checks whether the caller is currently authenticated.</summary>
        isloggedin,
        /// <summary>Serves static or stored files without authentication.</summary>
        serve,
        /// <summary>Uploads temporary files that do not require authentication.</summary>
        tmpupload,
        /// <summary>Starts the password recovery process.</summary>
        recoverpassword,
        /// <summary>Sends recovery e-mails to users.</summary>
        recoveryemail,
        /// <summary>Retrieves a thumbnail image for a stored file.</summary>
        thumbnail
    }

    /// <summary>
    /// Helper utilities for determining whether a request path is publicly
    /// accessible regardless of authentication state.
    /// </summary>
    public static class EveryoneAllowedRoutes
    {
        /// <summary>
        /// Determines if the specified route path is one of the globally
        /// available routes. These routes are derived from configuration and
        /// do not require any user permissions.
        /// </summary>
        /// <param name="base_path">Base API path configured for the application.</param>
        /// <param name="app_id">Identifier of the application owning the route.</param>
        /// <param name="route_path">Request path being evaluated.</param>
        /// <returns>
        /// <see langword="true"/> when the route is allowed for all users;
        /// otherwise, <see langword="false"/>.
        /// </returns>
        public static bool IsEveryoneAllowedRoute(string base_path, string app_id, string route_path)
        {
            if (route_path.IsIn(
                    [$"/{base_path}/{app_id}/auth/{nameof(MicroMEveryoneAllowedRoutes.isloggedin)}",
                    $"/{base_path}/{app_id}/auth/{nameof(MicroMEveryoneAllowedRoutes.refresh)}",
                    $"/{base_path}/{app_id}/auth/{nameof(MicroMEveryoneAllowedRoutes.logoff)}",
                    $"/{base_path}/{app_id}/auth/{nameof(MicroMEveryoneAllowedRoutes.recoverpassword)}",
                    $"/{base_path}/{app_id}/auth/{nameof(MicroMEveryoneAllowedRoutes.recoveryemail)}",
                    $"/{base_path}/{app_id}/ent/{nameof(MicromUsers)}/proc/{nameof(MicromUsersDef.usr_GetEnabledMenus)}",
                    $"/{base_path}/{app_id}/ent/{nameof(SystemProcs)}/proc/{nameof(SystemProcsDef.sys_GetTimeZoneOffset)}",
                    $"/{base_path}/{app_id}/ent/{nameof(FileStoreProcess)}/insert",
                    $"/{base_path}/{app_id}/ent/{nameof(FileStore)}/delete",
                    $"/{base_path}/{app_id}/ent/{nameof(CategoriesValues)}/view/{nameof(CategoriesValuesDef.cav_brwStandard)}",
                    $"/{base_path}/{app_id}/ent/{nameof(FileStore)}/view/{nameof(FileStoreDef.fst_brwFiles)}"],
                    StringComparer.OrdinalIgnoreCase
                    ))
            {
                return true;
            }

            if (route_path.StartsWith($"/{base_path}/{app_id}/{nameof(MicroMEveryoneAllowedRoutes.serve)}", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            if (route_path.StartsWith($"/{base_path}/{app_id}/{nameof(MicroMEveryoneAllowedRoutes.thumbnail)}", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            if (route_path.StartsWith($"/{base_path}/{app_id}/{nameof(MicroMEveryoneAllowedRoutes.tmpupload)}", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            return false;
        }
    }

}
