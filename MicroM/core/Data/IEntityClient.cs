
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

        Task<bool> Connect(CancellationToken ct, bool throw_exception = true, bool rollback_on_errors = true, bool isolation_level_read_committed = true);
        Task Disconnect();

        public enum AutoMapperMode
        {
            /// <summary>
            /// This mode maps the header names from the query results to existing properties in the mapped object.
            /// It is case sensitive and it will throw an exception if a property of the mapped object is missing from the headers.
            /// </summary>
            ByName,
            /// <summary>
            /// This mode maps the header names from the query results to existing properties in the mapped object.
            /// If column names contain spaces, spaces will be replaced by underscores.
            /// It is case sensitive and it will throw an exception if a property of the mapped object is missing from the headers.
            /// </summary>
            ByNameSpacesToUnderscore,
            /// <summary>
            /// This mode maps the header names from the query results to existing properties in the mapped object.
            /// It will not throw an exception if a property of the mapped object has not been returned from the query.
            /// Option returned headers must become nullable properties in the mapped object or it will throw a null reference exception
            /// </summary>
            ByNameLaxNotThrow,
            /// <summary>
            /// This mode maps the returned values from the query results to existing properties in the mapped object by position.
            /// It will loop through the objects properties in declared order and assing each value it the order returned by the query
            /// </summary>
            ByPosition
        }

        public IEntityClient Clone(string new_server = "", string new_db = "", string new_user = "", string new_password = "", int connection_timeout_secs = -1);


        /// <summary>
        /// Delegate to provide custom mapping.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="record">Will be injected in the call. <see cref="IGetFieldValue"/></param>
        /// <param name="headers">Will be injected in the call, contains the names for the result of the query</param>
        /// <param name="ct"></param>
        /// <returns></returns>
        public delegate Task<T> MapResult<T>(IGetFieldValue record, string[] headers, CancellationToken ct);

        Task<List<DataResult>> ExecuteSP(string sp_name, IEnumerable<ColumnBase>? parms, CancellationToken ct);

        Task<List<T>> ExecuteSP<T>(string sp_name, CancellationToken ct, AutoMapperMode mode = AutoMapperMode.ByName, IEnumerable<ColumnBase>? parms = null, MapResult<T>? mapper = null) where T : class, new();
        Task<T?> ExecuteSPSingleColumn<T>(string sp_name, CancellationToken ct, IEnumerable<ColumnBase>? parms = null);

        Task ExecuteSPChannel(string sp_name, IEnumerable<ColumnBase>? parms, DataResultSetChannel result, CancellationToken ct);
        Task ExecuteSPNonQuery(string sp_name, IEnumerable<ColumnBase>? parms, CancellationToken ct);

        Task<List<DataResult>> ExecuteSQL(string sql_text, CancellationToken ct);

        Task<List<T>> ExecuteSQL<T>(string sql_text, CancellationToken ct, AutoMapperMode mode = AutoMapperMode.ByName, MapResult<T>? mapper = null) where T : class, new();

        Task<T?> ExecuteSQLSingleColumn<T>(string sql_text, CancellationToken ct, IEnumerable<ColumnBase>? parms = null);

        Task ExecuteSQLChannel(string sql_text, DataResultSetChannel result, CancellationToken ct);
        Task ExecuteSQLNonQuery(string sql_text, CancellationToken ct);
        Task ExecuteSQLNonQuery(List<string> sql_scripts, CancellationToken ct);

    }
}