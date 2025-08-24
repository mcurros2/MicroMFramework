namespace MicroM.Web.Services
{
    /// <summary>
    /// Represents the EmailServiceItem.
    /// </summary>
    public record EmailServiceItem
    {
        /// <summary>
        /// Gets or sets the identifier of the email service configuration.
        /// </summary>
        public string? EmailServiceConfigurationId { get; set; }
        /// <summary>
        /// Gets or sets the sender's email address.
        /// </summary>
        public string? SenderEmail { get; set; }
        /// <summary>
        /// Gets or sets the sender's display name.
        /// </summary>
        public string? SenderName { get; set; }
        /// <summary>
        /// Gets or sets the template used for the email subject.
        /// </summary>
        public string? SubjectTemplate { get; set; }
        /// <summary>
        /// Gets or sets the template for the email message body.
        /// </summary>
        public string? MessageTemplate { get; set; }
        /// <summary>
        /// Gets or sets the collection of destination recipients.
        /// </summary>
        public EmailServiceDestination[]? Destinations { get; set; }
    }
}
