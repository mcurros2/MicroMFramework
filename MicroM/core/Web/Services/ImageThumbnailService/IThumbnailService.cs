namespace MicroM.Web.Services
{
    /// <summary>
    /// Represents the IThumbnailService.
    /// </summary>
    public interface IThumbnailService
    {
        /// <summary>
        /// Performs the CreateThumbnail operation.
        /// </summary>
        public string CreateThumbnail(string sourceFilePath, int maxSize = 150, int quality = 75);

        /// <summary>
        /// Performs the IsImageSupported operation.
        /// </summary>
        public bool IsImageSupported(string extension);

        /// <summary>
        /// Performs the public operation.
        /// </summary>
        public (string directory, string thumbnailFileName, string thumbnailFilePath, string extension) GetThumbnailFilename(string sourceFilePath, int maxSize = 150, int quality = 75);
    }
}
