using MicroM.Configuration;
using MicroM.Data;
using MicroM.Web.Services;

namespace MicroM.Core
{
    public abstract class EntityActionBase
    {
        public abstract Task<EntityActionResult> Execute(EntityBase entity, DataWebAPIRequest parms, EntityDefinition def, MicroMOptions? Options, IWebAPIServices? API, IMicroMEncryption? encryptor, CancellationToken ct, string? app_id);
    }
}
