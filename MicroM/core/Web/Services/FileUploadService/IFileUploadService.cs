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
        /// Performs the UploadFile operation.
        /// </summary>
        public Task<UploadFileResult> UploadFile(string app_id, string fileprocess_id, string file_name, Stream fileData, int? maxSize, int? quality, IEntityClient ec, CancellationToken ct);
        /// <summary>
        /// Performs the ServeFile operation.
        /// </summary>
        public Task<ServeFileResult?> ServeFile(string app_id, string fileguid, IEntityClient ec, CancellationToken ct);
        /// <summary>
        /// Performs the GetFilePath operation.
        /// </summary>
        public Task<string?> GetFilePath(string app_id, string fileguid, IEntityClient ec, CancellationToken ct);
        /// <summary>
        /// Performs the ServeThumbnail operation.
        /// </summary>
        public Task<ServeFileResult?> ServeThumbnail(string app_id, string fileguid, int? maxSize, int? quality, IEntityClient ec, CancellationToken ct);

    }
}
