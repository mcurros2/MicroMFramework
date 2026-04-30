using MicroM.Core;

namespace MicroM.AutomatedTests;

public interface IEntityType
{
    Type Type { get; }
}

public sealed class EntityType<E> : IEntityType where E : EntityBase, new()
{
    public Type Type { get; }
    public EntityType()
    {
        Type = typeof(E);
    }
}

