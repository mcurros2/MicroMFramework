namespace MicroM.Web.Services
{
    public class ServeFileResult
    {
        public FileStream FileStream { get; set; } = null!;
        public string ContentType { get; set; } = null!;
    }
}
