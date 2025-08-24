namespace MicroM.Web.Services
{
    public interface IThumbnailService
    {
        public string CreateThumbnail(string sourceFilePath, int maxSize = 150, int quality = 75);

        public bool IsImageSupported(string extension);

        public (string directory, string thumbnailFileName, string thumbnailFilePath, string extension) GetThumbnailFilename(string sourceFilePath, int maxSize = 150, int quality = 75);
    }
}
