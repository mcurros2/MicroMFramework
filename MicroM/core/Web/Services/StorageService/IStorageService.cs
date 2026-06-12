using MicroM.Configuration;
using MicroM.Core;
using MicroM.Data;

namespace MicroM.Web.Services;

public interface IStorageService<T>
{
    public ResultWithStatus<NewFileNameResult, ErrorResult> GetNewFileName(IEntityClient ec, ApplicationOption app, string file_name);

    public Task<ResultWithStatus<long, ErrorResult>> StoreFile(IEntityClient ec, ApplicationOption app, string fullPath, Stream filestream, CancellationToken ct);

    public Task<GetFileStreamResult?> GetFileStream(IEntityClient ec, ApplicationOption app, FileDetails fileDetails, CancellationToken ct);

}
