using MicroM.Configuration;
using MicroM.Extensions;
using MicroM.Web.Authentication;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using System.Data;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using static MicroM.Data.IEntityClient;

namespace MicroM.Data;

public class DatabaseClient : IDisposable, IAsyncDisposable, IEntityClient
{
    private bool disposedValue;

    private SqlConnection sql_connection = null!;
    private readonly SqlConnectionStringBuilder connection_builder = [];
    private SqlTransaction? sql_transaction;

    public string WebUser { get; } = "";
    public int QueryTimeout;

    #region "Connection Builder Mapped Properties"

    public string ConnectionString { get => connection_builder.ConnectionString; }

    public string MasterDatabase => "master";

    public string Server
    {
        get => connection_builder.DataSource;
        set => connection_builder.DataSource = value;
    }

    public string DB
    {
        get => connection_builder.InitialCatalog;
        set => connection_builder.InitialCatalog = value;
    }

    public string User
    {
        get => connection_builder.UserID;
        set => connection_builder.UserID = value;
    }

    public string Password
    {
        get => connection_builder.Password;
        set => connection_builder.Password = value;
    }

    public bool IntegratedSecurity
    {
        get => connection_builder.IntegratedSecurity;
        set => connection_builder.IntegratedSecurity = value;
    }

    public bool Pooling
    {
        get => connection_builder.Pooling;
        set => connection_builder.Pooling = value;
    }

    public int MinPoolSize
    {
        get => connection_builder.MinPoolSize;
        set => connection_builder.MinPoolSize = value;
    }

    public int MaxPoolSize
    {
        get => connection_builder.MaxPoolSize;
        set => connection_builder.MaxPoolSize = value;
    }

    public string WorkstationID
    {
        get => connection_builder.WorkstationID;
        set => connection_builder.WorkstationID = value.Truncate(128);
    }

    public string ApplicationName
    {
        get => connection_builder.ApplicationName;
        set => connection_builder.ApplicationName = value.Truncate(128);
    }

    public string CurrentLanguage
    {
        get => connection_builder.CurrentLanguage;
        set => connection_builder.CurrentLanguage = value;
    }

    public SqlConnectionEncryptOption Encryption
    {
        get => connection_builder.Encrypt;
        set => connection_builder.Encrypt = value;
    }

    public SqlConnectionStringBuilder SQLConnectionSB { get => connection_builder; }

    #endregion

    #region "SQLConnection Mapped Properties"
    public int ConnectionTimeout { get => sql_connection.ConnectionTimeout; }

    public ConnectionState ConnectionState { get => sql_connection.State; }

    public string HTTPService { get => ""; set => throw new NotImplementedException(); }

    #endregion

    #region "Server Claims"

    public Dictionary<string, object>? ServerClaims { get; } = null;

    public void OverrideColumnValues(IEnumerable<ColumnBase> parms)
    {
        if (ServerClaims != null)
        {
            foreach (var parm in parms)
            {
                if (!string.IsNullOrEmpty(parm.OverrideWith))
                {
                    if (ServerClaims.TryGetValue(parm.OverrideWith, out object? value))
                    {
                        parm.ValueObject = value;
                    }
                    else
                    {
                        parm.ValueObject = null;
                    }
                }
            }
        }
    }

    #endregion

    #region "Constructors"

    private void Init(string server, string db, string user = "", string password = "", bool integrated_security = false, int connection_timeout_secs = -1)
    {
        Server = server;
        DB = db;
        User = user;
        Password = password;
        IntegratedSecurity = (string.IsNullOrEmpty(user) && string.IsNullOrEmpty(password)) || integrated_security;
        connection_builder.ConnectTimeout = (connection_timeout_secs == -1) ? DataDefaults.DefaultConnectionTimeOutInSecs : connection_timeout_secs;
        connection_builder.Encrypt = SqlConnectionEncryptOption.Optional;
        sql_connection = new SqlConnection(ConnectionString);
    }

    private readonly ILogger? _logger;

    public DatabaseClient(string server, string db, string user = "", string password = "", bool integrated_security = false, int connection_timeout_secs = -1, ILogger? logger = null, Dictionary<string, object>? server_claims = null)
    {
        ServerClaims = server_claims;
        WebUser = server_claims != null && server_claims.TryGetValue(nameof(MicroMServerClaimTypes.MicroMUsername), out var webUser) ? (string)webUser : "";
        _logger = logger;
        Init(server, db, user, password, integrated_security, connection_timeout_secs);
    }

    public DatabaseClient(DatabaseClient dbc, string new_server = "", string new_db = "", int connection_timeout_secs = -1, ILogger? logger = null, Dictionary<string, object>? server_claims = null)
    {
        ServerClaims = server_claims;
        WebUser = server_claims != null && server_claims.TryGetValue(nameof(MicroMServerClaimTypes.MicroMUsername), out var webUser) ? (string)webUser : "";
        new_server = string.IsNullOrEmpty(new_server) ? dbc.Server : new_server;
        new_db = string.IsNullOrEmpty(new_db) ? dbc.DB : new_db;
        _logger = logger;
        Init(new_server, new_db, dbc.User, dbc.Password, dbc.IntegratedSecurity, connection_timeout_secs);
    }

