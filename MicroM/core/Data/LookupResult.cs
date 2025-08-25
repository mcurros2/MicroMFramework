namespace MicroM.Web
{
    /// <summary>
    /// Represents the outcome of an entity lookup, such as when a client requests
    /// the display text associated with a key value.
    /// </summary>
    public record LookupResult
    {
        /// <summary>
        /// Gets or sets the descriptive text returned by the lookup. UI components
        /// consume this value to show user-friendly text for a provided key. The value may
        /// be <see langword="null"/> when the lookup cannot resolve a description for the
        /// supplied key; callers should handle this by checking for <see langword="null"/>
        /// and substituting a placeholder or omitting the text as appropriate.
        /// </summary>
        public string? Description { get; set; } = null;
    }
}
