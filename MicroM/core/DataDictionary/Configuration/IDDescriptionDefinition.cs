namespace MicroM.DataDictionary.Configuration
{
    /// <summary>
    /// Represents a definition that pairs an identifier with a description.
    /// </summary>
    public class IDDescriptionDefinition
    {
        /// <summary>
        /// Gets the identifier value.
        /// </summary>
        public string ID { get; init; } = null!;

        /// <summary>
        /// Gets the descriptive text associated with the identifier.
        /// </summary>
        public string Description { get; init; } = null!;

        /// <summary>
        /// Initializes a new instance of the <see cref="IDDescriptionDefinition"/> class.
        /// </summary>
        public IDDescriptionDefinition() { }

        /// <summary>
        /// Initializes a new instance of the <see cref="IDDescriptionDefinition"/> class with the specified identifier and description.
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