    public IEntityClient Clone(string new_server = "", string new_db = "", string new_user = "", string new_password = "", int connection_timeout_secs = -1)
    {
        return new DatabaseClient(
            new_server.IfNullOrEmpty(this.Server)
            , new_db.IfNullOrEmpty(this.DB)
            , new_user.IfNullOrEmpty(this.User)
            , new_password.IfNullOrEmpty(this.Password)
            , string.IsNullOrEmpty(new_password.IfNullOrEmpty(this.Password))
            , connection_timeout_secs, _logger
            , ServerClaims
            )
        {
            Pooling = this.Pooling,
            MinPoolSize = this.MinPoolSize,
            MaxPoolSize = this.MaxPoolSize,
            WorkstationID = this.WorkstationID,
            ApplicationName = this.ApplicationName
        };
    }

    #endregion

    #region "Methods"

    /// <summary>
    /// Opens a connection to the server. If the connection is already opened, returns without error
    /// If the connection state is not Closed, it closes the connection and opens a new one.
    /// </summary>
    /// <returns>True if the connection was already opened</returns>
    /// <exception cref="DataAbstractionException"></exception>
    public async Task<bool> Connect(CancellationToken ct, bool throw_exception = true, bool rollback_on_errors = true, bool isolation_level_read_committed = true, bool set_nocount_on = true)
    {

        if (sql_connection.State == ConnectionState.Open) return true;
        if (sql_connection.State != ConnectionState.Closed) await sql_connection.CloseAsync();
        sql_connection.ConnectionString = connection_builder.ConnectionString;
        try
        {
            _logger?.LogTrace("Connecting to {Server}, DB {DB}, User {User}, Integrated Security {IntegratedSecurity}, Web User {WebUsr}", Server, DB, User, IntegratedSecurity, WebUser);
            await sql_connection.OpenAsync(ct);

            // Set initial options. When using connection pooling is important to revert any settings to this options
            string initial_options = $"{rollback_on_errors.True("SET XACT_ABORT ON; ")}{isolation_level_read_committed.True("SET TRANSACTION ISOLATION LEVEL READ COMMITTED; ")}{set_nocount_on.True("SET NOCOUNT ON;")}";

            if (!string.IsNullOrEmpty(initial_options))
            {
                // When connection pooling is enabled you can get an open transaction from the pool. Those should be closed early at Disconnect(), but re handled here for safety
                await ExecuteNonQuery(CommandType.Text, $"{initial_options}{(Pooling ? " if @@trancount > 0 ROLLBACK;" : "")}", ct);
            }
            return true;
        }
        catch (Exception x) when (x is not TaskCanceledException)
        {
            if (throw_exception) throw new DataAbstractionException($"An error ocurred while connecting: {x.Message}.", x);
            return false;
        }
    }

    private async Task RollbackUnexpectedOpenTransactions()
    {
        if (sql_transaction != null)
        {
            _logger?.LogWarning("An open transaction found when disconnecting. {Server}, DB {DB}, User {User}, Integrated Security {IntegratedSecurity}, Web User {WebUsr}", Server, DB, User, IntegratedSecurity, WebUser);
            try
            {
                await sql_transaction.RollbackAsync();
                await sql_transaction.DisposeAsync();
                sql_transaction = null;
            }
            catch
            {
            }
        }

        if (sql_connection.State != ConnectionState.Open) return;

        // When connection pooling is enabled, unproperly closed connections may leave an open transaction
        if (Pooling)
        {
            await ExecuteNonQuery(CommandType.Text, "if @@trancount > 0 ROLLBACK", CancellationToken.None);
        }
    }

    /// <summary>
    /// Closes a connection to the server.
    /// </summary>
    public async Task Disconnect()
    {
        try
        {
            _logger?.LogTrace("Disconnecting from {Server}, DB {DB}, User {User}, Integrated Security {IntegratedSecurity}, Web User {WebUsr}", Server, DB, User, IntegratedSecurity, WebUser);
            await RollbackUnexpectedOpenTransactions();
            await sql_connection.CloseAsync();
        }
        catch (Exception x) when (x is not TaskCanceledException)
        {
            throw new DataAbstractionException($"An error ocurred while disconnecting: {x.Message}.", x);
        }
    }

    /// <summary>
    /// Returs true if there is an open transaction
    /// </summary>
    /// <returns></returns>
    public bool isTransactionOpen => (sql_transaction != null);

    /// <summary>
    /// Creates a transaction. If a transaction is already created, throw an exception
    /// </summary>
    /// <param name="ct"></param>
    public async Task BeginTransaction(CancellationToken ct)
    {
        if (sql_transaction != null) throw new DataAbstractionException("A transaction is already created. Nested transactions are not supported");
        try
        {
            _logger?.LogTrace("Begin transaction. Server: {Server}, DB {DB}, User {User}, Integrated Security {IntegratedSecurity}, Web User {WebUsr}", Server, DB, User, IntegratedSecurity, WebUser);
            sql_transaction = (SqlTransaction)await sql_connection.BeginTransactionAsync(ct);
        }
        catch (Exception x) when (x is not TaskCanceledException)
        {
            throw new DataAbstractionException($"An error ocurred while creating a transaction: {x.Message}.", x);
        }
    }

