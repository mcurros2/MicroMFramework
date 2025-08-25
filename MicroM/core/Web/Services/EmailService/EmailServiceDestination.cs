namespace MicroM.Web.Services
{
    /// <summary>
    /// Represents the EmailServiceDestination.
    /// </summary>
    public record EmailServiceDestination
    {
        /// <summary>
        /// Gets or sets the destination identifier.
        /// </summary>
        public string? reference_id { get; set; }
        /// <summary>
        /// Gets or sets the destination email address.
        /// </summary>
        public string? destination_email { get; set; }
        /// <summary>
        /// Gets or sets the name associated with the destination.
        /// </summary>
        public string? destination_name { get; set; }
        /// <summary>
        /// Gets or sets the collection of tags for the destination.
        /// </summary>
        public EmailServiceTags[]? tags { get; set; }
    }
}
