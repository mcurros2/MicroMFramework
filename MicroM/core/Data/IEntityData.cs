using MicroM.Web.Services;
using static MicroM.Data.IEntityClient;

namespace MicroM.Data
{
    /// <summary>
    /// Defines operations for manipulating entity data through stored procedures and views.
    /// </summary>
    public interface IEntityData
    {
        /// <summary>Gets the entity client performing database operations.</summary>
        IEntityClient EntityClient { get; }

        /// <summary>Gets the optional encryption service.</summary>
        IMicroMEncryption? Encryptor { get; }

        /// <summary>Deletes a record for the entity.</summary>
        Task<DBStatusResult> DeleteData(CancellationToken ct, bool throw_dbstat_exception = false);

        /// <summary>Executes a stored procedure and returns raw results.</summary>
        Task<List<DataResult>> ExecuteProc(CancellationToken ct, ProcedureDefinition proc, int row_limit = 0, bool set_parms_from_columns = true);

        /// <summary>Executes a stored procedure and maps results to type <typeparamref name="T"/>.</summary>
        Task<List<T>> ExecuteProc<T>(CancellationToken ct, ProcedureDefinition proc, int row_limit = 0, bool set_parms_from_columns = true, IEntityClient.AutoMapperMode mode = IEntityClient.AutoMapperMode.ByName, IEntityClient.MapResult<T>? mapper = null) where T : class, new();

        /// <summary>Executes a stored procedure and returns status information.</summary>
        Task<DBStatusResult> ExecuteProcDBStatus(CancellationToken ct, ProcedureDefinition proc, bool set_parms_from_columns = true, bool throw_dbstat_exception = false);

        /// <summary>Executes a stored procedure returning a single column value.</summary>
        Task<T?> ExecuteProcSingleColumn<T>(CancellationToken ct, ProcedureDefinition proc, int row_limit = 0, bool set_parms_from_columns = true);

        /// <summary>Executes a stored procedure returning a single row mapped to <typeparamref name="T"/>.</summary>
        Task<T?> ExecuteProcSingleRow<T>(CancellationToken ct, ProcedureDefinition proc, bool set_parms_from_columns = true, IEntityClient.AutoMapperMode mode = IEntityClient.AutoMapperMode.ByName, IEntityClient.MapResult<T>? mapper = null) where T : class, new();

        /// <summary>Executes a view definition.</summary>
        Task<List<DataResult>> ExecuteView(CancellationToken ct, ViewDefinition view, int? row_limit = null);

        /// <summary>Retrieves data for the entity.</summary>
        Task<bool> GetData(CancellationToken ct);

        /// <summary>Maps columns returned by a GET operation to the entity definition.</summary>
        bool MapGetColumns(List<DataResult>? result);

        /// <summary>Inserts a new record for the entity.</summary>
        Task<DBStatusResult> InsertData(CancellationToken ct, bool throw_dbstat_exception = false);

        /// <summary>Performs a lookup using the configured foreign keys.</summary>
        Task<string?> LookupData(CancellationToken ct, string? lookup_name = null);

        /// <summary>Updates a record for the entity.</summary>
        Task<DBStatusResult> UpdateData(CancellationToken ct, bool throw_dbstat_exception = false);

        /// <summary>Gets data and maps it to <typeparamref name="T"/>.</summary>
        Task<T?> GetData<T>(CancellationToken ct, AutoMapperMode mode = AutoMapperMode.ByNameLaxNotThrow, MapResult<T>? mapper = null) where T : class, new();

    }
}