    /// <summary>
    /// Reverts a transaction. If there is no open transaction, it throws an exception.
    /// </summary>
    /// <param name="ct"></param>
    public async Task RollbackTransaction(CancellationToken ct)
    {
        if (sql_transaction == null) throw new DataAbstractionException("Cannot Rollback the transaction. There is no open transaction in this client.");
        try
        {
            _logger?.LogTrace("Rollback transaction. Server: {Server}, DB {DB}, User {User}, Integrated Security {IntegratedSecurity}, Web User {WebUsr}", Server, DB, User, IntegratedSecurity, WebUser);
            await sql_transaction.RollbackAsync(ct);
            await sql_transaction.DisposeAsync();
        }
        catch (Exception x) when (x is not TaskCanceledException)
        {
            throw new DataAbstractionException($"An error ocurred while rolling back the transaction: {x.Message}.", x);
        }
        finally
        {
            sql_transaction = null;
        }
    }

    /// <summary>
    /// Ends a transaction and commit the changes. If there is not open transaction throw an exception.
    /// </summary>
    /// <param name="ct"></param>
    public async Task CommitTransaction(CancellationToken ct)
    {
        if (sql_transaction == null) throw new DataAbstractionException("Cannot Commit the transaction. There is no open transaction in this client.");
        try
        {
            _logger?.LogTrace("Commit transaction. Server: {Server}, DB {DB}, User {User}, Integrated Security {IntegratedSecurity}, Web User {WebUsr}", Server, DB, User, IntegratedSecurity, WebUser);
            await sql_transaction.CommitAsync(ct);
            await sql_transaction.DisposeAsync();
        }
        catch (Exception x) when (x is not TaskCanceledException)
        {
            throw new DataAbstractionException($"An error ocurred while commiting the transaction: {x.Message}.", x);
        }
        finally
        {
            sql_transaction = null;
        }
    }

    #region "Helpers"

    private static void CheckQueryConnectionStatusAndThrow(SqlConnection conn)
    {
        if (!conn.State.IsIn(ConnectionState.Open, ConnectionState.Closed, ConnectionState.Broken))
        {
            throw new DataAbstractionException($"The connection is in an unexpected state {conn.State}");
        }
    }


    #endregion

    #region "ExecuteNonQuery"

    private async Task ExecuteNonQuery(CommandType cmd_type, string query_text, CancellationToken ct, IEnumerable<ColumnBase>? parms = null)
    {
        if (query_text.IsNullOrEmpty()) return;
        CheckQueryConnectionStatusAndThrow(sql_connection);

        if (cmd_type == CommandType.StoredProcedure && DataDefaults.AppendDBOtoProcs && !query_text.Contains('.', StringComparison.OrdinalIgnoreCase))
            query_text = $"dbo.{query_text}";

        bool should_close = (sql_connection.State != ConnectionState.Open);
        using SqlCommand cmd = sql_connection.CreateCommand();
        try
        {
            await Connect(ct);
            ct.ThrowIfCancellationRequested();

            cmd.CommandText = query_text;
            cmd.CommandType = cmd_type;
            cmd.CommandTimeout = DataDefaults.DefaultCommandTimeOutInMins * 60;

            if (cmd_type == CommandType.StoredProcedure && parms != null)
            {
                OverrideColumnValues(parms);
                cmd.Parameters.AddRange(parms.AsSqlParameters());
            }

            using var ctr = ct.Register(() =>
            {
                try
                {
                    _logger?.LogTrace("Cancel {SQLText}\nServer: {Server}, DB {DB}, User {User}, Integrated Security {IntegratedSecurity}, Web User {WebUsr}", query_text, Server, DB, User, IntegratedSecurity, WebUser);
                    cmd.Cancel();
                }
                catch (SqlException ex)
                {
                    _logger?.LogTrace("Exception while cancelling {Exception}\n{SQLText\n}Server: {Server}, DB {DB}, User {User}, Integrated Security {IntegratedSecurity}, Web User {WebUsr}", ex, cmd.TraceSQL(), Server, DB, User, IntegratedSecurity, WebUser);
                    Debug.Print($"cmd.Cancel: {ex}");
                }
            });

            if (sql_transaction != null) cmd.Transaction = sql_transaction;

            _logger?.LogTrace("Executing {SQLText}\nServer: {Server}, DB {DB}, User {User}, Integrated Security {IntegratedSecurity}, Web User {WebUsr}", cmd.TraceSQL(), Server, DB, User, IntegratedSecurity, WebUser);
            await cmd.ExecuteNonQueryAsync(ct);

        }
        catch (Exception ex)
        {
            await Disconnect();
            if (ct.IsCancellationRequested && ex is SqlException)
            {
                throw new TaskCanceledException(ex.Message, ex);
            }
            else
            {
                Debug.Print(cmd.TraceSQL());

                throw new DataAbstractionException($"ExecuteNonQuery: {ex.Message}\n{cmd.CommandText.Truncate(100)} [truncated]", ex);
            }
        }
        finally
        {
            if (should_close || ct.IsCancellationRequested) await Disconnect();
        }

    }

    #endregion

    #region "Execute query generic data result"



    //public delegate Task<T> MapResult<T>(IValueReader record, string[] headers, CancellationToken ct);

