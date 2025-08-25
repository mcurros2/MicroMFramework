using MicroM.Core;

namespace MicroM.Data
{
    /// <summary>
    /// Defines a reusable filter that targets a specific type of entity.
    /// </summary>
    /// <typeparam name="TFilterEntity">The type of <see cref="EntityBase"/> the filter applies to.</typeparam>
    /// <param name="name">An optional name identifying the filter instance.</param>
    public class EntityFilter<TFilterEntity>(string name = "") : EntityFilterBase(name, typeof(TFilterEntity)) where TFilterEntity : EntityBase
    {
    }
}
