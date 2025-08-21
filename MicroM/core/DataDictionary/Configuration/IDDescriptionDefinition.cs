namespace MicroM.DataDictionary.Configuration
{
    /// <summary>
    /// Defines a simple pair of identifier and description.
    /// </summary>
    public class IDDescriptionDefinition
    {
        /// <summary>
        /// Gets the identifier value.
        /// </summary>
        public string ID { get; init; } = null!;

        /// <summary>
        /// Gets the associated description text.
        /// </summary>
        public string Description { get; init; } = null!;

        /// <summary>
        /// Initializes a new instance of the <see cref="IDDescriptionDefinition"/> class.
        /// </summary>
        public IDDescriptionDefinition() { }

        /// <summary>
        /// Initializes a new instance of the <see cref="IDDescriptionDefinition"/> class with identifier and description.
        /// </summary>
        /// <param name="id">Identifier value.</param>
        /// <param name="description">Description text.</param>
        public IDDescriptionDefinition(string id, string description)
        {
            ID = id;
            Description = description;
        }
    }
}
