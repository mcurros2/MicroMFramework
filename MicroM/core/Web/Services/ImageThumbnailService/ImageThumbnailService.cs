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

            using SKCodec codec = SKCodec.Create(skiaStream);
            var originalBitmap = SKBitmap.Decode(codec);
            if (originalBitmap == null)
            {
                throw new InvalidOperationException("Failed to decode the source image.");
            }

            SKMatrix matrix = GetExifMatrix(codec.EncodedOrigin);

            var orientedBitmap = new SKBitmap(originalBitmap.Width, originalBitmap.Height);

            using (var canvas = new SKCanvas(orientedBitmap))
            {
                canvas.Clear(SKColors.Transparent);
                canvas.SetMatrix(matrix);
                canvas.DrawBitmap(originalBitmap, 0, 0);
            }

            (int newWidth, int newHeight) = CalculateThumbnailSize(orientedBitmap.Width, orientedBitmap.Height, maxSize);

            using var resizedBitmap = originalBitmap.Resize(new SKImageInfo(newWidth, newHeight), new SKSamplingOptions(SKFilterMode.Linear, SKMipmapMode.None));
            using SKImage thumbnailImage = SKImage.FromBitmap(resizedBitmap);

            SKData encodedData = thumbnailImage.Encode(imageFormat.Value, quality);

            using FileStream thumbnailStream = File.OpenWrite(thumbnailFilePath);

            encodedData.SaveTo(thumbnailStream);

            // Disponer los recursos
            orientedBitmap.Dispose();
            originalBitmap.Dispose();

            return thumbnailFilePath;
        }

        // Método auxiliar para obtener la matriz de transformación
        private static SKMatrix GetExifMatrix(SKEncodedOrigin origin)
        {
            SKMatrix matrix = SKMatrix.CreateIdentity();
            switch (origin)
            {
                case SKEncodedOrigin.TopLeft:
                    break;
                case SKEncodedOrigin.TopRight:
                    matrix = SKMatrix.CreateScale(-1, 1, 0.5f, 0);
                    break;
                case SKEncodedOrigin.BottomRight:
                    matrix = SKMatrix.CreateRotation(180, 0.5f, 0.5f);
                    break;
                case SKEncodedOrigin.BottomLeft:
                    matrix = SKMatrix.CreateScale(1, -1, 0, 0.5f);
                    break;
                case SKEncodedOrigin.LeftTop:
                    matrix = SKMatrix.CreateRotation(90, 0.5f, 0.5f);
                    break;
                case SKEncodedOrigin.RightTop:
                    matrix = SKMatrix.CreateRotation(270, 0.5f, 0.5f);
                    break;
                case SKEncodedOrigin.RightBottom:
                    matrix = SKMatrix.CreateRotation(270, 0.5f, 0.5f);
                    break;
                case SKEncodedOrigin.LeftBottom:
                    matrix = SKMatrix.CreateRotation(90, 0.5f, 0.5f);
                    break;
            }
            return matrix;
        }


    }
}
