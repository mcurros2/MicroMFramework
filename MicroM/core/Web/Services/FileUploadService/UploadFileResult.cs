namespace MicroM.Web.Services
{
    /// <summary>
    /// Result returned from a file upload operation.
    /// </summary>
    public record UploadFileResult
    {
        /// <summary>
        /// Error message when the upload fails; otherwise <see langword="null"/>.
        /// </summary>
        public string? ErrorMessage { get; init; } = null;
        /// <summary>
        /// Identifier of the stored file.
        /// </summary>
        public string FileId { get; init; } = string.Empty;
        /// <summary>
        /// Identifier of the process that handled the file.
        /// </summary>
        public string FileProcessId { get; init; } = string.Empty;
        /// <summary>
        /// Generated unique file name used for storage.
        /// </summary>
        public string FileGuid { get; init; } = string.Empty;

    }
}
