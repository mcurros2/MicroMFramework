using MicroM.Data;
using MicroM.Web.Authentication;
using MicroM.Web.Services;
using Microsoft.AspNetCore.Mvc;

namespace MicroM.Web.Controllers;

public interface IPublicController
{
    string GetStatus();

    Task<ObjectResult> PublicAction(IAuthenticationProvider auth, IEntitiesService api, string app_id, string entityName, string actionName, DataWebAPIRequest parms, CancellationToken ct);
    Task<ObjectResult> PublicDelete(IAuthenticationProvider auth, IEntitiesService api, string app_id, string entityName, DataWebAPIRequest parms, CancellationToken ct);
    Task<ObjectResult> PublicGet(IAuthenticationProvider auth, IEntitiesService api, string app_id, string entityName, DataWebAPIRequest parms, CancellationToken ct);
    Task<ObjectResult> PublicInsert(IAuthenticationProvider auth, IEntitiesService api, DataWebAPIRequest parms, string app_id, string entityName, CancellationToken ct);
    Task<ObjectResult> PublicLookup(IAuthenticationProvider auth, IEntitiesService api, string app_id, string entityName, string? lookupName, DataWebAPIRequest parms, CancellationToken ct);
    Task<ObjectResult> PublicProc(IAuthenticationProvider auth, IEntitiesService api, string app_id, string entityName, string procName, DataWebAPIRequest parms, CancellationToken ct);
    Task<ObjectResult> PublicProcess(IAuthenticationProvider auth, IEntitiesService api, string app_id, string entityName, string procName, DataWebAPIRequest parms, CancellationToken ct);
    Task<ObjectResult> PublicUpdate(IAuthenticationProvider auth, IEntitiesService api, string app_id, string entityName, DataWebAPIRequest parms, CancellationToken ct);
    Task<ObjectResult> PublicView(IAuthenticationProvider auth, IEntitiesService api, string app_id, string entityName, string viewName, DataWebAPIRequest parms, CancellationToken ct);

}
