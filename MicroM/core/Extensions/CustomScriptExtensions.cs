using MicroM.Core;
using MicroM.Database;

namespace MicroM.Extensions;

public static class CustomScriptExtensions
{
    public static CustomOrderedDictionary<CustomScript> Filter(this CustomOrderedDictionary<CustomScript> custom_procs, CustomOrderedDictionary<DatabaseSchemaCreationOptions<EntityBase>> entities)
    {
        var filtered_custom_procs = new CustomOrderedDictionary<CustomScript>();
        foreach (var proc in custom_procs.Values)
        {
            if (entities.Values.Any(entity => (entity.EntityInstance.Def.Mneo == proc.mneo) || proc.mneo == null))
            {
                filtered_custom_procs.TryAdd(proc.ProcName ?? $"{Guid.NewGuid()}", proc);
            }
        }
        return filtered_custom_procs;
    }
}
