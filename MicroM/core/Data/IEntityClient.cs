
using System.Data;

namespace MicroM.Data
{
    public interface IEntityClient : IDisposable
    {
        ConnectionState ConnectionState { get; }
        string ConnectionString { get; }
        int ConnectionTimeout { get; }
        string CurrentLanguage { get; set; }
        string DB { get; set; }
        bool IntegratedSecurity { get; set; }
        string Password { get; set; }

        bool Pooling { get; set; }
        int MinPoolSize { get; set; }
        int MaxPoolSize { get; set; }

        string WorkstationID { get; set; }
        string ApplicationName { get; set; }

        string Server { get; set; }
        string User { get; set; }
        string HTTPService { get; set; }
        string WebUser { get; }

        string MasterDatabase { get; }

        bool isTransactionOpen { get; }

        Dictionary<string, object>? ServerClaims { get; }

        public void OverrideColumnValues(IEnumerable<ColumnBase> parms);

        Task BeginTransaction(CancellationToken ct);
        Task CommitTransaction(CancellationToken ct);
        Task RollbackTransaction(CancellationToken ct);

        Task<bool> Connect(CancellationToken ct, bool throw_exception = true, bool rollback_on_errors = true, bool isolation_level_read_committed = true, bool set_nocount_on = true);
        Task Disconnect();

        public IEntityClient Clone(string new_server = "", string new_db = "", string new_user = "", string new_password = "", int connection_timeout_secs = -1);

        /// <summary>
        /// Delegate to provide custom mapping.
        /// </summary>
        public delegate Task<T> MapResult<T>(IValueReader record, string[] headers, string[] typeInfo, CancellationToken ct);

        Task<List<DataResult>> ExecuteSP(string sp_name, IEnumerable<ColumnBase>? parms, CancellationToken ct);

        Task<List<T>> ExecuteSP<T>(string sp_name, CancellationToken ct, AutoMapperMode mode = AutoMapperMode.ByName, IEnumerable<ColumnBase>? parms = null, MapResult<T>? mapper = null) where T : class, new();
        Task<T?> ExecuteSPSingleColumn<T>(string sp_name, CancellationToken ct, IEnumerable<ColumnBase>? parms = null);

        Task ExecuteSPNonQuery(string sp_name, IEnumerable<ColumnBase>? parms, CancellationToken ct);

        Task<List<DataResult>> ExecuteSQL(string sql_text, CancellationToken ct);

        Task<List<T>> ExecuteSQL<T>(string sql_text, CancellationToken ct, AutoMapperMode mode = AutoMapperMode.ByName, MapResult<T>? mapper = null) where T : class, new();

        Task<T?> ExecuteSQLSingleColumn<T>(string sql_text, CancellationToken ct, IEnumerable<ColumnBase>? parms = null);

        Task ExecuteSQLChannel(string sql_text, DataResultSetChannel result, int records_channel_capacity, CancellationToken ct);
        Task ExecuteSPChannel(string sp_name, IEnumerable<ColumnBase>? parms, DataResultSetChannel result, int records_channel_capacity, CancellationToken ct);

        Task ExecuteSQLNonQuery(string sql_text, CancellationToken ct);
        Task ExecuteSQLNonQuery(List<string> sql_scripts, CancellationToken ct);

    }
}