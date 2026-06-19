using MicroM.Configuration;
using MicroM.Configuration.CategoriesDefinitions;
using MicroM.Core;
using MicroM.Data;
using MicroM.DataDictionary.Entities;
using MicroM.DataDictionary.StatusDefinitions;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace MicroM.Web.Services;

public class FileUploadService(
    IOptions<MicroMOptions> options, ILogger<FileUploadService> log, ILoggerFactory loggerFactory, IThumbnailService thumbnailService, IStorageService<FileStorageService> fileStorage,
    IStorageService<SQLServerStorageService> sqlStorage, IOptions<DiskFileCacheOptions> cacheOptions
    ) : IFileUploadService, IDisposable
{
    private readonly MicroMOptions _options = options.Value;
    private readonly DiskFileCacheOptions _cacheOptions = cacheOptions.Value;
    private readonly FileExtensionContentTypeProvider _contentTypeProvider = new();

    private readonly MemoryCache _fileDetailsCache = new(new MemoryCacheOptions() { SizeLimit = 100000 }, loggerFactory);

    private readonly IThumbnailService _thumbnailService = thumbnailService;
    private bool disposedValue;

    private async Task<ResultWithStatus<FileUploadQueueFileResult?, ErrorResult>> QueueFile(ApplicationOption app, string fileprocess_id, string file_name, string? file_tag, IEntityClient ec, CancellationToken ct)
    {
        var newFileNameResult = fileStorage.GetNewFileName(ec, app, file_name);

        if (newFileNameResult.Status != null)
        {
            return new(Result: null, Status: newFileNameResult.Status);
        }

        if (newFileNameResult.Result == null)
        {
            return new(Result: null, Status: new(Error: "filename_error", ErrorDescription: "Error generating new file name"));
        }

        FileStore? fileStore = null;
        try
        {
            await ec.Connect(ct);

            fileStore = new FileStore(ec, schema_name: app.SchemaConfiguration.DDSchema);
            fileStore.Def.c_fileprocess_id.Value = fileprocess_id;
            fileStore.Def.vc_filename.Value = Path.GetFileName(file_name);
            fileStore.Def.vc_fileguid.Value = newFileNameResult.Result.newFileName;
            fileStore.Def.vc_filefolder.Value = newFileNameResult.Result.folder;
            fileStore.Def.c_filestoragetype_id.Value = app.FileStorageType ?? nameof(FileStorageTypes.LocalFileStorage);
            fileStore.Def.vc_file_tag.Value = file_tag;
            fileStore.Def.bi_filesize.Value = 0;
            await fileStore.InsertData(ct, true, _options);
            await fileStore.GetData(ct);
        }
        catch (Exception ex)
        {
            log.LogError("Error queuing file {file_name}, {ex}", file_name, ex);
            return new(Result: null, Status: new(Error: "queue_error", ErrorDescription: $"Error queuing file: {ex.Message}"));
        }
        finally
        {
            await ec.Disconnect();
        }

        return new(Result: new(new FileDetails
        {
            c_file_id = fileStore.Def.c_file_id.Value,
            c_fileprocess_id = fileStore.Def.c_fileprocess_id.Value,
            vc_filename = fileStore.Def.vc_filename.Value,
            vc_filefolder = fileStore.Def.vc_filefolder.Value,
            vc_fileguid = fileStore.Def.vc_fileguid.Value,
            bi_filesize = fileStore.Def.bi_filesize.Value,
            vc_file_tag = fileStore.Def.vc_file_tag.Value ?? "",
            c_fileuploadstatus_id = fileStore.Def.c_fileuploadstatus_id.Value,
            c_filestoragetype_id = fileStore.Def.c_filestoragetype_id.Value,
            fullPath = newFileNameResult.Result.fullPath
        }, newFileNameResult.Result, fileStore), Status: null);
    }

    private FileStream OpenReadFileStream(string path)
    {
        return new FileStream(
            path,
            FileMode.Open, FileAccess.Read, FileShare.Read,
            bufferSize: _cacheOptions.ReadBufferSize,
            options: FileOptions.SequentialScan | FileOptions.Asynchronous
            );
    }


    public async Task<UploadFileResult> UploadFile(ApplicationOption app, string fileprocess_id, string fileName, Stream fileData, string? file_tag, int? maxSize, int? quality, IEntityClient ec, CancellationToken ct)
    {
        string fullPath = string.Empty;
        try
        {
            // Queue the upload, upload status is pending
            var queue_result = await QueueFile(app, fileprocess_id, fileName, file_tag, ec, ct);

            if (queue_result.Status != null) return new() { ErrorMessage = queue_result.Status.ErrorDescription };
            if (queue_result.Result == null) return new() { ErrorMessage = "Error queuing file" };

            var file_details = queue_result.Result.details;
            var new_file_result = queue_result.Result.new_file_result;
            var fileStore = queue_result.Result.file_store;

            if (string.IsNullOrEmpty(file_details.fullPath)) return new() { ErrorMessage = "Error queuing file upload path" };

            fullPath = file_details.fullPath;

            // Change upload status to Uploading
            await FileStore.UpdateStatus(ec, app.SchemaConfiguration.DDSchema, file_details.c_file_id, nameof(FileUpload.Uploading), ct);

            // fileData stream is the Request body stream, which is not seekable and can only be read once.
            var store_result = await fileStorage.StoreFile(ec, app, file_details, fileData, ct);

            if (store_result.Status != null)
            {
                await FileStore.UpdateStatus(ec, app.SchemaConfiguration.DDSchema, file_details.c_file_id, nameof(FileUpload.Failed), ct);
                log.LogError("Error storing file {fileName} in storage service: {errorMessage}", fileName, store_result.Status);
                return new() { ErrorMessage = $"Error storing file: {store_result.Status}" };
            }

            var should_create_thumbnail = !string.IsNullOrWhiteSpace(new_file_result.extension) && _thumbnailService.IsImageSupported(new_file_result.extension);
            var is_sql_storage = app.FileStorageType == nameof(FileStorageTypes.SQLFileStorage);

            if (is_sql_storage)
            {
                // SQL Storage needs a stream to the uploaded file
                await using FileStream uploaded_file_stream = OpenReadFileStream(fullPath);

                var sql_result = await sqlStorage.StoreFile(ec, app, file_details, uploaded_file_stream, ct);

                if (sql_result.Status != null)
                {
                    await FileStore.UpdateStatus(ec, app.SchemaConfiguration.DDSchema, file_details.c_file_id, nameof(FileUpload.Failed), ct);
                    log.LogError("Error storing file {fileName} in SQL storage service: {errorMessage}", fullPath, sql_result.Status);
                    return new() { ErrorMessage = "Error uploading file to SQL storage" };
                }

            }

            await ec.Connect(ct);

            // MMC: update file size
            fileStore.Def.bi_filesize.Value = store_result.Result;
            await fileStore.UpdateData(ct, true);

            await FileStore.UpdateStatus(ec, app.SchemaConfiguration.DDSchema, fileStore.Def.c_file_id.Value, nameof(FileUpload.Uploaded), ct);

            await ec.Disconnect();

            // Generate thumbnail, skiasharp is synchronous
            if (should_create_thumbnail)
            {
                try
                {
                    string thumbnailPath = await Task.Run(async () =>
                    {
                        await using FileStream thumbnail_file_stream = new(fullPath, FileMode.Open, FileAccess.Read, FileShare.Read);

                        var thumb = _thumbnailService.CreateThumbnail(fullPath, thumbnail_file_stream, maxSize ?? default, quality ?? default);

                        if (is_sql_storage)
                        {
                            await thumbnail_file_stream.DisposeAsync();
                            File.Delete(fullPath);
                        }

                        return thumb;
                    });

                    log.LogDebug("Thumbnail created at {ThumbnailPath} for file {FullPath}", thumbnailPath, fullPath);
                }
                catch (Exception ex)
                {
                    log.LogError(ex, "Failed to create thumbnail for file {FullPath}", fullPath);
                }
            }
            else
            {
                if (is_sql_storage)
                {
                    File.Delete(fullPath);
                }
            }

            return new UploadFileResult() { FileId = fileStore.Def.c_file_id.Value, FileProcessId = fileStore.Def.c_fileprocess_id.Value!, FileGuid = fileStore.Def.vc_fileguid.Value };
        }
        catch (OperationCanceledException)
        {
            if (!string.IsNullOrEmpty(fullPath) && File.Exists(fullPath))
            {
                // Delete the file if the operation was canceled
                File.Delete(fullPath);
            }

            return new UploadFileResult() { ErrorMessage = "Operation canceled" };
        }
        catch
        {
            if (!string.IsNullOrEmpty(fullPath) && File.Exists(fullPath))
            {
                // Clean up the file in case of other exceptions
                File.Delete(fullPath);
            }

            throw;
        }
        finally
        {
            await ec.Disconnect();
        }
    }


    public async Task<FileDetails?> GetFileDetails(ApplicationOption app, string fileguid, IEntityClient ec, CancellationToken ct)
    {
        string cacheKey = $"FileStore_{fileguid}";

        if (_fileDetailsCache.TryGetValue(cacheKey, out FileDetails? cacheEntry))
        {
            return cacheEntry;
        }

        try
        {
            await ec.Connect(ct);

            var fileStore = new FileStore(ec, schema_name: app.SchemaConfiguration.DDSchema);
            fileStore.Def.fst_getByGUID.Parms[nameof(fileStore.Def.vc_fileguid)].ValueObject = fileguid;
            var fileDetails = await fileStore.ExecuteProcSingleRow<FileDetails>(fileStore.Def.fst_getByGUID, ct, set_parms_from_columns: false, mode: AutoMapperMode.ByNameLaxNotThrow);
            if (fileDetails != null)
            {
                if (fileDetails.c_fileuploadstatus_id == nameof(FileUpload.Uploaded))
                {
                    var uploadsPath = Path.Combine(_options.UploadsFolder!, app.ApplicationID, fileDetails.vc_filefolder);

                    var filePath = Path.GetFullPath(Path.Combine(uploadsPath, fileDetails.vc_fileguid));

                    // MMC: check for directory traversal attacks
                    if (!filePath.StartsWith(uploadsPath, StringComparison.OrdinalIgnoreCase))
                    {
                        return null;
                    }

                    fileDetails.fullPath = filePath;

                    var cacheEntryOptions = new MemoryCacheEntryOptions
                    {
                        Priority = CacheItemPriority.Normal,
                        Size = 1 // Each cache entry is counted as 1 unit towards the size limit
                    };

                    cacheEntry = fileDetails;
                    _fileDetailsCache.Set(cacheKey, cacheEntry, cacheEntryOptions);
                }
                else
                {
                    log.LogWarning("File {fileguid} not valid to serve. Status: {status}", fileguid, fileDetails.c_fileuploadstatus_id);
                }
            }

        }
        catch (Exception ex)
        {
            log.LogError(ex, "Error serving file {fileguid}", fileguid);
        }
        finally
        {
            await ec.Disconnect();
        }

        return cacheEntry;
    }

    public async Task<GetFileStreamResult?> ServeFile(ApplicationOption app, string fileguid, IEntityClient ec, CancellationToken ct)
    {
        GetFileStreamResult? result = null;

        var details = await GetFileDetails(app, fileguid, ec, ct);

        if (details != null)
        {
            if (details.c_filestoragetype_id == nameof(FileStorageTypes.SQLFileStorage))
            {
                result = await sqlStorage.GetFileStream(ec, app, details, ct);
            }
            else
            {
                result = await fileStorage.GetFileStream(ec, app, details, ct);
            }
        }

        return result;
    }

    public async Task<GetFileStreamResult?> ServeThumbnail(ApplicationOption app, string fileguid, int? maxSize, int? quality, IEntityClient ec, CancellationToken ct)
    {
        GetFileStreamResult? result = null;

        var file_details = await GetFileDetails(app, fileguid, ec, ct);
        if (file_details != null)
        {
            var thumb = _thumbnailService.GetThumbnailFilename(file_details.fullPath);

            if (File.Exists(thumb.fullDestinationPath))
            {
                if (!_contentTypeProvider.TryGetContentType(thumb.fullDestinationPath, out var contentType))
                {
                    contentType = "application/octet-stream"; // Default MIME type
                }

                result = new()
                {
                    ContentType = contentType,
                    Stream = OpenReadFileStream(thumb.fullDestinationPath)
                };
            }
            else
            {
                if (file_details.c_filestoragetype_id == nameof(FileStorageTypes.SQLFileStorage))
                {
                    result = await sqlStorage.GetFileStream(ec, app, file_details, ct);
                }
                else
                {
                    result = await fileStorage.GetFileStream(ec, app, file_details, ct);
                }
            }

        }

        return result;
    }

    public async Task<Stream?> GetFileStream(IEntityClient ec, ApplicationOption app, FileDetails fileDetails, CancellationToken ct)
    {
        GetFileStreamResult? result;

        if (fileDetails.c_filestoragetype_id == nameof(FileStorageTypes.SQLFileStorage))
        {
            result = await sqlStorage.GetFileStream(ec, app, fileDetails, ct);
        }
        else
        {
            result = await fileStorage.GetFileStream(ec, app, fileDetails, ct);
        }

        return result?.Stream;
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!disposedValue)
        {
            if (disposing)
            {
                _fileDetailsCache.Dispose();
            }

            // TODO: free unmanaged resources (unmanaged objects) and override finalizer

            disposedValue = true;
        }
    }

    public void Dispose()
    {
        // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }
}
