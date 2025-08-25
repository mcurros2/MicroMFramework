using MicroM.Core;

namespace MicroM.Database
{
    /// <summary>
    /// Options used when generating database schema for an entity.
    /// </summary>
    /// <typeparam name="T">Entity type.</typeparam>
    /// <param name="EntityInstance">Instance of the entity.</param>
    /// <param name="create_or_alter">Indicates if existing objects should be altered.</param>
    public record DatabaseSchemaCreationOptions<T>(T EntityInstance, bool create_or_alter = true) where T : EntityBase
    {
        /// <summary>Type of the entity.</summary>
        public Type EntityType => EntityInstance.GetType();
        /// <summary>Mnemonic of the entity.</summary>
        public string Mneo => EntityInstance.Def.Mneo;

        /// <summary>Gets or sets a value indicating whether to create or alter existing objects.</summary>
        public bool create_or_alter { get; set; } = create_or_alter;
    }
}
