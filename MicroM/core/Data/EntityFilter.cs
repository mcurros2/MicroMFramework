using MicroM.Core;

namespace MicroM.Data
{
    /// <summary>
    /// Represents a typed filter definition for an entity.
    /// </summary>
    /// <typeparam name="TFilterEntity">The entity type the filter applies to.</typeparam>
    public class EntityFilter<TFilterEntity>(string name = "") : EntityFilterBase(name, typeof(TFilterEntity)) where TFilterEntity : EntityBase
    {
    }
}
