using MicroM.Data;
using MicroM.Web.Authentication;
using MicroM.Web.Services;
using Microsoft.AspNetCore.Mvc;

namespace MicroM.Web.Controllers;

public interface IEntitiesController
{
    string GetStatus();

    Task<ObjectResult> Action(IAuthenticationProvider auth, IMicroMAppConfiguration app_config, IEntitiesService ents, string app_id, string entityName, string actionName, DataWebAPIRequest parms, CancellationToken ct);
    Task<ObjectResult> Insert(IAuthenticationProvider auth, IMicroMAppConfiguration app_config, IEntitiesService ents, DataWebAPIRequest parms, string app_id, string entityName, CancellationToken ct);
    Task<ObjectResult> Get(IAuthenticationProvider auth, IMicroMAppConfiguration app_config, IEntitiesService ents, string app_id, string entityName, DataWebAPIRequest parms, CancellationToken ct);
    Task<ObjectResult> Update(IAuthenticationProvider auth, IMicroMAppConfiguration app_config, IEntitiesService ents, string app_id, string entityName, DataWebAPIRequest parms, CancellationToken ct);
    Task<ObjectResult> Delete(IAuthenticationProvider auth, IMicroMAppConfiguration app_config, IEntitiesService ents, string app_id, string entityName, DataWebAPIRequest parms, CancellationToken ct);
    Task<ObjectResult> Lookup(IAuthenticationProvider auth, IMicroMAppConfiguration app_config, IEntitiesService ents, string app_id, string entityName, string? lookupName, DataWebAPIRequest parms, CancellationToken ct);
    Task<ObjectResult> Import(IAuthenticationProvider auth, IMicroMAppConfiguration app_config, IEntitiesService ents, string app_id, string entityName, string? import_proc, DataWebAPIRequest parms, CancellationToken ct);
    Task<ObjectResult> View(IAuthenticationProvider auth, IMicroMAppConfiguration app_config, IEntitiesService ents, string app_id, string entityName, string viewName, DataWebAPIRequest parms, CancellationToken ct);
    Task<ObjectResult> Proc(IAuthenticationProvider auth, IMicroMAppConfiguration app_config, IEntitiesService ents, string app_id, string entityName, string procName, DataWebAPIRequest parms, CancellationToken ct);
    Task<ObjectResult> Process(IAuthenticationProvider auth, IMicroMAppConfiguration app_config, IEntitiesService ents, string app_id, string entityName, string procName, DataWebAPIRequest parms, CancellationToken ct);
    ObjectResult GetDefinition(IEntitiesService ents, IMicroMAppConfiguration app_config, string app_id, string entityName);
    Task<int> GetTimeZoneOffset(IAuthenticationProvider auth, IMicroMAppConfiguration app_config, IEntitiesService ents, string app_id, CancellationToken ct);

}