    private async Task<T?> ExecuteSingleColumn<T>(CommandType cmd_type, string sql_text, CancellationToken ct, IEnumerable<ColumnBase>? parms = null)
    {
        CheckQueryConnectionStatusAndThrow(sql_connection);

        if (cmd_type == CommandType.StoredProcedure && DataDefaults.AppendDBOtoProcs && !sql_text.Contains('.', StringComparison.OrdinalIgnoreCase))
            sql_text = $"dbo.{sql_text}";

        bool should_close = (sql_connection.State != ConnectionState.Open);
        using SqlCommand cmd = sql_connection.CreateCommand();
        T? ret = default;
        try
        {
            await Connect(ct);
            ct.ThrowIfCancellationRequested();

            cmd.CommandText = sql_text;
            cmd.CommandType = cmd_type;
            cmd.CommandTimeout = DataDefaults.DefaultCommandTimeOutInMins * 60;
            if (cmd_type == CommandType.StoredProcedure && parms != null)
            {
                OverrideColumnValues(parms);
                cmd.Parameters.AddRange(parms.AsSqlParameters());
            }

            using var ctr = ct.Register(() =>
            {
                try
                {
                    _logger?.LogTrace("Cancelling command {SQLText}\nServer : {Server}, DB {DB}, User {User}, Integrated Security {IntegratedSecurity}, Web User {WebUsr}", sql_text, Server, DB, User, IntegratedSecurity, WebUser);
                    cmd.Cancel();
                }
                catch (SqlException ex)
                {
                    _logger?.LogTrace("Exception while cancelling {Exception}\n{SQLText\n}Server: {Server}, DB {DB}, User {User}, Integrated Security {IntegratedSecurity}, Web User {WebUsr}", ex, cmd.TraceSQL(), Server, DB, User, IntegratedSecurity, WebUser);
                    Debug.Print($"cmd.Cancel: {ex}");
                }
            });

            if (sql_transaction != null) cmd.Transaction = sql_transaction;

            _logger?.LogTrace("Executing SingleColumn {SQLText}\nServer: {Server}, DB {DB}, User {User}, Integrated Security {IntegratedSecurity}, Web User {WebUsr}", cmd.TraceSQL(), Server, DB, User, IntegratedSecurity, WebUser);
            using SqlDataReader reader = await cmd.ExecuteReaderAsync(CommandBehavior.SingleRow, ct);
            if (await reader.ReadAsync())
            {
                bool isNull = await reader.IsDBNullAsync(0, ct);

                ret = isNull ? default : await reader.GetFieldValueAsync<T>(0, ct);
            }

        }
        catch (Exception ex)
        {
            await Disconnect();
            if (ct.IsCancellationRequested && ex is SqlException)
            {
                throw new TaskCanceledException(ex.Message, ex);
            }
            else
            {
                Debug.Print(cmd.TraceSQL());
                throw new DataAbstractionException($"ExecuteSingleColumn: {ex.Message}\n{cmd.CommandText.Truncate(100)} [truncated]", ex);
            }
        }
        finally
        {
            if (should_close || ct.IsCancellationRequested) await Disconnect();
        }

        return ret;

    }

    private async Task<List<T>> ExecuteSingleQuery<T>(CommandType cmd_type, string sql_text, CancellationToken ct, AutoMapperMode mode = AutoMapperMode.ByName, MapResult<T>? mapper = null, IEnumerable<ColumnBase>? parms = null) where T : class, new()
    {
        CheckQueryConnectionStatusAndThrow(sql_connection);

        if (cmd_type == CommandType.StoredProcedure && DataDefaults.AppendDBOtoProcs && !sql_text.Contains('.', StringComparison.OrdinalIgnoreCase))
            sql_text = $"dbo.{sql_text}";

        bool should_close = (sql_connection.State != ConnectionState.Open);
        List<T> ret = [];
        using SqlCommand cmd = sql_connection.CreateCommand();
        try
        {
            await Connect(ct);
            ct.ThrowIfCancellationRequested();

            cmd.CommandText = sql_text;
            cmd.CommandType = cmd_type;
            cmd.CommandTimeout = DataDefaults.DefaultCommandTimeOutInMins * 60;
            if (cmd_type == CommandType.StoredProcedure && parms != null)
            {
                OverrideColumnValues(parms);
                cmd.Parameters.AddRange(parms.AsSqlParameters());
            }

            using var ctr = ct.Register(() =>
            {
                try
                {
                    _logger?.LogTrace("Cancelling command {SQLText}\nServer : {Server}, DB {DB}, User {User}, Integrated Security {IntegratedSecurity}, Web User {WebUsr}", sql_text, Server, DB, User, IntegratedSecurity, WebUser);
                    cmd.Cancel();
                }
                catch (SqlException ex)
                {
                    _logger?.LogTrace("Exception while cancelling {Exception}\n{SQLText\n}Server: {Server}, DB {DB}, User {User}, Integrated Security {IntegratedSecurity}, Web User {WebUsr}", ex, cmd.TraceSQL(), Server, DB, User, IntegratedSecurity, WebUser);
                    Debug.Print($"cmd.Cancel: {ex}");
                }
            });

            if (sql_transaction != null) cmd.Transaction = sql_transaction;

            _logger?.LogTrace("Executing SingleQuery {SQLText}\nServer: {Server}, DB {DB}, User {User}, Integrated Security {IntegratedSecurity}, Web User {WebUsr}", cmd.TraceSQL(), Server, DB, User, IntegratedSecurity, WebUser);
            using SqlDataReader reader = await cmd.ExecuteReaderAsync(CommandBehavior.SingleResult, ct);
            ValueReader vr = new(reader);
            if (mapper != null)
            {
                await foreach (T result in DataMappingProvider.GetResult<T>(vr, mapper, ct)) ret.Add(result);
            }
            else
            {
                if (mode == AutoMapperMode.ByPosition)
                {
                    await foreach (T result in AutoMapper.AutoMapperGetResultByPosition<T>(vr, ct)) ret.Add(result);
                }
                else
                {
                    await foreach (T result in AutoMapper.AutoMapperGetResultByName<T>(vr, mode, ct)) ret.Add(result);
                }
            }

        }
        catch (Exception ex)
        {
            await Disconnect();
            if (ct.IsCancellationRequested && ex is SqlException)
            {
                throw new TaskCanceledException(ex.Message, ex);
            }
            else
            {
                Debug.Print(cmd.TraceSQL());
                throw new DataAbstractionException($"ExecuteSingleQuery: {ex.Message}\n{cmd.CommandText.Truncate(100)} [truncated]", ex);
            }
        }
        finally
        {
            if (should_close || ct.IsCancellationRequested) await Disconnect();
        }

        return ret;

    }


