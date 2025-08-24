namespace MicroM.Web.Services
{
    /// <summary>
    /// Represents the EmailServiceTags.
    /// </summary>
    public record EmailServiceTags
    {
        /// <summary>
        /// Gets or sets the metadata tag name.
        /// </summary>
        public string? tag { get; set; }
        /// <summary>
        /// Gets or sets the value associated with the tag.
        /// </summary>
        public string? value { get; set; }
    }
}
