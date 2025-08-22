using MicroM.Core;

namespace MicroM.Data;

public class EntityForeignKey<TParent, TChild>(
    string name = "", bool fake = false, bool do_not_create_index = false, List<BaseColumnMapping>? key_mappings = null
    ) : EntityForeignKeyBase(name, typeof(TParent), typeof(TChild), fake, do_not_create_index, key_mappings) where TParent : EntityBase where TChild : EntityBase
{
}
