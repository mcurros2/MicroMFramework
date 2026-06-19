using MicroM.Configuration;
using SkiaSharp;

namespace MicroM.Web.Services;

public class ImageThumbnailService : IThumbnailService
{

    public static (int width, int height) CalculateThumbnailSize(int originalWidth, int originalHeight, int maxSize)
    {
        double ratioX = (double)maxSize / originalWidth;
        double ratioY = (double)maxSize / originalHeight;
        double ratio = Math.Min(ratioX, ratioY);

        var newWidth = Math.Max(1, (int)Math.Round(originalWidth * ratio));
        var newHeight = Math.Max(1, (int)Math.Round(originalHeight * ratio));

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

    public (string thumbnailFileName, string extension, string fullDestinationPath) GetThumbnailFilename(string sourceFilePath, int maxSize = 150, int quality = 75)
    {
        if (string.IsNullOrWhiteSpace(sourceFilePath)) throw new ArgumentException("Source file path cannot be null or empty.", nameof(sourceFilePath));

        string fileNameWithoutExt = Path.GetFileNameWithoutExtension(sourceFilePath);
        string extension = Path.GetExtension(sourceFilePath);

        if (string.IsNullOrWhiteSpace(extension)) throw new ArgumentException("Source file must have a valid extension.", nameof(sourceFilePath));

        string thumbnailFileName = $"{fileNameWithoutExt}-thmb-{maxSize}-{quality}{extension}";

        string fullDestinationPath = Path.Combine(Path.GetDirectoryName(sourceFilePath) ?? string.Empty, thumbnailFileName);

        return (thumbnailFileName, extension, fullDestinationPath);
    }

    public string CreateThumbnail(string originalFileName, Stream sourceStream, int maxSize = 150, int quality = 75)
    {
        ArgumentNullException.ThrowIfNull(sourceStream, nameof(sourceStream));

        if (maxSize < 50 || maxSize > 800) throw new ArgumentOutOfRangeException(nameof(maxSize), "Max size must be between 50 and 800.");
        if (quality < 10 || quality > 100) throw new ArgumentOutOfRangeException(nameof(quality), "Quality must be between 10 and 100.");

        var (thumbnailFileName, extension, fullDestinationPath) = GetThumbnailFilename(originalFileName, maxSize, quality);

        var imageFormat = GetImageFormat(extension) ?? throw new NotSupportedException("Image format not supported.");


        using var skiaStream = new SKManagedStream(sourceStream, disposeManagedStream: false);

        using var codec = SKCodec.Create(skiaStream) ?? throw new InvalidOperationException("Failed to create source image codec.");

        var origin = codec.EncodedOrigin;

        if (!ShouldCreateThumbnail(codec.Info.Width, codec.Info.Height, origin, maxSize))
        {
            return string.Empty;
        }

        var decodeInfo = GetScaledDecodeInfo(codec, maxSize);

        using var decodedBitmap = new SKBitmap(decodeInfo);

        var decodeResult = codec.GetPixels(decodeInfo, decodedBitmap.GetPixels());
        if (decodeResult != SKCodecResult.Success && decodeResult != SKCodecResult.IncompleteInput)
            throw new InvalidOperationException($"Failed to decode image. Codec result: {decodeResult}");


        var (orientedWidth, orientedHeight) = GetOrientedSize(decodedBitmap.Width, decodedBitmap.Height, origin);

        var (finalWidth, finalHeight) = CalculateThumbnailSize(orientedWidth, orientedHeight, maxSize);

        using var finalBitmap = new SKBitmap(new SKImageInfo(finalWidth, finalHeight, SKColorType.Bgra8888, SKAlphaType.Premul));

        using (var canvas = new SKCanvas(finalBitmap))
        {
            canvas.Clear(SKColors.Transparent);

            var scaleX = (float)finalWidth / orientedWidth;
            var scaleY = (float)finalHeight / orientedHeight;

            canvas.Scale(scaleX, scaleY);

            ApplyExifOrientation(
                canvas,
                origin,
                decodedBitmap.Width,
                decodedBitmap.Height);

            canvas.DrawBitmap(decodedBitmap, 0, 0);
        }

        using var thumbnailImage = SKImage.FromBitmap(finalBitmap) ?? throw new InvalidOperationException("Failed to create thumbnail image.");

        using var encodedData = thumbnailImage.Encode(imageFormat, quality) ?? throw new InvalidOperationException("Failed to encode thumbnail image.");

        using FileStream thumbnailStream = new(
            fullDestinationPath, FileMode.Create, FileAccess.Write, FileShare.None, bufferSize: DataDefaults.DefaultExportToExcelFileStreamCapacity,
            options: FileOptions.Asynchronous | FileOptions.SequentialScan
            );

        encodedData.SaveTo(thumbnailStream);

        return thumbnailFileName;
    }

    private static (int Width, int Height) GetOrientedSize(int width, int height, SKEncodedOrigin origin)
    {
        return origin is SKEncodedOrigin.LeftTop or SKEncodedOrigin.RightTop or SKEncodedOrigin.RightBottom or SKEncodedOrigin.LeftBottom
                ? (height, width)
                : (width, height);
    }


    private static void ApplyExifOrientation(SKCanvas canvas, SKEncodedOrigin origin, int width, int height)
    {
        switch (origin)
        {
            // Normal
            case SKEncodedOrigin.TopLeft:
                break;

            // Mirror horizontal
            case SKEncodedOrigin.TopRight:
                canvas.Translate(width, 0);
                canvas.Scale(-1, 1);
                break;

            // Rotate 180
            case SKEncodedOrigin.BottomRight:
                canvas.Translate(width, height);
                canvas.RotateDegrees(180);
                break;

            // Mirror vertical
            case SKEncodedOrigin.BottomLeft:
                canvas.Translate(0, height);
                canvas.Scale(1, -1);
                break;

            // Transpose
            case SKEncodedOrigin.LeftTop:
                canvas.RotateDegrees(90);
                canvas.Scale(1, -1);
                break;

            // Rotate 90 CW
            case SKEncodedOrigin.RightTop:
                canvas.Translate(height, 0);
                canvas.RotateDegrees(90);
                break;

            // Transverse
            case SKEncodedOrigin.RightBottom:
                canvas.Translate(height, width);
                canvas.RotateDegrees(90);
                canvas.Scale(-1, 1);
                break;

            // Rotate 270 CW
            case SKEncodedOrigin.LeftBottom:
                canvas.Translate(0, width);
                canvas.RotateDegrees(-90);
                break;
        }
    }

    private static SKImageInfo GetScaledDecodeInfo(SKCodec codec, int maxSize)
    {
        var info = codec.Info;

        if (info.Width <= 0 || info.Height <= 0) throw new InvalidOperationException("Invalid source image dimensions.");

        var scale = Math.Min((float)maxSize / info.Width, (float)maxSize / info.Height);

        scale = Math.Min(scale, 1f);

        var scaledSize = codec.GetScaledDimensions(scale);

        if (scaledSize.Width <= 0 || scaledSize.Height <= 0) scaledSize = new SKSizeI(info.Width, info.Height);

        return new SKImageInfo(scaledSize.Width, scaledSize.Height, SKColorType.Bgra8888, SKAlphaType.Premul);
    }

    private static bool ShouldCreateThumbnail(int originalWidth, int originalHeight, SKEncodedOrigin origin, int maxSize)
    {
        var (orientedWidth, orientedHeight) = GetOrientedSize(originalWidth, originalHeight, origin);

        return orientedWidth > maxSize || orientedHeight > maxSize;
    }
}
