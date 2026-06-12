using MicroM.Configuration;
using MicroM.Core;
using MicroM.Data;
using MicroM.DataDictionary.Entities;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace MicroM.Web.Services;

public class SQLServerStorageService(IOptions<MicroMOptions> options, ILogger<SQLServerStorageService> log) : IStorageService<SQLServerStorageService>
{
    private readonly MicroMOptions _options = options.Value;
    private readonly FileExtensionContentTypeProvider _contentTypeProvider = new();

    public ResultWithStatus<NewFileNameResult, ErrorResult> GetNewFileName(IEntityClient ec, ApplicationOption app, string file_name)
    {
        throw new NotImplementedException();
    }

    public async Task<GetFileStreamResult?> GetFileStream(IEntityClient ec, ApplicationOption app, FileDetails fileDetails, CancellationToken ct)
    {
        GetFileStreamResult? result = null;

        if (!string.IsNullOrEmpty(fileDetails.c_file_id) && !string.IsNullOrEmpty(fileDetails.fullPath))
        {
            if (!_contentTypeProvider.TryGetContentType(fileDetails.fullPath, out var contentType))
            {
                contentType = "application/octet-stream"; // Default MIME type
            }

            result = new() { ContentType = contentType, Stream = await FileStoreContent.GetFileStream(ec, app, fileDetails.c_file_id, ct) };
        }

        return result;
    }

    public async Task<ResultWithStatus<long, ErrorResult>> StoreFile(IEntityClient ec, ApplicationOption app, string fullPath, Stream filestream, CancellationToken ct)
    {
        try
        {
            await FileStoreContent.StoreFile(ec, app, fullPath, filestream, ct);

            return new ResultWithStatus<long, ErrorResult>(Result: filestream.Length, Status: null);
        }
        catch (Exception ex)
        {
            log.LogError("Error storing file {fullPath}: {ex}", fullPath, ex);
            return new ResultWithStatus<long, ErrorResult>(Result: 0, Status: new(Error: "FileStorageError", ErrorDescription: $"An error occurred while storing the file: {ex.Message}"));
        }
    }
}