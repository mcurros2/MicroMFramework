namespace MicroM.Web.Services
{
    /// <summary>
    /// Represents the EmailServiceDestination.
    /// </summary>
    public record EmailServiceDestination
    {
        /// <summary>
        /// Gets or sets the }.
        /// </summary>
        public string? reference_id { get; set; }
        /// <summary>
        /// Gets or sets the }.
        /// </summary>
        public string? destination_email { get; set; }
        /// <summary>
        /// Gets or sets the }.
        /// </summary>
        public string? destination_name { get; set; }
        /// <summary>
        /// Gets or sets the }.
        /// </summary>
        public EmailServiceTags[]? tags { get; set; }
    }
}
