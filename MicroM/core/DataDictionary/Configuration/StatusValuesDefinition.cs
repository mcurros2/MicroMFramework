namespace MicroM.DataDictionary.Configuration
{
    /// <summary>
    /// Represents a single allowed value within a status definition.
    /// </summary>
    public class StatusValuesDefinition
    {
        /// <summary>
        /// Gets the identifier for the status value.
        /// </summary>
        public string StatusValueID { get; internal set; } = "";

        /// <summary>
        /// Gets the human readable description.
        /// </summary>
        public string Description { get; init; } = "";

        /// <summary>
        /// Gets a value indicating whether this is the initial status.
        /// </summary>
        public bool InitialValue { get; init; }

        /// <summary>
        /// Initializes a new instance of the <see cref="StatusValuesDefinition"/> class.
        /// </summary>
        public StatusValuesDefinition() : base() { }

        /// <summary>
        /// Initializes a new instance with description and optional initial value and identifier.
        /// </summary>
        /// <param name="description">Status description.</param>
        /// <param name="initialValue">True if the value is the initial status.</param>
        /// <param name="value_id">Optional explicit identifier.</param>
        public StatusValuesDefinition(string description, bool initialValue = false, string value_id = "")
        {
            StatusValueID = value_id;
            Description = description;
            InitialValue = initialValue;
        }
    }
}
