namespace MicroM.Web.Services
{
    /// <summary>
    /// Represents the EmailServiceItem.
    /// </summary>
    public record EmailServiceItem
    {
        /// <summary>
        /// Gets or sets the }.
        /// </summary>
        public string? EmailServiceConfigurationId { get; set; }
        /// <summary>
        /// Gets or sets the }.
        /// </summary>
        public string? SenderEmail { get; set; }
        /// <summary>
        /// Gets or sets the }.
        /// </summary>
        public string? SenderName { get; set; }
        /// <summary>
        /// Gets or sets the }.
        /// </summary>
        public string? SubjectTemplate { get; set; }
        /// <summary>
        /// Gets or sets the }.
        /// </summary>
        public string? MessageTemplate { get; set; }
        /// <summary>
        /// Gets or sets the }.
        /// </summary>
        public EmailServiceDestination[]? Destinations { get; set; }
    }
}
