using MicroM.Data;

namespace MicroM.Web.Services
{
    public interface IFileUploadService
    {
        //public Task<UploadFileResult> QueueFile(string app_id, string fileprocess_id, string file_name, Stream fileData, IEntityClient ec, CancellationToken ct);

        public Task<UploadFileResult> UploadFile(string app_id, string fileprocess_id, string file_name, Stream fileData, int? maxSize, int? quality, IEntityClient ec, CancellationToken ct);
        public Task<ServeFileResult?> ServeFile(string app_id, string fileguid, IEntityClient ec, CancellationToken ct);
        public Task<string?> GetFilePath(string app_id, string fileguid, IEntityClient ec, CancellationToken ct);
        public Task<ServeFileResult?> ServeThumbnail(string app_id, string fileguid, int? maxSize, int? quality, IEntityClient ec, CancellationToken ct);

    }
}
