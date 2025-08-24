namespace MicroM.Web.Services
{
    public record EmailServiceDestination
    {
        public string? reference_id { get; set; }
        public string? destination_email { get; set; }
        public string? destination_name { get; set; }
        public EmailServiceTags[]? tags { get; set; }
    }
}
