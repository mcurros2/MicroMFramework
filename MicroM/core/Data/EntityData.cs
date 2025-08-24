using MicroM.Configuration;
using MicroM.Core;
using MicroM.Extensions;
using MicroM.Web.Services;
using System.Data;
using static MicroM.Data.IEntityClient;

namespace MicroM.Data
{
    /// <summary>
    /// Provides common operations for interacting with entity data and procedures.
    /// </summary>
    public class EntityData(IEntityClient ec, EntityDefinition def, IMicroMEncryption? encryptor = null) : IEntityData
    {
        public IEntityClient EntityClient { get; init; } = ec;

        private readonly EntityDefinition def = def;

        public IMicroMEncryption? Encryptor => encryptor;

        protected List<DBStatus> ReadStatus(List<DataResult> results, bool set_autonum = false)
        {
            bool failed = false;
            List<DBStatus> ret = [];
            foreach (object?[] record in results[0].records)
            {

                var stat = new DBStatus((DBStatusCodes)record[0]!, (string)record[1]!);
                if (stat.Status == DBStatusCodes.Error) failed = true;
                ret.Add(stat);
            }
            if (!failed)
            {
                if (set_autonum) SetAutonum(ret);
            }
            return ret;
        }

        protected void SetAutonum(List<DBStatus> status)
        {
            foreach (var status_item in status)
            {
                if (status_item.Status == DBStatusCodes.Autonum)
                {
                    var cols = def.Columns.GetWithFlags(ColumnFlags.Autonum);
                    foreach (var col in cols)
                    {
                        col.ValueObject = status_item.Message;
                        return;
                    }
                }
            }
        }

        private async Task<DBStatusResult> ExecuteStatusData(string proc_name, IEnumerable<ColumnBase> parms, CancellationToken ct, bool throw_dbstat_exception = false)
        {
            bool failed = false;
            bool autonum = false;

            var dbstats = await EntityClient.ExecuteSP<DBStatus>(proc_name, ct, parms: parms,
                mapper: async (IGetFieldValue fv, string[] headers, CancellationToken ct) =>
                {
                    var status = new DBStatus((DBStatusCodes)await fv.GetFieldValueAsync<int>(0, ct), await fv.GetFieldValueAsync<string>(1, ct));
                    if (status.Status.IsIn(DBStatusCodes.Error, DBStatusCodes.RecordHasChanged)) failed = true;
                    if (status.Status == DBStatusCodes.Autonum)
                    {
                        if (def.AutonumColumn != null)
                        {
                            def.AutonumColumn.ValueObject = status.Message;
                            autonum = true;
                        }
                        else
                        {
                            if (throw_dbstat_exception) throw new InvalidOperationException($"Autonum returned from {proc_name} but no autonum column defined in EntityDefinition.");
                            else status = new DBStatus(DBStatusCodes.Error, $"Autonum returned from {proc_name} but no autonum column defined in EntityDefinition.");
                        }
                    }
                    return status;
                });

            if (throw_dbstat_exception && failed) throw new DataAbstractionException($"Error while updating '{def.TableName}' with Mneo '{def.Mneo}'", dbstats);
            return new() { Failed = failed, AutonumReturned = autonum, Results = dbstats }; ;

        }


        /// <summary>
        /// Updates a record for an entity. This method will execute the XXXX_update stored procedure for the entity.
        /// If the client is not connected to the database server, it will open a new connection.
        /// </summary>
        /// <param name="ct"></param>
        /// <param name="throw_dbstat_exception"></param>
        /// <returns></returns>
        /// <exception cref="DataAbstractionException"></exception>
        public async Task<DBStatusResult> UpdateData(CancellationToken ct, bool throw_dbstat_exception = false)
        {
            var parms = def.Columns.GetParmsWithFlags(ColumnFlags.Update);
            if (Encryptor != null) parms.EncryptColumnData(Encryptor);
            return await ExecuteStatusData($"{def.Mneo}_update", parms, ct, throw_dbstat_exception);
        }

        /// <summary>
        /// Creates a record for an entity. This method will execute the XXXX_update stored procedure for the entity, with a null last update timestamp.
        /// If the client is not connected to the database server, it will open a new connection.
        /// </summary>
        /// <param name="ct"></param>
        /// <param name="throw_dbstat_exception"></param>
        /// <returns></returns>
        /// <exception cref="DataAbstractionException"></exception>
        public async Task<DBStatusResult> InsertData(CancellationToken ct, bool throw_dbstat_exception = false)
        {
            var parms = def.Columns.GetParmsWithFlags(ColumnFlags.Insert);
            if (Encryptor != null) parms.EncryptColumnData(Encryptor);
            return await ExecuteStatusData($"{def.Mneo}_update", parms, ct, throw_dbstat_exception);
        }

