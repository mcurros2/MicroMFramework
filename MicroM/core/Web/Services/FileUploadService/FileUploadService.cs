using MicroM.Configuration;
using MicroM.Data;
using MicroM.DataDictionary;
using MicroM.DataDictionary.StatusDefs;
using MicroM.Extensions;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Globalization;

namespace MicroM.Web.Services
{
    /// <summary>
    /// Represents the FileUploadService.
    /// </summary>
    public class FileUploadService(IOptions<MicroMOptions> options, ILogger<FileUploadService> logger, ILoggerFactory loggerFactory, IThumbnailService thumbnailService) : IFileUploadService, IDisposable
    {
        private readonly MicroMOptions _options = options.Value;
        private readonly FileExtensionContentTypeProvider _contentTypeProvider = new();
        private readonly ILogger<FileUploadService> _log = logger;
        private readonly MemoryCache _cache = new MemoryCache(new MemoryCacheOptions() { SizeLimit = 100000 }, loggerFactory);
        private readonly IThumbnailService _thumbnailService = thumbnailService;
        private bool disposedValue;

        /// <summary>
        /// Queues metadata for an incoming file and prepares its destination path.
        /// </summary>
        /// <param name="app_id">Application identifier.</param>
        /// <param name="fileprocess_id">Process identifier associated with the file.</param>
        /// <param name="file_name">Original name of the uploaded file.</param>
        /// <param name="ec">Entity client used for database access.</param>
        /// <param name="ct">Token to observe for cancellation.</param>
        /// <returns>
        /// A tuple containing an error message, the <see cref="FileStore"/> record,
        /// the full file path, and the file extension.
        /// </returns>
        private async Task<(string? errorMessage, FileStore? file_store, string? fullPath, string? extension)>
            QueueFile(string app_id, string fileprocess_id, string file_name, IEntityClient ec, CancellationToken ct)
        {
            var (allowed, extension) = file_name.IsFileExtensionAllowed(_options.AllowedUploadFileExtensions ?? ConfigurationDefaults.AllowedFileUploadExtensions);

            if (!allowed)
            {
                return (errorMessage: "File type not allowed", file_store: null, fullPath: null, extension: null);
            }

            string newFileName = $"{Guid.NewGuid()}{extension}";
            string folder = DateTime.Now.ToString("yyyyMM", CultureInfo.InvariantCulture);
            var uploadsPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, app_id, _options.UploadsFolder ?? ConfigurationDefaults.UploadsFolder, folder);

            string fullPath = Path.Combine(uploadsPath, newFileName);

            // MMC: check for directory traversal attacks
            if (!fullPath.StartsWith(uploadsPath, StringComparison.OrdinalIgnoreCase))
            {
                return (errorMessage: "Invalid filename", file_store: null, fullPath: null, extension: null);
            }

            FileStore? fileStore = null;
            try
            {
                if (!Directory.Exists(uploadsPath))
                {
                    Directory.CreateDirectory(uploadsPath);
                }

                await ec.Connect(ct);

                fileStore = new FileStore(ec);
                fileStore.Def.c_fileprocess_id.Value = fileprocess_id;
                fileStore.Def.vc_filename.Value = Path.GetFileName(file_name);
                fileStore.Def.vc_fileguid.Value = newFileName;
                fileStore.Def.vc_filefolder.Value = folder;
                fileStore.Def.bi_filesize.Value = 0;
                await fileStore.InsertData(ct, true, _options);
                await fileStore.GetData(ct);


            }
            catch (Exception ex)
            {
                _log.LogError("Error queuing file {file_name}, {ex}", file_name, ex);
                return (errorMessage: $"Error queuing file: {ex.Message}", file_store: null, fullPath: null, extension: null);
            }
            finally
            {
                await ec.Disconnect();
            }

            return (errorMessage: null, file_store: fileStore, fullPath, extension);
        }

