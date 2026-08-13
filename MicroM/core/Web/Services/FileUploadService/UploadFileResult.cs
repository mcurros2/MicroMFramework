namespace MicroM.Web.Services
{
    public record UploadFileResult
    {
        public string? ErrorMessage { get; init; } = null;
        public string FileProcessId { get; init; } = string.Empty;
        public string vc_fileguid { get; init; } = string.Empty;

    }
}
