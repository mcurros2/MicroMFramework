using MicroM.Web.Services;
using static MicroM.Data.IEntityClient;

namespace MicroM.Data
{
    public interface IEntityData
    {
        IEntityClient EntityClient { get; }

        IMicroMEncryption? Encryptor { get; }

        Task<DBStatusResult> DeleteData(CancellationToken ct, bool throw_dbstat_exception = false);
        Task<List<DataResult>> ExecuteProc(ProcedureDefinition proc, CancellationToken ct, int row_limit = 0, bool set_parms_from_columns = true);
        Task<List<T>> ExecuteProc<T>(ProcedureDefinition proc, CancellationToken ct, int row_limit = 0, bool set_parms_from_columns = true, AutoMapperMode mode = AutoMapperMode.ByName, MapResult<T>? mapper = null) where T : class, new();
        Task<DBStatusResult> ExecuteProcDBStatus(ProcedureDefinition proc, CancellationToken ct, bool set_parms_from_columns = true, bool throw_dbstat_exception = false);
        Task<T?> ExecuteProcSingleColumn<T>(ProcedureDefinition proc, CancellationToken ct, int row_limit = 0, bool set_parms_from_columns = true);
        Task<T?> ExecuteProcSingleRow<T>(ProcedureDefinition proc, CancellationToken ct, bool set_parms_from_columns = true, AutoMapperMode mode = AutoMapperMode.ByName, MapResult<T>? mapper = null) where T : class, new();
        Task<List<DataResult>> ExecuteView(ViewDefinition view, CancellationToken ct, int? row_limit = null);
        Task<bool> GetData(CancellationToken ct);
        bool MapGetColumns(List<DataResult>? result);
        Task<DBStatusResult> InsertData(CancellationToken ct, bool throw_dbstat_exception = false);
        Task<string?> LookupData(CancellationToken ct, string? lookup_name = null);
        Task<DBStatusResult> UpdateData(CancellationToken ct, bool throw_dbstat_exception = false);
        Task<T?> GetData<T>(CancellationToken ct, AutoMapperMode mode = AutoMapperMode.ByNameLaxNotThrow, MapResult<T>? mapper = null) where T : class, new();
        Task ExecuteProcChannel(ProcedureDefinition proc, DataResultSetChannel result_channel, CancellationToken ct, int row_limit = 0, bool set_parms_from_columns = true, int? records_channel_capacity = null, bool complete_channel = true, int? max_allowed_rows = null);
        Task ExecuteViewChannel(ViewDefinition view, DataResultSetChannel result_channel, CancellationToken ct, int? row_limit = null, int? records_channel_capacity = null, bool complete_channel = true, int? max_allowed_rows = null);
    }
}