        /// <summary>
        /// Uploads a file to the configured storage and records it in the database.
        /// </summary>
        /// <param name="app_id">Application identifier.</param>
        /// <param name="fileprocess_id">Process identifier associated with the file.</param>
        /// <param name="fileName">Original file name.</param>
        /// <param name="fileData">Stream containing the file data.</param>
        /// <param name="maxSize">Optional maximum thumbnail size.</param>
        /// <param name="quality">Optional thumbnail quality.</param>
        /// <param name="ec">Entity client used for database access.</param>
        /// <param name="ct">Token to observe for cancellation.</param>
        /// <returns>Information about the stored file or an error message.</returns>
        /// <remarks>Throws <see cref="OperationCanceledException"/> when cancelled and may propagate I/O or database exceptions.</remarks>
        public async Task<UploadFileResult> UploadFile(string app_id, string fileprocess_id, string fileName, Stream fileData, int? maxSize, int? quality, IEntityClient ec, CancellationToken ct)
        {
            string fullPath = string.Empty;
            try
            {
                var result = await QueueFile(app_id, fileprocess_id, fileName, ec, ct);

                if (result.errorMessage != null)
                {
                    return new() { ErrorMessage = result.errorMessage };
                }

                if (result.file_store == null)
                {
                    return new() { ErrorMessage = "Error queuing file" };
                }

                if (string.IsNullOrEmpty(result.fullPath))
                {
                    return new() { ErrorMessage = "Error queuing file upload path" };
                }

                fullPath = result.fullPath;

                var fileStore = result.file_store;

                // MMC: change status to uploading
                await ec.Connect(ct);

                var fileStoreSatus = new FileStoreStatus(ec);
                fileStoreSatus.Def.c_file_id.Value = fileStore.Def.c_file_id.Value;
                fileStoreSatus.Def.c_status_id.Value = nameof(FileUpload);
                fileStoreSatus.Def.c_statusvalue_id.Value = nameof(FileUpload.Uploading);
                await fileStoreSatus.UpdateData(ct, true);

                // MMC: disconnect here as the upload process can take a long time
                await ec.Disconnect();

                using var fileStream = new FileStream(result.fullPath, FileMode.Create, FileAccess.Write, FileShare.None);
                await fileData.CopyToAsync(fileStream, ct);

                await fileStream.FlushAsync(ct);
                long fileSize = fileStream.Length;
                await fileStream.DisposeAsync();

                // MMC: connect again to update the status
                await ec.Connect(ct);
                fileStoreSatus.Def.c_status_id.Value = nameof(FileUpload);
                fileStoreSatus.Def.c_statusvalue_id.Value = nameof(FileUpload.Uploaded);
                await fileStoreSatus.UpdateData(ct, true);

                // MMC: update file size
                result.file_store.Def.bi_filesize.Value = fileSize;
                await result.file_store.UpdateData(ct, true);

                // Generate thumbnail, skiasharp is synchronous
                if (!string.IsNullOrWhiteSpace(result.extension) && _thumbnailService.IsImageSupported(result.extension))
                {
                    try
                    {
                        string thumbnailPath = await Task.Run(() =>
                        {
                            return _thumbnailService.CreateThumbnail(fullPath, maxSize ?? default, quality ?? default);
                        });

                        _log.LogInformation("Thumbnail created at {ThumbnailPath} for file {FullPath}", thumbnailPath, fullPath);
                    }
                    catch (Exception ex)
                    {
                        _log.LogError(ex, "Failed to create thumbnail for file {FullPath}", fullPath);
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


        /// <summary>
        /// Retrieves the full path of a stored file.
        /// </summary>
        /// <param name="app_id">Application identifier.</param>
        /// <param name="fileguid">Generated file name used for storage.</param>
        /// <param name="ec">Entity client used for database access.</param>
        /// <param name="ct">Token to observe for cancellation.</param>
        /// <returns>The full path to the file if found; otherwise <see langword="null"/>.</returns>
        /// <remarks>Throws <see cref="OperationCanceledException"/> when cancelled.</remarks>
        public async Task<string?> GetFilePath(string app_id, string fileguid, IEntityClient ec, CancellationToken ct)
        {
            string cacheKey = $"FileStore_{fileguid}";

            if (_cache.TryGetValue(cacheKey, out string? filePath))
            {
                return filePath;
            }

            try
            {
                await ec.Connect(ct);

                var fileStore = new FileStore(ec);
                fileStore.Def.fst_getByGUID.Parms[nameof(fileStore.Def.vc_fileguid)].ValueObject = fileguid;
                var fileDetails = await fileStore.ExecuteProcSingleRow<FileDetails>(ct, fileStore.Def.fst_getByGUID, set_parms_from_columns: false, mode: IEntityClient.AutoMapperMode.ByNameLaxNotThrow);
                if (fileDetails != null)
                {
                    if (fileDetails.c_fileuploadstatus_id == nameof(FileUpload.Uploaded))
                    {

                        var uploadsPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, app_id, _options.UploadsFolder ?? ConfigurationDefaults.UploadsFolder, fileDetails.vc_filefolder);

                        filePath = Path.Combine(uploadsPath, fileDetails.vc_fileguid);

                        // MMC: check for directory traversal attacks
                        if (!filePath.StartsWith(uploadsPath, StringComparison.OrdinalIgnoreCase))
                        {
                            return null;
                        }

                        // MMC: The cache should have as SizeLimit. When the limit is reached, the cache will remove the least recently used item as per default behavior.
                        // The removal is triggered when add/get/remove are called.
                        var cacheEntryOptions = new MemoryCacheEntryOptions
                        {
                            Priority = CacheItemPriority.Normal,
                            Size = 1
                        };

                        _cache.Set(cacheKey, filePath, cacheEntryOptions);
                    }
                    else
                    {
                        _log.LogWarning("File {fileguid} not valid to serve. Status: {status}", fileguid, fileDetails.c_fileuploadstatus_id);
                    }
                }

            }
            catch (Exception ex)
            {
                _log.LogError("Error serving file {fileguid}, {ex}", fileguid, ex);
            }
            finally
            {
                await ec.Disconnect();
            }

            return filePath;
        }

        /// <summary>
        /// Serves the requested file to the client.
        /// </summary>
        /// <param name="app_id">Application identifier.</param>
        /// <param name="fileguid">Generated file name used for storage.</param>
        /// <param name="ec">Entity client used for database access.</param>
        /// <param name="ct">Token to observe for cancellation.</param>
        /// <returns>A result containing the file stream and content type, if available.</returns>
        /// <remarks>Throws <see cref="OperationCanceledException"/> when cancelled.</remarks>
        public async Task<ServeFileResult?> ServeFile(string app_id, string fileguid, IEntityClient ec, CancellationToken ct)
        {
            ServeFileResult? result = null;

            var filePath = await GetFilePath(app_id, fileguid, ec, ct);
            if (filePath != null)
            {
                if (File.Exists(filePath))
                {
                    if (!_contentTypeProvider.TryGetContentType(filePath, out var contentType))
                    {
                        contentType = "application/octet-stream"; // Default MIME type
                    }

                    result = new() { ContentType = contentType, FileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read) };
                }

            }

            return result;
        }

        /// <summary>
        /// Serves a thumbnail for the requested file, generating one if necessary.
        /// </summary>
        /// <param name="app_id">Application identifier.</param>
        /// <param name="fileguid">Generated file name used for storage.</param>
        /// <param name="maxSize">Optional maximum size for generated thumbnail.</param>
        /// <param name="quality">Optional quality setting for generated thumbnail.</param>
        /// <param name="ec">Entity client used for database access.</param>
        /// <param name="ct">Token to observe for cancellation.</param>
        /// <returns>A result containing the thumbnail stream and content type, if available.</returns>
        /// <remarks>Throws <see cref="OperationCanceledException"/> when cancelled.</remarks>
        public async Task<ServeFileResult?> ServeThumbnail(string app_id, string fileguid, int? maxSize, int? quality, IEntityClient ec, CancellationToken ct)
        {
            ServeFileResult? result = null;

            var filePath = await GetFilePath(app_id, fileguid, ec, ct);
            if (filePath != null)
            {
                var thumb = _thumbnailService.GetThumbnailFilename(filePath);

                if (File.Exists(thumb.thumbnailFilePath))
                {
                    if (!_contentTypeProvider.TryGetContentType(thumb.thumbnailFilePath, out var contentType))
                    {
                        contentType = "application/octet-stream"; // Default MIME type
                    }

                    result = new() { ContentType = contentType, FileStream = new FileStream(thumb.thumbnailFilePath, FileMode.Open, FileAccess.Read, FileShare.Read) };
                }
                else if (File.Exists(filePath))
                {
                    if (!_contentTypeProvider.TryGetContentType(filePath, out var contentType))
                    {
                        contentType = "application/octet-stream"; // Default MIME type
                    }

                    result = new() { ContentType = contentType, FileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read) };
                }

            }

            return result;
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    _cache.Dispose();
                }

                // TODO: free unmanaged resources (unmanaged objects) and override finalizer
                
                disposedValue = true;
            }
        }

        /// <summary>
        /// Releases allocated resources.
        /// </summary>
        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}
