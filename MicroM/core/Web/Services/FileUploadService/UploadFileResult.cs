namespace MicroM.Web.Services
{
    /// <summary>
    /// Represents the UploadFileResult.
    /// </summary>
    public record UploadFileResult
    {
        /// <summary>
        /// Gets or sets the null;.
        /// </summary>
        public string? ErrorMessage { get; init; } = null;
        /// <summary>
        /// Gets or sets the string.Empty;.
        /// </summary>
        public string FileId { get; init; } = string.Empty;
        /// <summary>
        /// Gets or sets the string.Empty;.
        /// </summary>
        public string FileProcessId { get; init; } = string.Empty;
        /// <summary>
        /// Gets or sets the string.Empty;.
        /// </summary>
        public string FileGuid { get; init; } = string.Empty;

    }
}