        /// <summary>
        /// Deletes a record for an entity. This method will execute the XXXX_drop stored procedure for the entity.
        /// If the client is not connected to the database server, it will open a new connection.
        /// </summary>
        /// <param name="ct"></param>
        /// <param name="throw_dbstat_exception"></param>
        /// <returns></returns>
        /// <exception cref="DataAbstractionException"></exception>
        public async Task<DBStatusResult> DeleteData(CancellationToken ct, bool throw_dbstat_exception = false)
        {
            var parms = def.Columns.GetParmsWithFlags(ColumnFlags.Delete);
            return await ExecuteStatusData($"{def.Mneo}_drop", parms, ct, throw_dbstat_exception);
        }

        /// <summary>
        /// Reads a record for an entity and places the values in each column definition. This method will execute XXXX_get stored procedure for the entity.
        /// If the client is not connected to the database server, it will open a new connection.
        /// </summary>
        /// <param name="ct"></param>
        /// <returns></returns>
        /// <exception cref="InvalidOperationException">is thrown if the number of columns returned by the XXXX_get stored procedure is not equal to the number of columns defined in <see cref="EntityDefinition"/></exception>
        public async Task<bool> GetData(CancellationToken ct)
        {
            var parms = def.Columns.GetParmsWithFlags(ColumnFlags.Get);
            var result = await EntityClient.ExecuteSP($"{def.Mneo}_get", parms, ct);

            bool ret = MapGetColumns(result);

            return ret;
        }

        public bool MapGetColumns(List<DataResult>? result)
        {
            bool ret = false;

            if (result.HasData())
            {
                var cols = def.Columns.GetWithFlags(ColumnFlags.All, ColumnFlags.None, [nameof(DefaultColumns.webusr)]);
                if (result![0].Header.Length != cols.Count)
                {
                    throw new InvalidOperationException($"{def.Mneo}_get: the number of columns returned is not equal to the number of columns defined in EntityDefinition.");
                }

                int r = 0;
                foreach (var col in cols.Values)
                {
                    col.ValueObject = result[0].records[0][r++];
                }
                if (Encryptor != null) cols.Values.DecryptColumnData(Encryptor);
                ret = true;

            }

            return ret;
        }

        /// <summary>
        /// Reads a record for an entity and maps the result to a T with default mapping <seealso cref="AutoMapperMode.ByNameLaxNotThrow"/>. This method will execute XXXX_get stored procedure for the entity.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="ct"></param>
        /// <param name="mode"></param>
        /// <param name="mapper"></param>
        /// <returns></returns>
        public async Task<T?> GetData<T>(CancellationToken ct, AutoMapperMode mode = AutoMapperMode.ByNameLaxNotThrow, MapResult<T>? mapper = null) where T : class, new()
        {
            var parms = def.Columns.GetParmsWithFlags(ColumnFlags.Get);

            var result = await EntityClient.ExecuteSP<T>($"{def.Mneo}_get", parms: parms, mode: mode, mapper: mapper, ct: ct);

            return result.FirstOrDefault();
        }


        /// <summary>
        /// Gets the description for an entity. This method will execute XXXX_lookup stored procedure for the entity.
        /// If the client is not connected to the database server, it will open a new connection.
        /// </summary>
        /// <param name="ct"></param>
        /// <param name="lookup_name"></param>
        /// <returns>The string representing the description for a record of the entity</returns>
        public async Task<string?> LookupData(CancellationToken ct, string? lookup_name = null)
        {
            string? lkp_proc = lookup_name;
            IEnumerable<ColumnBase> parms;

            // MMC: if there is no special lookup procedure defined, use the default lookup procedure
            if (string.IsNullOrEmpty(lkp_proc))
            {
                lkp_proc = $"{def.Mneo}{nameof(DefaultProcedureNames._lookup)}";
                // MMC: check to see if the lookup procedure has been defined
                def.Procs.TryGetValue(lkp_proc, out ProcedureDefinition? proc);
                if (proc == null)
                {
                    // MMC: if not defined, use the columns defined with get flag as parms for the lookup, the columns will have the values set already
                    parms = def.Columns.GetParmsWithFlags(ColumnFlags.Get);
                }
                else
                {
                    proc.SetParmsValues(def.Columns);
                    parms = proc.Parms.Values;
                }
            }
            else
            {
                def.Procs.TryGetValue(lkp_proc, out ProcedureDefinition? proc);
                if (proc == null) throw new ArgumentException($"The lookup procedure {lkp_proc} has not been defined for entity {def.Mneo}");
                if (!proc.isLookup) throw new ArgumentException($"The lookup procedure {lkp_proc} is not a lookup procedure");

                proc.SetParmsValues(def.Columns);
                parms = proc.Parms.Values;
            }

            return await EntityClient.ExecuteSPSingleColumn<string>(lkp_proc, ct, parms);
        }

