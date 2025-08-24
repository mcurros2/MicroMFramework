
using System.Data;

namespace MicroM.Data
{
    /// <summary>
    /// Defines database operations for entity-based clients.
    /// </summary>
    public interface IEntityClient : IDisposable
    {
        /// <summary>Gets the current connection state.</summary>
        ConnectionState ConnectionState { get; }

        /// <summary>Gets the connection string.</summary>
        string ConnectionString { get; }

        /// <summary>Gets the configured connection timeout in seconds.</summary>
        int ConnectionTimeout { get; }

        /// <summary>Gets or sets the current language for the session.</summary>
        string CurrentLanguage { get; set; }

        /// <summary>Gets or sets the database name.</summary>
        string DB { get; set; }

        /// <summary>Gets or sets a value indicating whether integrated security is used.</summary>
        bool IntegratedSecurity { get; set; }

        /// <summary>Gets or sets the password for the connection.</summary>
        string Password { get; set; }

        /// <summary>Gets or sets whether connection pooling is enabled.</summary>
        bool Pooling { get; set; }

        /// <summary>Gets or sets the minimum size of the connection pool.</summary>
        int MinPoolSize { get; set; }

        /// <summary>Gets or sets the maximum size of the connection pool.</summary>
        int MaxPoolSize { get; set; }

        /// <summary>Gets or sets the workstation identifier.</summary>
        string WorkstationID { get; set; }

        /// <summary>Gets or sets the application name.</summary>
        string ApplicationName { get; set; }

        /// <summary>Gets or sets the server name.</summary>
        string Server { get; set; }

        /// <summary>Gets or sets the user name.</summary>
        string User { get; set; }

        /// <summary>Gets or sets the HTTP service endpoint.</summary>
        string HTTPService { get; set; }

        /// <summary>Gets the authenticated web user.</summary>
        string WebUser { get; }

        /// <summary>Gets the name of the master database.</summary>
        string MasterDatabase { get; }

        /// <summary>Gets a value indicating whether a transaction is currently open.</summary>
        bool isTransactionOpen { get; }

        /// <summary>Gets the server-side claims for the current user.</summary>
        Dictionary<string, object>? ServerClaims { get; }

        /// <summary>
        /// Overrides parameter values with the provided column collection.
        /// </summary>
        /// <param name="parms">Columns whose values override defaults.</param>
        public void OverrideColumnValues(IEnumerable<ColumnBase> parms);

        /// <summary>Begins a database transaction.</summary>
        Task BeginTransaction(CancellationToken ct);

        /// <summary>Commits the current transaction.</summary>
        Task CommitTransaction(CancellationToken ct);

        /// <summary>Rolls back the current transaction.</summary>
        Task RollbackTransaction(CancellationToken ct);

        /// <summary>
        /// Opens the database connection.
        /// </summary>
        /// <param name="ct">Cancellation token.</param>
        /// <param name="throw_exception">Whether to throw on errors.</param>
        /// <param name="rollback_on_errors">Rollback transaction on error.</param>
        /// <param name="isolation_level_read_committed">Use read-committed isolation level.</param>
        /// <param name="set_nocount_on">Set NOCOUNT ON.</param>
        /// <returns>True if the connection was opened.</returns>
        Task<bool> Connect(CancellationToken ct, bool throw_exception = true, bool rollback_on_errors = true, bool isolation_level_read_committed = true, bool set_nocount_on = true);

        /// <summary>Closes the database connection.</summary>
        Task Disconnect();

        /// <summary>
        /// Specifies how query results are mapped to objects.
        /// </summary>
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
            /// It will loop through the objects properties in declared order and assing each value in the order returned by the query
            /// </summary>
            ByPosition
        }

        /// <summary>
        /// Creates a shallow clone of the client with new connection parameters.
        /// </summary>
        /// <param name="new_server">Optional new server.</param>
        /// <param name="new_db">Optional new database.</param>
        /// <param name="new_user">Optional new user.</param>
        /// <param name="new_password">Optional new password.</param>
        /// <param name="connection_timeout_secs">Optional timeout override.</param>
        /// <returns>A new <see cref="IEntityClient"/> instance.</returns>
        public IEntityClient Clone(string new_server = "", string new_db = "", string new_user = "", string new_password = "", int connection_timeout_secs = -1);

        /// <summary>
        /// Delegate to provide custom mapping.
        /// </summary>
        /// <typeparam name="T">Target type.</typeparam>
        /// <param name="record">Source record.</param>
        /// <param name="headers">Query result headers.</param>
        /// <param name="ct">Cancellation token.</param>
        /// <returns>The mapped instance.</returns>
        public delegate Task<T> MapResult<T>(IGetFieldValue record, string[] headers, CancellationToken ct);

        /// <summary>Executes a stored procedure and returns the results.</summary>
        Task<List<DataResult>> ExecuteSP(string sp_name, IEnumerable<ColumnBase>? parms, CancellationToken ct);

        /// <summary>Executes a stored procedure and maps results to the specified type.</summary>
        Task<List<T>> ExecuteSP<T>(string sp_name, CancellationToken ct, AutoMapperMode mode = AutoMapperMode.ByName, IEnumerable<ColumnBase>? parms = null, MapResult<T>? mapper = null) where T : class, new();

        /// <summary>Executes a stored procedure and returns a single column.</summary>
        Task<T?> ExecuteSPSingleColumn<T>(string sp_name, CancellationToken ct, IEnumerable<ColumnBase>? parms = null);

        /// <summary>Executes a stored procedure and streams results to a channel.</summary>
        Task ExecuteSPChannel(string sp_name, IEnumerable<ColumnBase>? parms, DataResultSetChannel result, CancellationToken ct);

        /// <summary>Executes a stored procedure that does not return results.</summary>
        Task ExecuteSPNonQuery(string sp_name, IEnumerable<ColumnBase>? parms, CancellationToken ct);

        /// <summary>Executes raw SQL and returns the results.</summary>
        Task<List<DataResult>> ExecuteSQL(string sql_text, CancellationToken ct);

        /// <summary>Executes raw SQL and maps results to the specified type.</summary>
        Task<List<T>> ExecuteSQL<T>(string sql_text, CancellationToken ct, AutoMapperMode mode = AutoMapperMode.ByName, MapResult<T>? mapper = null) where T : class, new();

        /// <summary>Executes raw SQL returning a single column.</summary>
        Task<T?> ExecuteSQLSingleColumn<T>(string sql_text, CancellationToken ct, IEnumerable<ColumnBase>? parms = null);

        /// <summary>Executes SQL and streams results to a channel.</summary>
        Task ExecuteSQLChannel(string sql_text, DataResultSetChannel result, CancellationToken ct);

        /// <summary>Executes SQL that does not return results.</summary>
        Task ExecuteSQLNonQuery(string sql_text, CancellationToken ct);

        /// <summary>Executes multiple SQL non-query commands.</summary>
        Task ExecuteSQLNonQuery(List<string> sql_scripts, CancellationToken ct);

    }
}