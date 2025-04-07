using MicroM.DataDictionary;
using MicroM.Extensions;

namespace MicroM.Web.Services.Security
{
    public enum MicroMEveryoneAllowedRoutes
    {
        logoff,
        refresh,
        isloggedin,
        serve,
        tmpupload,
        recoverpassword,
        recoveryemail,
        thumbnail
    }

    public static class EveryoneAllowedRoutes
    {
        public static bool IsEveryoneAllowedRoute(string base_path, string app_id, string route_path)
        {
            if (route_path.IsIn(
                    [$"/{base_path}/{app_id}/{nameof(MicroMEveryoneAllowedRoutes.isloggedin)}",
                    $"/{base_path}/{app_id}/{nameof(MicroMEveryoneAllowedRoutes.refresh)}",
                    $"/{base_path}/{app_id}/{nameof(MicroMEveryoneAllowedRoutes.logoff)}",
                    $"/{base_path}/{app_id}/{nameof(MicroMEveryoneAllowedRoutes.recoverpassword)}",
                    $"/{base_path}/{app_id}/{nameof(MicroMEveryoneAllowedRoutes.recoveryemail)}",
                    $"/{base_path}/{app_id}/{nameof(MicromUsers)}/proc/{nameof(MicromUsersDef.usr_GetEnabledMenus)}",
                    $"/{base_path}/{app_id}/{nameof(FileStoreProcess)}/insert",
                    $"/{base_path}/{app_id}/{nameof(FileStore)}/delete",
                    $"/{base_path}/{app_id}/{nameof(CategoriesValues)}/view/{nameof(CategoriesValuesDef.cav_brwStandard)}",
                    $"/{base_path}/{app_id}/{nameof(FileStore)}/view/{nameof(FileStoreDef.fst_brwFiles)}"],
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
