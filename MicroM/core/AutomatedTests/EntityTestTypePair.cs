using MicroM.Core;

namespace MicroM.AutomatedTests;

public interface IEntityTestTypePair
{
    Type EntityType { get; }
    Type DataType { get; }
}

public sealed class EntityTestTypePair<E, D> : IEntityTestTypePair where E : EntityBase, new() where D : BaseSeedData, new()
{
    public Type EntityType { get; }
    public Type DataType { get; }
    public EntityTestTypePair()
    {
        EntityType = typeof(E);
        DataType = typeof(D);
    }
}