    #endregion

    #region "Read records helper"
    private static object?[] ReadRecordFast(SqlDataReader reader, int field_count, string[] typeInfo)
    {
        object?[] record = new object[field_count];

        reader.GetValues(record);

        for (int i = 0; i < field_count; i++)
        {
            var val = record[i];

            if (val == DBNull.Value)
            {
                record[i] = null;
                continue;
            }

            if (typeInfo[i] == "date" && val is DateTime dt)
            {
                record[i] = DateOnly.FromDateTime(dt);
            }
        }

        return record;
    }

    private static async Task<object?[]> ReadRecordComplex(SqlDataReader reader, int field_count, string[] typeInfo, bool[] isMax, CancellationToken ct)
    {
        object?[] record = new object[field_count];

        for (int i = 0; i < field_count; i++)
        {
            if (await reader.IsDBNullAsync(i, ct))
            {
                record[i] = null;
                continue;
            }

            if (isMax[i])
            {
                record[i] = await reader.GetFieldValueAsync<object>(i, ct);
            }
            else
            {
                var val = reader.GetValue(i);
                record[i] = typeInfo[i] == "date" && val is DateTime dt ? DateOnly.FromDateTime(dt) : val;
            }
        }

        return record;
    }

    private static async Task<object?[]> ReadRecord(SqlDataReader reader, int field_count, string[] type_info, bool[] isMax, CancellationToken ct)
    {
        // For non max fields we can use GetValues which is optimized, but for max fields we need to use GetFieldValueAsync to avoid OutOfMemory exceptions with large data
        return isMax.Any(x => x) ? await ReadRecordComplex(reader, field_count, type_info, isMax, ct) : ReadRecordFast(reader, field_count, type_info);
    }

    #endregion

    #region "Execute query no channel"


    private static async IAsyncEnumerable<DataResult> GetResult(SqlDataReader reader, [EnumeratorCancellation] CancellationToken ct)
    {
        DataResult? ret;
        do
        {
            ct.ThrowIfCancellationRequested();

            var field_count = reader.FieldCount;

            var (headers, typeInfo, isMax) = DataMappingProvider.GetHeaders(reader);
            if (field_count > 0)
            {
                ret = new(headers, typeInfo);
                yield return ret;
            }
            else
            {
                ret = new DataResult(field_count);
                //yield return ret;
            }

            while (await reader.ReadAsync(ct))
            {
                ct.ThrowIfCancellationRequested();

                object?[] record = await ReadRecord(reader, field_count, ret.typeInfo, isMax, ct);
                ret.records.Add(record);
            }
        }
        while (await reader.NextResultAsync(ct));

    }

