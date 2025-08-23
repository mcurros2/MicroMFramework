namespace MicroM.Web
{
    /// <summary>
    /// Represents a basic lookup result returned by a lookup service.
    /// </summary>
    public record LookupResult
    {
        /// <summary>
        /// Gets or sets the descriptive text for the result.
        /// </summary>
        public string? Description { get; set; } = null;
    }
}
