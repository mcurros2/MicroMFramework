using MicroM.Configuration;
using MicroM.Core;
using MicroM.Data;
using MicroM.Extensions;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Globalization;

namespace MicroM.Web.Services;

public class FileStorageService(IOptions<MicroMOptions> options, ILogger<FileStorageService> log) : IStorageService<FileStorageService>
{
    private readonly MicroMOptions _options = options.Value;
    private readonly FileExtensionContentTypeProvider _contentTypeProvider = new();

    public ResultWithStatus<NewFileNameResult, ErrorResult> GetNewFileName(IEntityClient ec, ApplicationOption app, string file_name)
    {
        var (allowed, extension) = file_name.IsFileExtensionAllowed(_options.AllowedUploadFileExtensions ?? ConfigurationDefaults.AllowedFileUploadExtensions);

        if (!allowed)
        {
            return new ResultWithStatus<NewFileNameResult, ErrorResult>(Status: new ErrorResult(Error: "invalid_file_extension", ErrorDescription: "File type not allowed"), Result: null);
        }

        string newFileName = $"{Guid.NewGuid()}{extension}";
        string folder = DateTime.Now.ToString("yyyyMM", CultureInfo.InvariantCulture);
        var uploadsPath = Path.Combine(_options.UploadsFolder!, app.ApplicationID, folder);

        string fullPath = Path.Combine(uploadsPath, newFileName);

        // MMC: check for directory traversal attacks
        if (!fullPath.StartsWith(uploadsPath, StringComparison.OrdinalIgnoreCase))
        {
            return new ResultWithStatus<NewFileNameResult, ErrorResult>(Status: new ErrorResult(Error: "invalid_file_path", ErrorDescription: "File path not allowed"), Result: null);
        }

        return new ResultWithStatus<NewFileNameResult, ErrorResult>(Status: null, Result: new NewFileNameResult(newFileName, extension, folder, uploadsPath, fullPath));
    }

    public async Task<ResultWithStatus<long, ErrorResult>> StoreFile(IEntityClient ec, ApplicationOption app, string fullPath, Stream filestream, CancellationToken ct)
    {
        if (string.IsNullOrEmpty(fullPath))
        {
            return new ResultWithStatus<long, ErrorResult>(Status: new ErrorResult(Error: "invalid_fullpath", ErrorDescription: "The full path is null or empty."), Result: -1);
        }

        try
        {
            var directory = Path.GetDirectoryName(fullPath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            using var fileStream = new FileStream(fullPath, FileMode.Create, FileAccess.Write, FileShare.None);
            await filestream.CopyToAsync(fileStream, ct);

            await fileStream.FlushAsync(ct);
            long fileSize = fileStream.Length;
            await fileStream.DisposeAsync();

            return new ResultWithStatus<long, ErrorResult>(Status: null, Result: fileSize);

        }
        catch (Exception ex)
        {
            log.LogError(ex, "Error occurred while storing file.");
            return new ResultWithStatus<long, ErrorResult>(Status: new ErrorResult(Error: "file_storage_error", ErrorDescription: "An error occurred while storing the file."), Result: -1);
        }
    }

    public async Task<GetFileStreamResult?> GetFileStream(IEntityClient ec, ApplicationOption app, FileDetails fileDetails, CancellationToken ct)
    {
        GetFileStreamResult? result = null;

        if (!string.IsNullOrEmpty(fileDetails.fullPath))
        {
            if (File.Exists(fileDetails.fullPath))
            {
                if (!_contentTypeProvider.TryGetContentType(fileDetails.fullPath, out var contentType))
                {
                    contentType = "application/octet-stream"; // Default MIME type
                }

                result = new() { ContentType = contentType, Stream = new FileStream(fileDetails.fullPath, FileMode.Open, FileAccess.Read, FileShare.Read) };
            }

        }

        return result;
    }

}