    private async Task<List<DataResult>> ExecuteQuery(CommandType cmd_type, string sql_text, CancellationToken ct, IEnumerable<ColumnBase>? parms = null)
    {
        CheckQueryConnectionStatusAndThrow(sql_connection);

        if (cmd_type == CommandType.StoredProcedure && DataDefaults.AppendDBOtoProcs && !sql_text.Contains('.', StringComparison.OrdinalIgnoreCase))
            sql_text = $"dbo.{sql_text}";

        bool should_close = (sql_connection.State != ConnectionState.Open);
        List<DataResult> ret = [];
        using SqlCommand cmd = sql_connection.CreateCommand();
        try
        {
            Debug.Print($"ExecuteQuery start executing {DateTime.Now:O}");
            await Connect(ct);
            ct.ThrowIfCancellationRequested();

            cmd.CommandText = sql_text;
            cmd.CommandType = cmd_type;
            cmd.CommandTimeout = DataDefaults.DefaultCommandTimeOutInMins * 60;
            if (cmd_type == CommandType.StoredProcedure && parms != null)
            {
                OverrideColumnValues(parms);
                cmd.Parameters.AddRange(parms.AsSqlParameters());
            }

            using var ctr = ct.Register(() =>
            {
                try
                {
                    _logger?.LogTrace("Cancelling command {SQLText}\nServer: {Server}, DB {DB}, User {User}, Integrated Security {IntegratedSecurity}, Web User {WebUsr}", sql_text, Server, DB, User, IntegratedSecurity, WebUser);
                    cmd.Cancel();
                }
                catch (SqlException ex)
                {
                    _logger?.LogTrace("Exception while cancelling {Exception}\n{SQLText\n}Server: {Server}, DB {DB}, User {User}, Integrated Security {IntegratedSecurity}, Web User {WebUsr}", ex, cmd.TraceSQL(), Server, DB, User, IntegratedSecurity, WebUser);
                    Debug.Print($"cmd.Cancel: {ex}");
                }
            });

            if (sql_transaction != null) cmd.Transaction = sql_transaction;

            _logger?.LogTrace("Executing {SQLText}\nServer: {Server}, DB {DB}, User {User}, Integrated Security {IntegratedSecurity}, Web User {WebUsr}", cmd.TraceSQL(), Server, DB, User, IntegratedSecurity, WebUser);
            using SqlDataReader reader = await cmd.ExecuteReaderAsync(ct);
            ct.ThrowIfCancellationRequested();
            await foreach (DataResult result in GetResult(reader, ct))
            {
                ret ??= [];
                if (result != null) ret.Add(result);
            }

        }
        catch (Exception ex)
        {
            await Disconnect();
            if (ct.IsCancellationRequested && ex is SqlException)
            {
                throw new TaskCanceledException(ex.Message, ex);
            }
            else
            {
                throw new DataAbstractionException($"ExecuteQuery: {ex.Message}\n{cmd.CommandText.Truncate(100)} [truncated]", ex);
            }
        }
        finally
        {
            if (should_close || ct.IsCancellationRequested) await Disconnect();
        }

        Debug.Print($"ExecuteQuery has finished processing the results {DateTime.Now:O}");
        return ret;

    }


    #endregion

    #region "ExecuteQueryChannel"

    private static async IAsyncEnumerable<DataResultChannel> GetResultAndWriteToChannel(SqlDataReader reader, int channel_capacity, int? max_allowed_rows, [EnumeratorCancellation] CancellationToken ct)
    {
        DataResultChannel ret;
        do
        {
            ct.ThrowIfCancellationRequested();
            var field_count = reader.FieldCount;

            var (headers, typeInfo, isMax) = DataMappingProvider.GetHeaders(reader);

            if (field_count > 0)
            {
                ret = new(field_count, channel_capacity, headers, typeInfo);
                yield return ret;
            }
            else
            {
                ret = new(field_count, channel_capacity);
            }

            try
            {
                int read_rows = 0;
                while (await reader.ReadAsync(ct))
                {
                    ct.ThrowIfCancellationRequested();

                    object?[] record = await ReadRecord(reader, field_count, typeInfo, isMax, ct);

                    //if (!ret.records.Writer.TryWrite(record))
                    //{
                    //    var error = new InternalBufferOverflowException();
                    //    ret.records.Writer.TryComplete(error);
                    //    throw error;
                    //}


                    if (max_allowed_rows != null)
                    {
                        read_rows++;
                        if (read_rows >= max_allowed_rows) throw new DataAbstractionException($"GetResultAndWriteToChannel: The query exceeds the maximum allowed rows for the results channel ({max_allowed_rows})");
                    }

                    // Apply Backpressure
                    await ret.records.Writer.WriteAsync(record, ct);
                }
            }
            catch (Exception ex)
            {
                var dataEx = ex;
                if (ex is not DataAbstractionException)
                {
                    dataEx = new DataAbstractionException($"GetResultAndWriteToChannel: {ex.Message}", ex);
                }
                ret.records.Writer.TryComplete(dataEx);
                throw dataEx;
            }
            finally
            {
                ret?.records.Writer.TryComplete();
            }

        } while (await reader.NextResultAsync(ct));

    }