        /// <summary>
        /// Executes the specified <paramref name="view"/> for an entity. This method will execute stored procedure view for the entity.
        /// If the client is not connected to the database server, it will open a new connection.
        /// </summary>
        /// <param name="ct"></param>
        /// <param name="view"></param>
        /// <param name="row_limit"></param>
        /// <returns></returns>
        public async Task<List<DataResult>> ExecuteView(CancellationToken ct, ViewDefinition view, int? row_limit = null)
        {
            row_limit ??= DataDefaults.DefaultRowLimitForViews;
            return await ExecuteProc(ct, view.Proc, (int)row_limit);
        }

        /// <summary>
        /// Executes the specified <paramref name="proc"/> for an entity. This method will execute stored procedure specified for the entity.
        /// If the client is not connected to the database server, it will open a new connection.
        /// </summary>
        /// <param name="ct"></param>
        /// <param name="proc"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentException">Throws this exception if <paramref name="row_limit"/> is greater than 0 and the stored procedure definition <paramref name="proc"/> hasn´t been defined for ReadOnlyLocks</exception>
        public async Task<DBStatusResult> ExecuteProcDBStatus(CancellationToken ct, ProcedureDefinition proc, bool set_parms_from_columns = true, bool throw_dbstat_exception = false)
        {
            //proc.Parms.TryGetValue(SystemColumnNames.webusr, out ColumnBase? webusr);
            //if (webusr != null) webusr.ValueObject = EntityClient.WebUser;

            bool should_close = (EntityClient.ConnectionState != ConnectionState.Open);
            await EntityClient.Connect(ct);

            DBStatusResult? result;
            try
            {
                if (proc.ReadonlyLocks)
                {
                    if (EntityClient.isTransactionOpen) throw new InvalidOperationException($"The stored procedure {proc.Name} has {nameof(proc.ReadonlyLocks)} true and is not supported inside transactions.");
                    await EntityClient.ExecuteSQLNonQuery($"set transaction isolation level read uncommitted;", ct);
                }
                if (set_parms_from_columns) proc.SetParmsValues(def.Columns);

                result = await ExecuteStatusData(proc.Name, proc.Parms.Values, ct, throw_dbstat_exception);
            }
            finally
            {
                if (proc.ReadonlyLocks)
                {
                    await EntityClient.ExecuteSQLNonQuery($"set transaction isolation level read committed;", ct);
                }
            }

            if (should_close) await EntityClient.Disconnect();

            return result;
        }

        /// <summary>
        /// Executes the specified <paramref name="proc"/> for an entity. This method will execute stored procedure specified for the entity.
        /// If the client is not connected to the database server, it will open a new connection.
        /// </summary>
        /// <param name="ct"></param>
        /// <param name="proc"></param>
        /// <param name="row_limit">Limits the number of rows returned</param>
        /// <returns></returns>
        /// <exception cref="ArgumentException">Throws this exception if <paramref name="row_limit"/> is greater than 0 and the stored procedure definition <paramref name="proc"/> hasn´t been defined for ReadOnlyLocks</exception>
        public async Task<List<DataResult>> ExecuteProc(CancellationToken ct, ProcedureDefinition proc, int row_limit = 0, bool set_parms_from_columns = true)
        {
            if (row_limit != 0 && proc.ReadonlyLocks == false) throw new ArgumentException($"Procedure {proc.Name} is defined with {nameof(proc.ReadonlyLocks)} false and cannot specify a {nameof(row_limit)} at execution");

            proc.Parms.TryGetValue(SystemColumnNames.webusr, out ColumnBase? webusr);
            if (webusr != null) webusr.ValueObject = EntityClient.WebUser;

            List<DataResult> result;

            bool should_close = (EntityClient.ConnectionState != ConnectionState.Open);

            await EntityClient.Connect(ct);

            try
            {
                if (proc.ReadonlyLocks)
                {
                    if (EntityClient.isTransactionOpen) throw new InvalidOperationException($"The stored procedure {proc.Name} has {nameof(proc.ReadonlyLocks)} true and is not supported inside transactions.");
                    await EntityClient.ExecuteSQLNonQuery($"set transaction isolation level read uncommitted; set rowcount {row_limit};", ct);
                }

                if (set_parms_from_columns) proc.SetParmsValues(def.Columns);

                result = await EntityClient.ExecuteSP(proc.Name, proc.Parms.Values, ct);
            }
            finally
            {
                if (proc.ReadonlyLocks)
                {
                    await EntityClient.ExecuteSQLNonQuery($"set transaction isolation level read committed; set rowcount 0;", ct);
                }
            }

            if (should_close) await EntityClient.Disconnect();

            return result;
        }

