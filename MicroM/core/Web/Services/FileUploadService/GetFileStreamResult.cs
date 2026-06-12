namespace MicroM.Web.Services
{
    public class GetFileStreamResult
    {
        public Stream Stream { get; set; } = null!;
        public string ContentType { get; set; } = null!;
    }
}
