namespace MicroM.Web.Services
{
    public record UploadFileResult
    {
        public string? ErrorMessage { get; init; } = null;
        public string FileId { get; init; } = string.Empty;
        public string FileProcessId { get; init; } = string.Empty;
        public string FileGuid { get; init; } = string.Empty;

    }
}
