namespace MicroM.Web.Services;

public interface IThumbnailService
{
    public string CreateThumbnail(string originalFileName, Stream sourceStream, int maxSize = 150, int quality = 75);

    public bool IsImageSupported(string extension);

    public (string thumbnailFileName, string extension, string fullDestinationPath) GetThumbnailFilename(string sourceFilePath, int maxSize = 150, int quality = 75);
}
