using SkiaSharp;

namespace MicroM.Web.Services
{
    public class ImageThumbnailService : IThumbnailService
    {
        public static (int width, int height) CalculateThumbnailSize(int originalWidth, int originalHeight, int maxSize)
        {
            double ratioX = (double)maxSize / originalWidth;
            double ratioY = (double)maxSize / originalHeight;
            double ratio = Math.Min(ratioX, ratioY);

            int newWidth = (int)(originalWidth * ratio);
            int newHeight = (int)(originalHeight * ratio);

            return (newWidth, newHeight);
        }

        public bool IsImageSupported(string extension)
        {
            return GetImageFormat(extension) != null;
        }

        private static SKEncodedImageFormat? GetImageFormat(string extension)
        {
            return extension.ToLowerInvariant() switch
            {
                ".jpg" or ".jpeg" => SKEncodedImageFormat.Jpeg,
                ".png" => SKEncodedImageFormat.Png,
                ".gif" => SKEncodedImageFormat.Gif,
                ".bmp" => SKEncodedImageFormat.Bmp,
                ".webp" => SKEncodedImageFormat.Webp,
                _ => null,
            };
        }

        public (string directory, string thumbnailFileName, string thumbnailFilePath, string extension)
            GetThumbnailFilename(string sourceFilePath, int maxSize = 150, int quality = 75)
        {
            if (string.IsNullOrWhiteSpace(sourceFilePath))
                throw new ArgumentException("Source file path cannot be null or empty.", nameof(sourceFilePath));

            string directory = Path.GetDirectoryName(sourceFilePath)
                               ?? throw new ArgumentException("Invalid source file path.", nameof(sourceFilePath));
            string fileNameWithoutExt = Path.GetFileNameWithoutExtension(sourceFilePath);
            string extension = Path.GetExtension(sourceFilePath);

            if (string.IsNullOrWhiteSpace(extension))
                throw new ArgumentException("Source file must have a valid extension.", nameof(sourceFilePath));

            string thumbnailFileName = $"{fileNameWithoutExt}-thmb-{maxSize}-{quality}{extension}";
            string thumbnailFilePath = Path.Combine(directory, thumbnailFileName);

            return (directory, thumbnailFileName, thumbnailFilePath, extension);
        }

        public string CreateThumbnail(string sourceFilePath, int maxSize = 150, int quality = 75)
        {
            if (string.IsNullOrWhiteSpace(sourceFilePath))
                throw new ArgumentException("Source file path cannot be null or empty.", nameof(sourceFilePath));

            if (maxSize < 50 || maxSize > 800)
                throw new ArgumentOutOfRangeException(nameof(maxSize), "Max size must be between 50 and 800.");

            if (quality < 10 || quality > 100)
                throw new ArgumentOutOfRangeException(nameof(quality), "Quality must be between 10 and 100.");

            var (directory, thumbnailFileName, thumbnailFilePath, extension) = GetThumbnailFilename(sourceFilePath);

            SKEncodedImageFormat? imageFormat = GetImageFormat(extension)
                ?? throw new NotSupportedException("Image format not supported.");

            using var sourceStream = File.OpenRead(sourceFilePath);
            using SKManagedStream skiaStream = new(sourceStream);
            using var originalBitmap = SKBitmap.Decode(skiaStream)
                ?? throw new InvalidOperationException("Failed to decode the source image.");

            (int newWidth, int newHeight) = CalculateThumbnailSize(originalBitmap.Width, originalBitmap.Height, maxSize);

            using SKBitmap resizedBitmap = originalBitmap.Resize(new SKImageInfo(newWidth, newHeight), SKFilterQuality.High)
                ?? throw new InvalidOperationException("Failed to resize the image.");

            using SKImage thumbnailImage = SKImage.FromBitmap(resizedBitmap);

            SKData encodedData = thumbnailImage.Encode(imageFormat.Value, quality);

            using FileStream thumbnailStream = File.OpenWrite(thumbnailFilePath);

            encodedData.SaveTo(thumbnailStream);

            return thumbnailFilePath;
        }

    }
}
