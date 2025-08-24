using MicroM.Web.Services;
using static MicroM.Data.IEntityClient;

namespace MicroM.Data
{
    public interface IEntityData
    {
        IEntityClient EntityClient { get; }

        IMicroMEncryption? Encryptor { get; }
       
        Task<DBStatusResult> DeleteData(CancellationToken ct, bool throw_dbstat_exception = false);
        Task<List<DataResult>> ExecuteProc(CancellationToken ct, ProcedureDefinition proc, int row_limit = 0, bool set_parms_from_columns = true);
        Task<List<T>> ExecuteProc<T>(CancellationToken ct, ProcedureDefinition proc, int row_limit = 0, bool set_parms_from_columns = true, IEntityClient.AutoMapperMode mode = IEntityClient.AutoMapperMode.ByName, IEntityClient.MapResult<T>? mapper = null) where T : class, new();
        Task<DBStatusResult> ExecuteProcDBStatus(CancellationToken ct, ProcedureDefinition proc, bool set_parms_from_columns = true, bool throw_dbstat_exception = false);
        Task<T?> ExecuteProcSingleColumn<T>(CancellationToken ct, ProcedureDefinition proc, int row_limit = 0, bool set_parms_from_columns = true);
        Task<T?> ExecuteProcSingleRow<T>(CancellationToken ct, ProcedureDefinition proc, bool set_parms_from_columns = true, IEntityClient.AutoMapperMode mode = IEntityClient.AutoMapperMode.ByName, IEntityClient.MapResult<T>? mapper = null) where T : class, new();
        Task<List<DataResult>> ExecuteView(CancellationToken ct, ViewDefinition view, int? row_limit = null);
        Task<bool> GetData(CancellationToken ct);
        bool MapGetColumns(List<DataResult>? result);
        Task<DBStatusResult> InsertData(CancellationToken ct, bool throw_dbstat_exception = false);
        Task<string?> LookupData(CancellationToken ct, string? lookup_name = null);
        Task<DBStatusResult> UpdateData(CancellationToken ct, bool throw_dbstat_exception = false);
        Task<T?> GetData<T>(CancellationToken ct, AutoMapperMode mode = AutoMapperMode.ByNameLaxNotThrow, MapResult<T>? mapper = null) where T : class, new();

    }
}