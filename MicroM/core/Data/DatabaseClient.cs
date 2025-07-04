﻿using MicroM.Configuration;
using MicroM.Extensions;
using MicroM.Web.Authentication;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using System.Data;
using System.Data.Common;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.CompilerServices;
using static MicroM.Data.IEntityClient;

namespace MicroM.Data
{

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
        /// <param name="ct"></param>
        /// <returns>True if the connection was already opened</returns>
        /// <exception cref="DataAbstractionException"></exception>
        public async Task<bool> Connect(CancellationToken ct, bool throw_exception = true, bool rollback_on_errors = true, bool isolation_level_read_committed = true)
        {

            if (sql_connection.State == ConnectionState.Open) return true;
            if (sql_connection.State != ConnectionState.Closed) await sql_connection.CloseAsync();
            sql_connection.ConnectionString = connection_builder.ConnectionString;
            try
            {
                _logger?.LogTrace("Connecting to {Server}, DB {DB}, User {User}, Integrated Security {IntegratedSecurity}, Web User {WebUsr}", Server, DB, User, IntegratedSecurity, WebUser);
                await sql_connection.OpenAsync(ct);
                if (rollback_on_errors)
                {
                    await ExecuteNonQuery(CommandType.Text, "SET XACT_ABORT ON", ct);
                }
                if (isolation_level_read_committed)
                {
                    await ExecuteNonQuery(CommandType.Text, "SET TRANSACTION ISOLATION LEVEL READ COMMITTED", ct);
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

            await ExecuteNonQuery(CommandType.Text, "if @@trancount > 0 ROLLBACK TRANSACTION", CancellationToken.None);

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

            if (cmd_type == CommandType.StoredProcedure && DataDefaults.AppendDBOtoProcs && !query_text.StartsWith("dbo.", StringComparison.OrdinalIgnoreCase))
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

                ct.Register(() =>
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

        private async static IAsyncEnumerable<T> AutoMapperGetResultByPosition<T>(ValueReader vr, [EnumeratorCancellation] CancellationToken ct) where T : new()
        {

            var members = typeof(T).GetMembers(BindingFlags.Public | BindingFlags.Instance).Where(p => p.MemberType.IsIn(MemberTypes.Property, MemberTypes.Field) && p.GetCustomAttribute<CompilerGeneratedAttribute>() == null).OrderBy(p => p.MetadataToken);

            if (await vr._reader.ReadAsync(ct))
            {
                do
                {
                    ct.ThrowIfCancellationRequested();
                    T record = new();
                    int x = 0;
                    foreach (var member in members)
                    {
                        var val = await vr.GetFieldValueAsync<object>(x++, ct);
                        if (val == null || val?.GetType() == typeof(DBNull)) val = null;

                        if (member is PropertyInfo prop)
                        {
                            prop.SetValue(record, val);
                        }
                        else if (member is FieldInfo field)
                        {
                            field.SetValue(record, val);
                        }
                    }
                    yield return record;
                }
                while (await vr._reader.ReadAsync(ct));
            }
        }

        private async static IAsyncEnumerable<T> AutoMapperGetResultByName<T>(ValueReader vr, AutoMapperMode mode, [EnumeratorCancellation] CancellationToken ct) where T : new()
        {
            var members = typeof(T).GetMembers(BindingFlags.Public | BindingFlags.Instance).Where(p => p.MemberType.IsIn(MemberTypes.Property, MemberTypes.Field) && p.GetCustomAttribute<CompilerGeneratedAttribute>() == null);

            if (await vr._reader.ReadAsync(ct))
            {
                var headers = GetHeadersHashSet(vr._reader, mode == AutoMapperMode.ByNameSpacesToUnderscore);
                do
                {
                    ct.ThrowIfCancellationRequested();
                    T record = new();
                    List<string> headerErrors = [];
                    foreach (var member in members)
                    {
                        if (!headers.Contains(member.Name))
                        {
                            headerErrors.Add(member.Name);
                        }
                        else
                        {
                            var val = await vr.GetFieldValueAsync<object>(member.Name, ct);
                            if (val == null || val?.GetType() == typeof(DBNull)) val = null;
                            if (member is PropertyInfo prop)
                            {
                                prop.SetValue(record, val);
                            }
                            else if (member is FieldInfo field)
                            {
                                field.SetValue(record, val);
                            }
                        }

                    }
                    if (headerErrors.Count > 0 && mode == AutoMapperMode.ByName) throw new MissingMemberException($"Missing expected columns: {string.Join(", ", headerErrors)}");

                    yield return record;
                }
                while (await vr._reader.ReadAsync(ct));
            }
        }

        //public delegate Task<T> MapResult<T>(IGetFieldValue record, string[] headers, CancellationToken ct);

        // MMC: this method has these problems:
        // It SHOULDN'T be returned from an API if expecting more than one row: it will serialize all column names for each record
        // It adds very little value
        private static async IAsyncEnumerable<T> GetResult<T>(ValueReader vr, MapResult<T> mapper, [EnumeratorCancellation] CancellationToken ct)
        {
            if (await vr._reader.ReadAsync(ct))
            {
                var result = GetHeaders(vr._reader);
                do
                {
                    ct.ThrowIfCancellationRequested();
                    T record = await mapper(vr, result.headers, ct);
                    yield return record;
                }
                while (await vr._reader.ReadAsync(ct));
            }
        }

        private static (string[] headers, string[] typeInfo) GetHeaders(DbDataReader reader, bool spaceToUnderscore = false)
        {
            string[] headers = new string[reader.FieldCount];
            string[] typeInfo = new string[reader.FieldCount];
            for (int x = 0; x < reader.FieldCount; x++)
            {
                string original_name = reader.GetName(x);
                string type = reader.GetDataTypeName(x);
                typeInfo[x] = type;

                string resulting_name;

                if (string.IsNullOrEmpty(original_name))
                {
                    resulting_name = $"Column{x}";
                }
                else
                {
                    resulting_name = spaceToUnderscore ? original_name.Replace(" ", "_") : original_name;
                }

                if (headers.Contains(resulting_name))
                {
                    throw new InvalidOperationException($"Duplicate column name when replacing spaces with underscores. Original: {original_name} Replaced: {resulting_name}");
                }

                headers[x] = resulting_name;
            }

            return (headers, typeInfo);
        }

        private static HashSet<string> GetHeadersHashSet(DbDataReader reader, bool spaceToUnderscore = false)
        {
            HashSet<string> headers = new(reader.FieldCount, StringComparer.OrdinalIgnoreCase);
            for (int x = 0; x < reader.FieldCount; x++)
            {
                string original_name = reader.GetName(x);
                string resulting_name;

                if (string.IsNullOrEmpty(original_name))
                {
                    resulting_name = $"Column{x}";
                }
                else
                {
                    resulting_name = spaceToUnderscore ? original_name.Replace(" ", "_") : original_name;
                }

                if (headers.Contains(resulting_name))
                {
                    throw new InvalidOperationException($"Duplicate column name when replacing spaces with underscores. Original: {original_name} Replaced: {resulting_name}");
                }

                headers.Add(resulting_name);
            }

            return headers;
        }

        private async Task<T?> ExecuteSingleColumn<T>(CommandType cmd_type, string sql_text, CancellationToken ct, IEnumerable<ColumnBase>? parms = null)
        {
            CheckQueryConnectionStatusAndThrow(sql_connection);

            if (cmd_type == CommandType.StoredProcedure && DataDefaults.AppendDBOtoProcs && !sql_text.StartsWith("dbo.", StringComparison.OrdinalIgnoreCase))
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

                ct.Register(() =>
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

            if (cmd_type == CommandType.StoredProcedure && DataDefaults.AppendDBOtoProcs && !sql_text.StartsWith("dbo.", StringComparison.OrdinalIgnoreCase))
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

                ct.Register(() =>
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
                    await foreach (T result in GetResult<T>(vr, mapper, ct)) ret.Add(result);
                }
                else
                {
                    if (mode == AutoMapperMode.ByPosition)
                    {
                        await foreach (T result in AutoMapperGetResultByPosition<T>(vr, ct)) ret.Add(result);
                    }
                    else
                    {
                        await foreach (T result in AutoMapperGetResultByName<T>(vr, mode, ct)) ret.Add(result);
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

        #region "Execute query no channel"


        private static async IAsyncEnumerable<DataResult> GetResult(SqlDataReader reader, [EnumeratorCancellation] CancellationToken ct)
        {
            DataResult? ret;
            do
            {
                ct.ThrowIfCancellationRequested();

                var field_count = reader.FieldCount;

                if (field_count > 0)
                {
                    var (headers, typeInfo) = GetHeaders(reader);
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

                    object?[] record = new object[field_count];
                    for (int x = 0; x < field_count; x++)
                    {
                        ct.ThrowIfCancellationRequested();
                        if (await reader.IsDBNullAsync(x, ct))
                        {
                            record[x] = null;
                        }
                        else
                        {
                            // MMC: need to convert system.datetime to dateonly as sqldatareader returns systems.datetime for date columns
                            if (ret.typeInfo[x] == "date")
                            {
                                var value = await reader.GetFieldValueAsync<object?>(x, ct);
                                if (value is DateTime dateTime)
                                {
                                    record[x] = DateOnly.FromDateTime(dateTime);
                                }
                                else
                                {
                                    record[x] = null;
                                }
                            }
                            else
                            {
                                record[x] = await reader.GetFieldValueAsync<object?>(x, ct);
                            }
                        }
                    }
                    ret.records.Add(record);
                }
            }
            while (await reader.NextResultAsync(ct));

        }

        private async Task<List<DataResult>> ExecuteQuery(CommandType cmd_type, string sql_text, CancellationToken ct, IEnumerable<ColumnBase>? parms = null)
        {
            CheckQueryConnectionStatusAndThrow(sql_connection);

            if (cmd_type == CommandType.StoredProcedure && DataDefaults.AppendDBOtoProcs && !sql_text.StartsWith("dbo.", StringComparison.OrdinalIgnoreCase))
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

                ct.Register(() =>
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

        private static async IAsyncEnumerable<DataResultChannel> GetResultAndWriteToChannel(SqlDataReader reader, [EnumeratorCancellation] CancellationToken ct)
        {
            DataResultChannel? ret;
            do
            {
                ct.ThrowIfCancellationRequested();

                ret = null;
                while (await reader.ReadAsync(ct))
                {
                    ct.ThrowIfCancellationRequested();

                    if (ret == null)
                    {
                        ret = new DataResultChannel(reader.FieldCount);
                        for (int x = 0; x < reader.FieldCount; x++)
                        {
                            string name = reader.GetName(x);
                            ret.Header[x] = (string.IsNullOrEmpty(name) ? $"Column{x}" : name);
                        }
                        yield return ret;
                    }

                    object[] record = new object[reader.FieldCount];
                    for (int x = 0; x < reader.FieldCount; x++)
                    {
                        ct.ThrowIfCancellationRequested();
                        record[x] = await reader.GetFieldValueAsync<object>(x);
                    }

                    if (!ret.Records.Writer.TryWrite(record))
                    {
                        throw new InternalBufferOverflowException();
                    }
                    //await ret.Records.Writer.WriteAsync(record, ct);
                }

                ret?.Records.Writer.Complete();

            } while (await reader.NextResultAsync(ct));

        }

        private async Task ExecuteQueryChannel(CommandType cmd_type, string sql_text, DataResultSetChannel result, CancellationToken ct, IEnumerable<ColumnBase>? parms = null)
        {
            CheckQueryConnectionStatusAndThrow(sql_connection);
            ArgumentNullException.ThrowIfNull(result);

            if (cmd_type == CommandType.StoredProcedure && DataDefaults.AppendDBOtoProcs && !sql_text.StartsWith("dbo.", StringComparison.OrdinalIgnoreCase))
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

                ct.Register(() =>
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
                await foreach (DataResultChannel ret in GetResultAndWriteToChannel(reader, ct))
                {
                    await result.Results.Writer.WriteAsync(ret, ct);
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
                    throw new DataAbstractionException($"ExecuteQueryChannel: {ex.Message}\n{cmd.CommandText.Truncate(100)} [truncated]", ex);
                }
            }
            finally
            {
                result.Results.Writer.Complete();
                if (should_close || ct.IsCancellationRequested) await Disconnect();
            }


        }

        #endregion

        /// <summary>
        /// Executes a SQL Query and maps the result of the first result set to a record. It uses <see cref="CommandBehavior.SingleResult"/>
        /// The record type provided must a have a paramerterless constructor. Optional you can provide a <see cref="MapResult{T}"/> custom mapper.
        /// If not it will use the default AutoMapper, that maps by the resulting query column names to the public members provided in T. If the query contains column names that are not in T, are ignored.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="sql_text"></param>
        /// <param name="ct"></param>
        /// <param name="mapper"></param>
        /// <returns></returns>
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
        /// Executes a SQL query and returns a DataResultSetChannel.
        /// </summary>
        /// <param name="sql_text"></param>
        /// <param name="result"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        public Task ExecuteSQLChannel(string sql_text, DataResultSetChannel result, CancellationToken ct)
        {
            return ExecuteQueryChannel(CommandType.Text, sql_text, result, ct);
        }

        /// <summary>
        /// Executes a stored procedure and writes results to a <see cref="DataResultSetChannel"/>
        /// </summary>
        /// <param name="sp_name"></param>
        /// <param name="parms"></param>
        /// <param name="result"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        public Task ExecuteSPChannel(string sp_name, IEnumerable<ColumnBase>? parms, DataResultSetChannel result, CancellationToken ct)
        {
            return ExecuteQueryChannel(CommandType.StoredProcedure, sp_name, result, ct, parms);
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
}
