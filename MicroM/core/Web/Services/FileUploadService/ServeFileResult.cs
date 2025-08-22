namespace MicroM.Web.Services
{
    /// <summary>
    /// Represents the ServeFileResult.
    /// </summary>
    public class ServeFileResult
    {
        /// <summary>
        /// Gets or sets the null!;.
        /// </summary>
        public FileStream FileStream { get; set; } = null!;
        /// <summary>
        /// Gets or sets the null!;.
        /// </summary>
        public string ContentType { get; set; } = null!;
    }
}
