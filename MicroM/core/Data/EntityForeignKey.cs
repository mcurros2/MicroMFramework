using MicroM.Core;

namespace MicroM.Data
{
    public class EntityForeignKey<TParent, TChild>(
        string name = "", bool fake = false, List<BaseColumnMapping>? key_mappings = null
        ) : EntityForeignKeyBase(name, typeof(TParent), typeof(TChild), fake, key_mappings) where TParent : EntityBase where TChild : EntityBase
    {
    }
}
