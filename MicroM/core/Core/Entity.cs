using MicroM.Data;
using MicroM.Web.Services;

namespace MicroM.Core
{
    /// <summary>
    /// Provides a strongly-typed entity base that exposes a definition of type
    /// <typeparamref name="TDefinition"/>.
    /// </summary>
    /// <typeparam name="TDefinition">The entity definition type.</typeparam>
    public abstract class Entity<TDefinition> : EntityBase where TDefinition : EntityDefinition, new()
    {
        /// <summary>
        /// Gets the strongly typed definition for this entity.
        /// </summary>
        public new TDefinition Def
        {
            get => (TDefinition)base.Def;
            protected set => base.Def = value;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Entity{TDefinition}"/> class.
        /// </summary>
        public Entity()
        {
            Def = new TDefinition();
            Def.ValidateDefinition(typeof(TDefinition).ToString());
            Init(null);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Entity{TDefinition}"/> class using the specified table name.
        /// </summary>
        /// <param name="table_name">The table name to associate with the entity definition.</param>
        public Entity(string table_name)
        {
            Def = new TDefinition() { TableName = table_name };
            Def.ValidateDefinition(typeof(TDefinition).ToString());
            Init(null);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Entity{TDefinition}"/> class using the provided client and encryptor.
        /// </summary>
        /// <param name="ec">Entity client used for data access.</param>
        /// <param name="encryptor">Optional encryption provider.</param>
        public Entity(IEntityClient ec, IMicroMEncryption? encryptor)
        {
            Def = new TDefinition();
            Def.ValidateDefinition(typeof(TDefinition).ToString());
            Init(ec, encryptor);
        }


    }
}
