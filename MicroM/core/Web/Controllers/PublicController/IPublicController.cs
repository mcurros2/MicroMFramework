using MicroM.Data;
using MicroM.Web.Services;
using Microsoft.AspNetCore.Mvc;

namespace MicroM.Web.Controllers;

public interface IPublicController
{
    string GetStatus();

    Task<ObjectResult> PublicAction(IMicroMAppConfiguration app_config, IEntitiesService api, string app_id, string entityName, string actionName, DataWebAPIRequest parms, CancellationToken ct);
    Task<ObjectResult> PublicDelete(IMicroMAppConfiguration app_config, IEntitiesService api, string app_id, string entityName, DataWebAPIRequest parms, CancellationToken ct);
    Task<ObjectResult> PublicGet(IMicroMAppConfiguration app_config, IEntitiesService api, string app_id, string entityName, DataWebAPIRequest parms, CancellationToken ct);
    Task<ObjectResult> PublicInsert(IMicroMAppConfiguration app_config, IEntitiesService api, DataWebAPIRequest parms, string app_id, string entityName, CancellationToken ct);
    Task<ObjectResult> PublicLookup(IMicroMAppConfiguration app_config, IEntitiesService api, string app_id, string entityName, string? lookupName, DataWebAPIRequest parms, CancellationToken ct);
    Task<ObjectResult> PublicProc(IMicroMAppConfiguration app_config, IEntitiesService api, string app_id, string entityName, string procName, DataWebAPIRequest parms, CancellationToken ct);
    Task<ObjectResult> PublicProcess(IMicroMAppConfiguration app_config, IEntitiesService api, string app_id, string entityName, string procName, DataWebAPIRequest parms, CancellationToken ct);
    Task<ObjectResult> PublicUpdate(IMicroMAppConfiguration app_config, IEntitiesService api, string app_id, string entityName, DataWebAPIRequest parms, CancellationToken ct);
    Task<ObjectResult> PublicView(IMicroMAppConfiguration app_config, IEntitiesService api, string app_id, string entityName, string viewName, DataWebAPIRequest parms, CancellationToken ct);

}