    private async Task ExecuteQueryChannel(CommandType cmd_type, string sql_text, DataResultSetChannel result, int records_channel_capacity, CancellationToken ct, IEnumerable<ColumnBase>? parms = null, bool complete_channel = true, int? max_allowed_rows = null)
    {
        CheckQueryConnectionStatusAndThrow(sql_connection);
        ArgumentNullException.ThrowIfNull(result);

        if (cmd_type == CommandType.StoredProcedure && DataDefaults.AppendDBOtoProcs && !sql_text.Contains('.', StringComparison.OrdinalIgnoreCase))
            sql_text = $"dbo.{sql_text}";

        bool should_close = (sql_connection.State != ConnectionState.Open);
        using SqlCommand cmd = sql_connection.CreateCommand();
        try
        {
            await Connect(ct);
            ct.ThrowIfCancellationRequested();

            cmd.CommandText = sql_text;
            cmd.CommandType = cmd_type;
            cmd.CommandTimeout = DataDefaults.DefaultCommandTimeOutInMins * 60;
            if (cmd_type == CommandType.StoredProcedure && parms != null)
            {
                OverrideColumnValues(parms);
                cmd.Parameters.AddRange(parms.AsSqlParameters());
            }

            using var ctr = ct.Register(() =>
            {
                try
                {
                    _logger?.LogTrace("Cancelling command {SQLText}\nServer: {Server}, DB {DB}, User {User}, Integrated Security {IntegratedSecurity}, Web User {WebUsr}", sql_text, Server, DB, User, IntegratedSecurity, WebUser);
                    cmd.Cancel();
                }
                catch (SqlException ex)
                {
                    _logger?.LogTrace("Exception while cancelling {Exception}\n{SQLText\n}Server: {Server}, DB {DB}, User {User}, Integrated Security {IntegratedSecurity}, Web User {WebUsr}", ex, cmd.TraceSQL(), Server, DB, User, IntegratedSecurity, WebUser);
                    Debug.Print($"cmd.Cancel: {ex}");
                }
            });

            if (sql_transaction != null) cmd.Transaction = sql_transaction;

            _logger?.LogTrace("Executing to channel {SQLText}\nServer: {Server}, DB {DB}, User {User}, Integrated Security {IntegratedSecurity}, Web User {WebUsr}", cmd.TraceSQL(), Server, DB, User, IntegratedSecurity, WebUser);
            using SqlDataReader reader = await cmd.ExecuteReaderAsync(ct);
            ct.ThrowIfCancellationRequested();
            await foreach (DataResultChannel ret in GetResultAndWriteToChannel(reader, records_channel_capacity, max_allowed_rows, ct))
            {
                await result.Results.Writer.WriteAsync(ret, ct);
            }

        }
        catch (Exception ex)
        {
            await Disconnect();
            if (complete_channel) result.Results.Writer.TryComplete(ex);

            if (ct.IsCancellationRequested && ex is SqlException)
            {
                var exCancelled = new TaskCanceledException(ex.Message, ex);
                throw exCancelled;
            }
            else
            {
                Debug.Print(cmd.TraceSQL());
                var dataEx = new DataAbstractionException($"ExecuteQueryChannel: {ex.Message}\n{cmd.CommandText.Truncate(100)} [truncated]", ex);
                throw dataEx;
            }
        }
        finally
        {
            if (complete_channel) result.Results.Writer.TryComplete();
            if (should_close || ct.IsCancellationRequested) await Disconnect();
        }
    }

    #endregion

    /// <summary>
    /// Executes a SQL Query and maps the result of the first result set to a record. It uses <see cref="CommandBehavior.SingleResult"/>
    /// The record type provided must a have a paramerterless constructor. Optional you can provide a <see cref="MapResult{T}"/> custom mapper.
    /// If not it will use the default AutoMapper, that maps by the resulting query column names to the public members provided in T. If the query contains column names that are not in T, are ignored.
    /// </summary>
    public async Task<List<T>> ExecuteSQL<T>(string sql_text, CancellationToken ct, AutoMapperMode mode = AutoMapperMode.ByName, MapResult<T>? mapper = null) where T : class, new()
    {
        return await ExecuteSingleQuery(CommandType.Text, sql_text, ct, mode, mapper);
    }
    public async Task<T?> ExecuteSQLSingleColumn<T>(string sql_text, CancellationToken ct, IEnumerable<ColumnBase>? parms = null)
    {
        return await ExecuteSingleColumn<T?>(CommandType.Text, sql_text, ct, parms);
    }

    public async Task<List<T>> ExecuteSP<T>(string sp_name, CancellationToken ct, AutoMapperMode mode = AutoMapperMode.ByName, IEnumerable<ColumnBase>? parms = null, MapResult<T>? mapper = null) where T : class, new()
    {
        return await ExecuteSingleQuery(CommandType.StoredProcedure, sp_name, ct, mode, mapper, parms);
    }

    public async Task<T?> ExecuteSPSingleColumn<T>(string sp_name, CancellationToken ct, IEnumerable<ColumnBase>? parms = null)
    {
        return await ExecuteSingleColumn<T?>(CommandType.StoredProcedure, sp_name, ct, parms);
    }

    /// <summary>
    /// Executes a SQL Query and returns a DataResult.
    /// </summary>
    /// <param name="sql_text"></param>
    /// <param name="ct"></param>
    /// <returns></returns>
    public async Task<List<DataResult>> ExecuteSQL(string sql_text, CancellationToken ct)
    {
        return await ExecuteQuery(CommandType.Text, sql_text, ct);
    }

    /// <summary>
    /// Executes a SQL command asynchronously and streams the results to the specified channel.
    /// </summary>
    /// <param name="sql_text">The SQL command text to execute. Must be a valid SQL statement.</param>
    /// <param name="result">The channel that receives the result records. The channel must be initialized before calling this method.</param>
    /// <param name="records_channel_capacity">The maximum number of records that the result channel can buffer. Must be greater than zero.</param>
    /// <param name="ct">A cancellation token that can be used to cancel the asynchronous operation.</param>
    /// <param name="complete_channel">Indicates whether the result channel should be marked as complete after all results have been sent. If true, the channel will be completed; if false, the channel will remain open for additional writes. Default is true.</param>
    /// <param name="max_allowed_rows">An optional parameter that specifies the maximum number of rows allowed to be returned by the SQL command. If the result set exceeds this limit, an exception will be thrown. This parameter is used to prevent excessive memory usage when processing large result sets. If null, there is no limit on the number of rows returned.</param>
    /// <returns>A task that represents the asynchronous execution of the SQL command. The task completes when all results have
    /// been sent to the channel or the operation is canceled.</returns>
    public Task ExecuteSQLChannel(string sql_text, DataResultSetChannel result, int records_channel_capacity, CancellationToken ct, bool complete_channel = true, int? max_allowed_rows = null)
    {
        return ExecuteQueryChannel(CommandType.Text, sql_text, result, records_channel_capacity, ct, complete_channel: complete_channel, max_allowed_rows: max_allowed_rows);
    }

