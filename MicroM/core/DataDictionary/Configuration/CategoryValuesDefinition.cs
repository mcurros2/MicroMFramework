namespace MicroM.DataDictionary.Configuration
{
    /// <summary>
    /// Represents a single value within a <see cref="CategoryDefinition"/>.
    /// </summary>
    public class CategoryValuesDefinition
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="CategoryValuesDefinition"/> class.
        /// </summary>
        public CategoryValuesDefinition() : base() { }

        /// <summary>
        /// Gets the identifier of the category value.
        /// </summary>
        public string CategoryValueID { get; internal set; } = "";

        /// <summary>
        /// Gets the display description for the value.
        /// </summary>
        public string Description { get; init; } = "";

        /// <summary>
        /// Initializes a new instance of the <see cref="CategoryValuesDefinition"/> class with description and optional value identifier.
        /// </summary>
        /// <param name="description">Human readable description.</param>
        /// <param name="value_id">Optional explicit identifier for the value.</param>
        public CategoryValuesDefinition(string description, string value_id = "") : base()
        {
            CategoryValueID = value_id;
            Description = description;
        }
    }
}
