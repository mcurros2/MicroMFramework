using MicroM.Configuration;
using MicroM.Core;
using MicroM.Data;
using MicroM.DataDictionary.Entities;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace MicroM.Web.Services;

public class SQLServerStorageService(IOptions<MicroMOptions> options, ILogger<SQLServerStorageService> log, IDiskFileCacheService disk_cache) : IStorageService<SQLServerStorageService>
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

            Stream result_stream;
            var cache_result = disk_cache.GetEntry(app.ApplicationID, fileDetails);
            if (cache_result != null)
            {
                result_stream = cache_result.fileStream;
            }
            else
            {
                var source_stream = await FileStoreContent.GetFileStream(ec, app, fileDetails.c_file_id, ct);
                var cached_stream = await disk_cache.AddEntry(app.ApplicationID, fileDetails, source_stream, ct);
                if (cached_stream != null)
                {
                    result_stream = cached_stream;
                }
                else
                {
                    log.LogError("Failed to cache file {fullPath} after retrieving from database. Returning stream directly.", fileDetails.fullPath);
                    result_stream = source_stream;
                }
            }

            result = new() { ContentType = contentType, Stream = result_stream };
        }

        return result;
    }

    public async Task<ResultWithStatus<long, ErrorResult>> StoreFile(IEntityClient ec, ApplicationOption app, FileDetails fileDetails, Stream filestream, CancellationToken ct)
    {
        try
        {
            await FileStoreContent.StoreFile(ec, app, fileDetails.c_file_id, filestream, ct);

            return new ResultWithStatus<long, ErrorResult>(Result: filestream.Length, Status: null);
        }
        catch (Exception ex)
        {
            log.LogError("Error storing file {fullPath}: {ex}", fileDetails.fullPath, ex);
            return new ResultWithStatus<long, ErrorResult>(Result: 0, Status: new(Error: "FileStorageError", ErrorDescription: $"An error occurred while storing the file: {ex.Message}"));
        }
    }
}