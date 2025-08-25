using MicroM.Data;

namespace MicroM.Web.Services
{
    /// <summary>
    /// Represents the IFileUploadService.
    /// </summary>
    public interface IFileUploadService
    {
        //public Task<UploadFileResult> QueueFile(string app_id, string fileprocess_id, string file_name, Stream fileData, IEntityClient ec, CancellationToken ct);

        /// <summary>
        /// Uploads a file and records it in the database.
        /// </summary>
        Task<UploadFileResult> UploadFile(string app_id, string fileprocess_id, string file_name, Stream fileData, int? maxSize, int? quality, IEntityClient ec, CancellationToken ct);
        /// <summary>
        /// Serves a stored file to the client.
        /// </summary>
        Task<ServeFileResult?> ServeFile(string app_id, string fileguid, IEntityClient ec, CancellationToken ct);
        /// <summary>
        /// Retrieves the full path of a stored file.
        /// </summary>
        Task<string?> GetFilePath(string app_id, string fileguid, IEntityClient ec, CancellationToken ct);
        /// <summary>
        /// Serves a thumbnail for the specified file.
        /// </summary>
        Task<ServeFileResult?> ServeThumbnail(string app_id, string fileguid, int? maxSize, int? quality, IEntityClient ec, CancellationToken ct);

    }
}