        public async Task<T?> ExecuteProcSingleRow<T>(CancellationToken ct, ProcedureDefinition proc, bool set_parms_from_columns = true, AutoMapperMode mode = AutoMapperMode.ByName, MapResult<T>? mapper = null) where T : class, new()
        {
            T? result = null;

            var result_list = await ExecuteProc<T>(ct, proc, 0, set_parms_from_columns, mode, mapper);
            if (result_list?.Count > 0) result = result_list[0];

            return result;
        }

        public async Task<List<T>> ExecuteProc<T>(CancellationToken ct, ProcedureDefinition proc, int row_limit = 0, bool set_parms_from_columns = true, AutoMapperMode mode = AutoMapperMode.ByName, MapResult<T>? mapper = null) where T : class, new()
        {
            if (row_limit != 0 && proc.ReadonlyLocks == false) throw new ArgumentException($"Procedure {proc.Name} is defined with {nameof(proc.ReadonlyLocks)} false and cannot specify a {nameof(row_limit)} at execution");

            proc.Parms.TryGetValue(SystemColumnNames.webusr, out ColumnBase? webusr);
            if (webusr != null) webusr.ValueObject = EntityClient.WebUser;

            List<T> result = [];

            bool should_close = (EntityClient.ConnectionState != ConnectionState.Open);
            await EntityClient.Connect(ct);

            try
            {
                if (proc.ReadonlyLocks)
                {
                    if (EntityClient.isTransactionOpen) throw new InvalidOperationException($"The stored procedure {proc.Name} has {nameof(proc.ReadonlyLocks)} true and is not supported inside transactions.");
                    await EntityClient.ExecuteSQLNonQuery($"set transaction isolation level read uncommitted; set rowcount {row_limit};", ct);
                }

                if (set_parms_from_columns) proc.SetParmsValues(def.Columns);

                result = await EntityClient.ExecuteSP<T>(proc.Name, ct, mode, proc.Parms.Values, mapper);
            }
            finally
            {
                if (proc.ReadonlyLocks)
                {
                    await EntityClient.ExecuteSQLNonQuery($"set transaction isolation level read committed; set rowcount 0;", ct);
                }
            }

            if (should_close) await EntityClient.Disconnect();

            return result;
        }

        public async Task<T?> ExecuteProcSingleColumn<T>(CancellationToken ct, ProcedureDefinition proc, int row_limit = 0, bool set_parms_from_columns = true)
        {
            if (row_limit != 0 && proc.ReadonlyLocks == false) throw new ArgumentException($"Procedure {proc.Name} is defined with {nameof(proc.ReadonlyLocks)} false and cannot specify a {nameof(row_limit)} at execution");

            proc.Parms.TryGetValue(SystemColumnNames.webusr, out ColumnBase? webusr);
            if (webusr != null) webusr.ValueObject = EntityClient.WebUser;

            T? result;

            bool should_close = (EntityClient.ConnectionState != ConnectionState.Open);
            await EntityClient.Connect(ct);

            try
            {
                if (proc.ReadonlyLocks)
                {
                    if (EntityClient.isTransactionOpen) throw new InvalidOperationException($"The stored procedure {proc.Name} has {nameof(proc.ReadonlyLocks)} true and is not supported inside transactions.");
                    await EntityClient.ExecuteSQLNonQuery($"set transaction isolation level read uncommitted; set rowcount {row_limit};", ct);
                }

                if (set_parms_from_columns) proc.SetParmsValues(def.Columns);

                result = await EntityClient.ExecuteSPSingleColumn<T>(proc.Name, ct, proc.Parms.Values);
            }
            finally
            {
                if (proc.ReadonlyLocks)
                {
                    await EntityClient.ExecuteSQLNonQuery($"set transaction isolation level read committed; set rowcount 0;", ct);
                }
            }

            if (should_close) await EntityClient.Disconnect();

            return result;
        }

    }
}
