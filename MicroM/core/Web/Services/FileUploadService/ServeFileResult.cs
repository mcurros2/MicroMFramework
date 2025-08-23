namespace MicroM.Web.Services
{
    /// <summary>
    /// Represents a file served to the client.
    /// </summary>
    public class ServeFileResult
    {
        /// <summary>
        /// Stream containing the file contents.
        /// </summary>
        public FileStream FileStream { get; set; } = null!;
        /// <summary>
        /// MIME type of the file being served.
        /// </summary>
        public string ContentType { get; set; } = null!;
    }
}
