using MicroM.Configuration;
using MicroM.Data;
using MicroM.Extensions;
using MicroM.Web.Services;
using static MicroM.Data.IEntityClient;

namespace MicroM.Core;

public abstract class EntityBase : InitBase
{

    public EntityDefinition Def { get; protected set; } = null!;


    private IEntityData? _data;
    protected IEntityData Data
    {
        get => _data ?? throw new ClassNotInitilizedException($"You should provide a {nameof(IEntityClient)} to perform data access operations.");
        set => _data = value;
    }

    public IEntityClient Client => Data.EntityClient;
    public IMicroMEncryption? Encryptor => Data.Encryptor;

    public Dictionary<string, Action> Actions { get; private set; } = new(StringComparer.OrdinalIgnoreCase);

    public virtual void Init(IEntityClient? ec, IMicroMEncryption? encryptor = null, string? schema_name = null)
    {
        if (Def.SchemaName.IsNullOrEmpty()) Def.SchemaName = schema_name;
        if (ec != null)
        {
            _data = new EntityData(ec, Def, encryptor);
            IsInitialized = true;
        }
    }

    public async Task<EntityActionResult?> ExecuteAction(string action_name, DataWebAPIRequest args, MicroMOptions? Options, IWebAPIServices? API, IMicroMEncryption? encryptor, CancellationToken ct, string? app_id = null)
    {
        EntityActionResult? result = null;
        var act = Def.Actions[action_name];

        if (act != null)
        {
            result = (EntityActionResult?)await act.Execute(this, args, Def, Options, API, encryptor, app_id, ct);
        }

        return result;
    }

    #region "Data API"

    public virtual async Task<DBStatusResult> DeleteData(CancellationToken ct, bool throw_dbstat_exception = false, MicroMOptions? options = null, Dictionary<string, object>? server_claims = null, IWebAPIServices? api = null, string? app_id = null)
    {
        return await Data.DeleteData(ct, throw_dbstat_exception);
    }

    public virtual async Task<List<DataResult>> ExecuteView(ViewDefinition view, CancellationToken ct, int? row_limit = null, MicroMOptions? options = null, Dictionary<string, object>? server_claims = null, IWebAPIServices? api = null, string? app_id = null)
    {
        return await Data.ExecuteView(view, ct, row_limit);
    }

    public virtual async Task ExecuteViewChannel(ViewDefinition view, DataResultSetChannel result_channel, CancellationToken ct, int? row_limit = null, MicroMOptions? options = null, Dictionary<string, object>? server_claims = null, IWebAPIServices? api = null, string? app_id = null, int? records_channel_capacity = null, bool complete_channel = true, int? max_allowed_rows = null)
    {
        await Data.ExecuteViewChannel(view, result_channel, ct, row_limit, records_channel_capacity, complete_channel, max_allowed_rows);
    }

    public virtual async Task<bool> GetData(CancellationToken ct, MicroMOptions? options = null, Dictionary<string, object>? server_claims = null, IWebAPIServices? api = null, string? app_id = null)
    {
        return await Data.GetData(ct);
    }

    public virtual async Task<T?> GetData<T>(CancellationToken ct, MicroMOptions? options = null, Dictionary<string, object>? server_claims = null, IWebAPIServices? api = null, AutoMapperMode mode = AutoMapperMode.ByNameLaxNotThrow, MapResult<T>? mapper = null, string? app_id = null) where T : class, new()
    {
        return await Data.GetData<T>(ct, mode, mapper);
    }

    public virtual async Task<DBStatusResult> InsertData(CancellationToken ct, bool throw_dbstat_exception = false, MicroMOptions? options = null, Dictionary<string, object>? server_claims = null, IWebAPIServices? api = null, string? app_id = null)
    {
        return await Data.InsertData(ct, throw_dbstat_exception);
    }

    public virtual async Task<string?> LookupData(CancellationToken ct, string? lookup_name = null, MicroMOptions? options = null, Dictionary<string, object>? server_claims = null, IWebAPIServices? api = null, string? app_id = null)
    {
        return await Data.LookupData(ct, lookup_name);
    }

    public virtual async Task<DBStatusResult> UpdateData(CancellationToken ct, bool throw_dbstat_exception = false, MicroMOptions? options = null, Dictionary<string, object>? server_claims = null, IWebAPIServices? api = null, string? app_id = null)
    {
        return await Data.UpdateData(ct, throw_dbstat_exception);
    }

    public virtual async Task<DBStatusResult> ExecuteProcessDBStatus(ProcedureDefinition proc, CancellationToken ct, bool set_parms_from_columns = true, bool throw_dbstat_exception = false, MicroMOptions? options = null, Dictionary<string, object>? server_claims = null, IWebAPIServices? api = null, string? app_id = null)
    {
        return await Data.ExecuteProcDBStatus(proc, ct, set_parms_from_columns, throw_dbstat_exception);
    }

    public virtual async Task<List<DataResult>> ExecuteProc(ProcedureDefinition proc, CancellationToken ct, int row_limit = 0, bool set_parms_from_columns = true, MicroMOptions? options = null, Dictionary<string, object>? server_claims = null, IWebAPIServices? api = null, string? app_id = null)
    {
        return await Data.ExecuteProc(proc, ct, row_limit, set_parms_from_columns);
    }

    public virtual async Task ExecuteProcChannel(ProcedureDefinition proc, DataResultSetChannel result_channel, CancellationToken ct, int row_limit = 0, bool set_parms_from_columns = true, MicroMOptions? options = null, Dictionary<string, object>? server_claims = null, IWebAPIServices? api = null, string? app_id = null, int? records_channel_capacity = null, bool complete_channel = true, int? max_allowed_rows = null)
    {
        await Data.ExecuteProcChannel(proc, result_channel, ct, row_limit, set_parms_from_columns, records_channel_capacity, complete_channel, max_allowed_rows);
    }

    public virtual async Task<List<T>> ExecuteProc<T>(ProcedureDefinition proc, CancellationToken ct, int row_limit = 0, bool set_parms_from_columns = true, AutoMapperMode mode = AutoMapperMode.ByName, MapResult<T>? mapper = null, MicroMOptions? options = null, Dictionary<string, object>? server_claims = null, IWebAPIServices? api = null, string? app_id = null) where T : class, new()
    {
        return await Data.ExecuteProc<T>(proc, ct, row_limit, set_parms_from_columns, mode, mapper);
    }

    public virtual async Task<T?> ExecuteProcSingleColumn<T>(ProcedureDefinition proc, CancellationToken ct, int row_limit = 0, bool set_parms_from_columns = true, MicroMOptions? options = null, Dictionary<string, object>? server_claims = null, IWebAPIServices? api = null, string? app_id = null)
    {
        return await Data.ExecuteProcSingleColumn<T>(proc, ct, row_limit, set_parms_from_columns);
    }

    public virtual async Task<T?> ExecuteProcSingleRow<T>(ProcedureDefinition proc, CancellationToken ct, bool set_parms_from_columns = true, AutoMapperMode mode = AutoMapperMode.ByName, MapResult<T>? mapper = null, MicroMOptions? options = null, Dictionary<string, object>? server_claims = null, IWebAPIServices? api = null, string? app_id = null) where T : class, new()
    {
        return await Data.ExecuteProcSingleRow<T>(proc, ct, set_parms_from_columns, mode, mapper);
    }

    #endregion

}
