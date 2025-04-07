namespace MicroM.Web.Services
{
    public record EmailServiceItem
    {
        public string? EmailServiceConfigurationId { get; set; }
        public string? SenderEmail { get; set; }
        public string? SenderName { get; set; }
        public string? SubjectTemplate { get; set; }
        public string? MessageTemplate { get; set; }
        public EmailServiceDestination[]? Destinations { get; set; }
    }
}
