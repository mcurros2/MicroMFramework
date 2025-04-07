namespace MicroM.Web.Services
{
    public record InitialConfigurationRequest
    {
        public string SQLConfigurationDB { get; set; } = "";
        public string SQLConfigUser { get; set; } = "";
    }
}
