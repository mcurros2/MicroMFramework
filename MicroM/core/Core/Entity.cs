using MicroM.Data;
using MicroM.Web.Services;

namespace MicroM.Core
{
    
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

        public Entity(string table_name)
        {
            Def = new TDefinition() { TableName = table_name };
            Def.ValidateDefinition(typeof(TDefinition).ToString());
            Init(null);
        }

        public Entity(IEntityClient ec, IMicroMEncryption? encryptor)
        {
            Def = new TDefinition();
            Def.ValidateDefinition(typeof(TDefinition).ToString());
            Init(ec, encryptor);
        }


    }
}
