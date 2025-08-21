using MicroM.Configuration;
using MicroM.Data;
using MicroM.Web.Services;
using static MicroM.Data.IEntityClient;

namespace MicroM.Core
{
    /// <summary>
    /// Base class for all entities providing initialization and data operations.
    /// </summary>
    public abstract class EntityBase : InitBase
    {

        /// <summary>
        /// Gets the definition for this entity.
        /// </summary>
        public EntityDefinition Def { get; protected set; } = null!;


        private IEntityData? _data;
        protected IEntityData Data
        {
            get => _data ?? throw new ClassNotInitilizedException($"You should provide a {nameof(IEntityClient)} to perform data access operations.");
            set => _data = value;
        }

        /// <summary>
        /// Gets the <see cref="IEntityClient"/> used for data access.
        /// </summary>
        public IEntityClient Client => Data.EntityClient;

        /// <summary>
        /// Gets the encryptor associated with this entity, if any.
        /// </summary>
        public IMicroMEncryption? Encryptor => Data.Encryptor;

        /// <summary>
        /// Gets the collection of available actions.
        /// </summary>
        public Dictionary<string, Action> Actions { get; private set; } = new(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// Initializes the entity with the specified client and optional encryptor.
        /// </summary>
        /// <param name="ec">Entity client used for data operations.</param>
        /// <param name="encryptor">Optional encryption provider.</param>
        public virtual void Init(IEntityClient? ec, IMicroMEncryption? encryptor = null)
        {
            if (ec != null)
            {
                _data = new EntityData(ec, Def, encryptor);
                IsInitialized = true;
            }
        }

        /// <summary>
        /// Executes a registered action for this entity.
        /// </summary>
        /// <param name="action_name">Name of the action to execute.</param>
        /// <param name="args">Action arguments.</param>
        /// <param name="Options">Optional configuration.</param>
        /// <param name="API">Optional Web API services.</param>
        /// <param name="encryptor">Optional encryption provider.</param>
        /// <param name="ct">Cancellation token.</param>
        /// <param name="app_id">Optional application identifier.</param>
        /// <returns>The action result or null if not found.</returns>
        public async Task<EntityActionResult?> ExecuteAction(string action_name, DataWebAPIRequest args, MicroMOptions? Options, IWebAPIServices? API, IMicroMEncryption? encryptor, CancellationToken ct, string? app_id = null)
        {
            EntityActionResult? result = null;
            var act = Def.Actions[action_name];

            if (act != null)
            {
                result = (EntityActionResult?)await act.Execute(this, args, Def, Options, API, encryptor, ct, app_id);
            }

            return result;
        }

        #region "Data API"

        /// <summary>
        /// Deletes entity data using the configured definition.
        /// </summary>
        public virtual async Task<DBStatusResult> DeleteData(CancellationToken ct, bool throw_dbstat_exception = false, MicroMOptions? options = null, Dictionary<string, object>? server_claims = null, IWebAPIServices? api = null, string? app_id = null)
        {
            return await Data.DeleteData(ct, throw_dbstat_exception);
        }

        /// <summary>
        /// Executes a view and returns the resulting data set.
        /// </summary>
        public virtual async Task<List<DataResult>> ExecuteView(CancellationToken ct, ViewDefinition view, int? row_limit = null, MicroMOptions? options = null, Dictionary<string, object>? server_claims = null, IWebAPIServices? api = null, string? app_id = null)
        {
            return await Data.ExecuteView(ct, view, row_limit);
        }

        /// <summary>
        /// Retrieves entity data.
        /// </summary>
        public virtual async Task<bool> GetData(CancellationToken ct, MicroMOptions? options = null, Dictionary<string, object>? server_claims = null, IWebAPIServices? api = null, string? app_id = null)
        {
            return await Data.GetData(ct);
        }

        /// <summary>
        /// Retrieves entity data mapped to the specified type.
        /// </summary>
        public virtual async Task<T?> GetData<T>(CancellationToken ct, MicroMOptions? options = null, Dictionary<string, object>? server_claims = null, IWebAPIServices? api = null, AutoMapperMode mode = AutoMapperMode.ByNameLaxNotThrow, MapResult<T>? mapper = null, string? app_id = null) where T : class, new()
        {
            return await Data.GetData<T>(ct, mode, mapper);
        }

        /// <summary>
        /// Inserts entity data.
        /// </summary>
        public virtual async Task<DBStatusResult> InsertData(CancellationToken ct, bool throw_dbstat_exception = false, MicroMOptions? options = null, Dictionary<string, object>? server_claims = null, IWebAPIServices? api = null, string? app_id = null)
        {
            return await Data.InsertData(ct, throw_dbstat_exception);
        }

        /// <summary>
        /// Performs a lookup and returns the resulting value.
        /// </summary>
        public virtual async Task<string?> LookupData(CancellationToken ct, string? lookup_name = null, MicroMOptions? options = null, Dictionary<string, object>? server_claims = null, IWebAPIServices? api = null, string? app_id = null)
        {
            return await Data.LookupData(ct, lookup_name);
        }

        /// <summary>
        /// Updates entity data.
        /// </summary>
        public virtual async Task<DBStatusResult> UpdateData(CancellationToken ct, bool throw_dbstat_exception = false, MicroMOptions? options = null, Dictionary<string, object>? server_claims = null, IWebAPIServices? api = null, string? app_id = null)
        {
            return await Data.UpdateData(ct, throw_dbstat_exception);
        }

        /// <summary>
        /// Executes a procedure that returns a <see cref="DBStatusResult"/>.
        /// </summary>
        public virtual async Task<DBStatusResult> ExecuteProcessDBStatus(CancellationToken ct, ProcedureDefinition proc, bool set_parms_from_columns = true, bool throw_dbstat_exception = false, MicroMOptions? options = null, Dictionary<string, object>? server_claims = null, IWebAPIServices? api = null, string? app_id = null)
        {
            return await Data.ExecuteProcDBStatus(ct, proc, set_parms_from_columns, throw_dbstat_exception);
        }

        /// <summary>
        /// Executes a procedure and returns a list of <see cref="DataResult"/>.
        /// </summary>
        public virtual async Task<List<DataResult>> ExecuteProc(CancellationToken ct, ProcedureDefinition proc, int row_limit = 0, bool set_parms_from_columns = true, MicroMOptions? options = null, Dictionary<string, object>? server_claims = null, IWebAPIServices? api = null, string? app_id = null)
        {
            return await Data.ExecuteProc(ct, proc, row_limit, set_parms_from_columns);
        }

        /// <summary>
        /// Executes a procedure and maps the results to the specified type.
        /// </summary>
        public virtual async Task<List<T>> ExecuteProc<T>(CancellationToken ct, ProcedureDefinition proc, int row_limit = 0, bool set_parms_from_columns = true, IEntityClient.AutoMapperMode mode = IEntityClient.AutoMapperMode.ByName, IEntityClient.MapResult<T>? mapper = null, MicroMOptions? options = null, Dictionary<string, object>? server_claims = null, IWebAPIServices? api = null, string? app_id = null) where T : class, new()
        {
            return await Data.ExecuteProc<T>(ct, proc, row_limit, set_parms_from_columns, mode, mapper);
        }

        /// <summary>
        /// Executes a procedure and returns a single column value.
        /// </summary>
        public virtual async Task<T?> ExecuteProcSingleColumn<T>(CancellationToken ct, ProcedureDefinition proc, int row_limit = 0, bool set_parms_from_columns = true, MicroMOptions? options = null, Dictionary<string, object>? server_claims = null, IWebAPIServices? api = null, string? app_id = null)
        {
            return await Data.ExecuteProcSingleColumn<T>(ct, proc, row_limit, set_parms_from_columns);
        }

        /// <summary>
        /// Executes a procedure and returns a single row mapped to the specified type.
        /// </summary>
        public virtual async Task<T?> ExecuteProcSingleRow<T>(CancellationToken ct, ProcedureDefinition proc, bool set_parms_from_columns = true, IEntityClient.AutoMapperMode mode = IEntityClient.AutoMapperMode.ByName, IEntityClient.MapResult<T>? mapper = null, MicroMOptions? options = null, Dictionary<string, object>? server_claims = null, IWebAPIServices? api = null, string? app_id = null) where T : class, new()
        {
            return await Data.ExecuteProcSingleRow<T>(ct, proc, set_parms_from_columns, mode, mapper);
        }

        #endregion

    }
}
