using MicroM.Core;

namespace MicroM.Data
{
    public class EntityFilter<TFilterEntity>(string name = "") : EntityFilterBase(name, typeof(TFilterEntity)) where TFilterEntity : EntityBase
    {
    }
}
