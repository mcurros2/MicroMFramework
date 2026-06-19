using MicroM.Configuration;
using MicroM.Data;

namespace MicroM.Web.Services;

public interface IFileUploadService
{
    Task<UploadFileResult> UploadFile(ApplicationOption app, string fileprocess_id, string file_name, Stream fileData, string? file_tag, int? maxSize, int? quality, IEntityClient ec, CancellationToken ct);
    Task<GetFileStreamResult?> ServeFile(ApplicationOption app, string fileguid, IEntityClient ec, CancellationToken ct);
    Task<FileDetails?> GetFileDetails(ApplicationOption app, string fileguid, IEntityClient ec, CancellationToken ct);
    Task<GetFileStreamResult?> ServeThumbnail(ApplicationOption app, string fileguid, int? maxSize, int? quality, IEntityClient ec, CancellationToken ct);

    Task<Stream?> GetFileStream(IEntityClient ec, ApplicationOption app, FileDetails fileDetails, CancellationToken ct);
}
