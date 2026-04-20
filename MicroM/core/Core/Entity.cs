using MicroM.Data;
using MicroM.Web.Services;

namespace MicroM.Core;

public abstract class Entity<TDefinition> : EntityBase where TDefinition : EntityDefinition, new()
{
    public new TDefinition Def
    {
        get => (TDefinition)base.Def;
        protected set => base.Def = value;
    }

    public Entity()
    {
        Def = new TDefinition();
        Def.ValidateDefinition(typeof(TDefinition).ToString());
        Init(null);
    }

    public Entity(string? schema_name = null)
    {
        Def = new TDefinition() { SchemaName = schema_name };
        Def.ValidateDefinition(typeof(TDefinition).ToString());
        Init(null, schema_name: schema_name);
    }

    public Entity(IEntityClient ec, IMicroMEncryption? encryptor, string? schema_name = null)
    {
        Def = new TDefinition() { SchemaName = schema_name };
        Def.ValidateDefinition(typeof(TDefinition).ToString());
        Init(ec, encryptor, schema_name);
    }


}