    /// <summary>
    /// Executes a stored procedure asynchronously and streams the results through the specified data result set channel.
    /// </summary>
    /// <remarks>This method is intended for efficient, asynchronous execution of stored procedures where results are
    /// processed incrementally via a channel. The operation can be cancelled using the provided cancellation
    /// token.</remarks>
    /// <param name="sp_name">The name of the stored procedure to execute. Cannot be null or empty.</param>
    /// <param name="parms">An optional collection of parameters to pass to the stored procedure. Each parameter should correspond to an input
    /// expected by the stored procedure.</param>
    /// <param name="result">The channel used to receive the results of the stored procedure execution. Cannot be null.</param>
    /// <param name="records_channel_capacity">The maximum number of records that the result channel can buffer before requiring processing. Must be a positive
    /// <param name="complete_channel">Indicates whether the result channel should be marked as complete after all results have been sent. If true, the channel will be completed; if false, the channel will remain open for additional writes. Default is true.</param>
    /// <param name="max_allowed_rows">An optional parameter that specifies the maximum number of rows allowed to be returned by the SQL command. If the result set exceeds this limit, an exception will be thrown. This parameter is used to prevent excessive memory usage when processing large result sets. If null, there is no limit on the number of rows returned.</param>
    /// <param name="ct">A cancellation token that can be used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation of executing the stored procedure and streaming the results.</returns>
    public Task ExecuteSPChannel(string sp_name, IEnumerable<ColumnBase>? parms, DataResultSetChannel result, int records_channel_capacity, CancellationToken ct, bool complete_channel = true, int? max_allowed_rows = null)
    {
        return ExecuteQueryChannel(CommandType.StoredProcedure, sp_name, result, records_channel_capacity, ct, parms, complete_channel: complete_channel, max_allowed_rows: max_allowed_rows);
    }


    public async Task<List<DataResult>> ExecuteSP(string sp_name, IEnumerable<ColumnBase>? parms, CancellationToken ct)
    {
        return await ExecuteQuery(CommandType.StoredProcedure, sp_name, ct, parms);
    }

    public Task ExecuteSPNonQuery(string sp_name, IEnumerable<ColumnBase>? parms, CancellationToken ct)
    {
        return ExecuteNonQuery(CommandType.StoredProcedure, sp_name, ct, parms);
    }


    public Task ExecuteSQLNonQuery(string sql_text, CancellationToken ct)
    {
        return ExecuteNonQuery(CommandType.Text, sql_text, ct);
    }

    public async Task ExecuteSQLNonQuery(List<string> sql_scripts, CancellationToken ct)
    {
        foreach (string script in sql_scripts)
        {
            await ExecuteSQLNonQuery(script, ct);
        }
    }

    #endregion

    #region "Dispose"

    /// <summary>
    /// Synchronously releases the unmanaged resources used by the DatabaseClient and optionally releases the managed resources.
    /// </summary>
    /// <param name="disposing">True to release both managed and unmanaged resources; false to release only unmanaged resources.</param>
    protected virtual void Dispose(bool disposing)
    {
        if (!disposedValue)
        {
            if (disposing)
            {
                // Dispose managed state (managed objects).
                // Use synchronous disposal on the transaction and connection.
                if (sql_transaction != null)
                {
                    try
                    {
                        sql_transaction.Dispose();
                    }
                    catch
                    {
                        // Optionally log the exception.
                    }
                    sql_transaction = null;
                }
                if (sql_connection != null)
                {
                    try
                    {
                        sql_connection.Dispose();
                    }
                    catch
                    {
                        // Optionally log the exception.
                    }
                    sql_connection = null!;
                }
            }

            // Free unmanaged resources (if any) here and set large fields to null.

            disposedValue = true;
        }
    }

    /// <summary>
    /// Synchronously disposes the DatabaseClient.
    /// </summary>
    public void Dispose()
    {
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Asynchronously disposes the DatabaseClient.
    /// </summary>
    /// <returns>A ValueTask representing the asynchronous disposal operation.</returns>
    public async ValueTask DisposeAsync()
    {
        if (!disposedValue)
        {
            // Asynchronously dispose managed resources.
            if (sql_transaction != null)
            {
                try
                {
                    await sql_transaction.DisposeAsync();
                }
                catch
                {
                    // TODO: log the exception.
                }
                sql_transaction = null;
            }
            if (sql_connection != null)
            {
                try
                {
                    await sql_connection.DisposeAsync();
                }
                catch
                {
                    // TODO: log the exception.
                }
                sql_connection = null!;
            }

            // Call the synchronous dispose to clean up any non‐async resources.
            Dispose(disposing: false);
            GC.SuppressFinalize(this);
            disposedValue = true;
        }
    }


    #endregion
}
