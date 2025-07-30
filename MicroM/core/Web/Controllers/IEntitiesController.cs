using MicroM.Data;
using MicroM.Web.Authentication;
using MicroM.Web.Services;
using Microsoft.AspNetCore.Mvc;

namespace MicroM.Web.Controllers;

public interface IEntitiesController
{
    string GetStatus();

    Task<ObjectResult> Action(IAuthenticationProvider auth, IEntitiesService ents, string app_id, string entityName, string actionName, DataWebAPIRequest parms, CancellationToken ct);
    Task<ObjectResult> Insert(IAuthenticationProvider auth, IEntitiesService ents, DataWebAPIRequest parms, string app_id, string entityName, CancellationToken ct);
    Task<ObjectResult> Get(IAuthenticationProvider auth, IEntitiesService ents, string app_id, string entityName, DataWebAPIRequest parms, CancellationToken ct);
    Task<ObjectResult> Update(IAuthenticationProvider auth, IEntitiesService ents, string app_id, string entityName, DataWebAPIRequest parms, CancellationToken ct);
    Task<ObjectResult> Delete(IAuthenticationProvider auth, IEntitiesService ents, string app_id, string entityName, DataWebAPIRequest parms, CancellationToken ct);
    Task<ObjectResult> Lookup(IAuthenticationProvider auth, IEntitiesService ents, string app_id, string entityName, string? lookupName, DataWebAPIRequest parms, CancellationToken ct);
    Task<ObjectResult> Import(IAuthenticationProvider auth, IEntitiesService ents, string app_id, string entityName, string? import_proc, DataWebAPIRequest parms, CancellationToken ct);
    Task<ObjectResult> View(IAuthenticationProvider auth, IEntitiesService ents, string app_id, string entityName, string viewName, DataWebAPIRequest parms, CancellationToken ct);
    Task<ObjectResult> Proc(IAuthenticationProvider auth, IEntitiesService ents, string app_id, string entityName, string procName, DataWebAPIRequest parms, CancellationToken ct);
    Task<ObjectResult> Process(IAuthenticationProvider auth, IEntitiesService ents, string app_id, string entityName, string procName, DataWebAPIRequest parms, CancellationToken ct);
    ObjectResult GetDefinition(IEntitiesService ents, string app_id, string entityName);
    Task<int> GetTimeZoneOffset(IAuthenticationProvider auth, IEntitiesService ents, string app_id, CancellationToken ct);

}
