# Class: MicroM.Data.DatabaseClient
## Overview
Provides SQL Server connectivity and helpers for executing queries and stored procedures.

**Inheritance**
object -> DatabaseClient

**Implements**
IDisposable, IAsyncDisposable, IEntityClient

## Example Usage
```csharp
using var client = new MicroM.Data.DatabaseClient("server", "database");
await client.Connect(CancellationToken.None);
```
## Constructors
| Constructor | Description |
|:------------|:-------------|
| DatabaseClient(string server, string db, string user = "", string password = "", bool integrated_security = false, int connection_timeout_secs = -1, ILogger? logger = null, Dictionary<string, object>? server_claims = null) | Initializes with connection parameters. |
| DatabaseClient(DatabaseClient dbc, string new_server = "", string new_db = "", int connection_timeout_secs = -1, ILogger? logger = null, Dictionary<string, object>? server_claims = null) | Initializes using settings from an existing client. |

## Properties
| Property | Type | Description |
|:------------|:-------------|:-------------|
| WebUser | string | Authenticated web user. |
| QueryTimeout | int | Command timeout in seconds. |
| ConnectionString | string | Connection string. |
| MasterDatabase | string | Name of the master database. |
| Server | string | SQL Server host. |
| DB | string | Database name. |
| User | string | SQL Server user ID. |
| Password | string | SQL Server password. |
| IntegratedSecurity | bool | Indicates if integrated security is used. |
| Pooling | bool | Indicates if connection pooling is enabled. |
| MinPoolSize | int | Minimum size of the connection pool. |
| MaxPoolSize | int | Maximum size of the connection pool. |
| WorkstationID | string | Workstation identifier. |
| ApplicationName | string | Application name for the connection. |
| CurrentLanguage | string | Current language for the connection. |
| Encryption | SqlConnectionEncryptOption | Encryption mode for the connection. |
| SQLConnectionSB | SqlConnectionStringBuilder | Underlying connection string builder. |
| ConnectionTimeout | int | Timeout for opening the connection. |
| ConnectionState | ConnectionState | Current connection state. |
| HTTPService | string | HTTP service endpoint. |
| ServerClaims | Dictionary<string, object>? | Server or user claims. |
| isTransactionOpen | bool | Indicates if a transaction is open. |

## Methods
| Method | Description |
|:------------|:-------------|
| OverrideColumnValues(IEnumerable<ColumnBase>) | Overrides parameter values with server claims. |
| Clone(string, string, string, string, int) | Creates a copy of the client with optional overrides. |
| Connect(CancellationToken, bool, bool, bool, bool) | Opens a connection to the server. |
| Disconnect() | Closes the connection. |
| BeginTransaction(CancellationToken) | Starts a transaction. |
| RollbackTransaction(CancellationToken) | Rolls back the current transaction. |
| CommitTransaction(CancellationToken) | Commits the current transaction. |
| ExecuteSQL<T>(string, CancellationToken, AutoMapperMode, MapResult<T>?) | Executes SQL and maps results. |
| ExecuteSQLSingleColumn<T>(string, CancellationToken, IEnumerable<ColumnBase>?) | Executes SQL returning a single column. |
| ExecuteSP<T>(string, CancellationToken, AutoMapperMode, IEnumerable<ColumnBase>?, MapResult<T>?) | Executes a stored procedure and maps results. |
| ExecuteSPSingleColumn<T>(string, CancellationToken, IEnumerable<ColumnBase>?) | Executes a stored procedure returning a single column. |
| ExecuteSQL(string, CancellationToken) | Executes SQL and returns <code>DataResult</code> list. |
| ExecuteSQLChannel(string, DataResultSetChannel, CancellationToken) | Executes SQL and writes to a channel. |
| ExecuteSPChannel(string, IEnumerable<ColumnBase>?, DataResultSetChannel, CancellationToken) | Executes a stored procedure and writes to a channel. |
| ExecuteSP(string, IEnumerable<ColumnBase>?, CancellationToken) | Executes a stored procedure and returns <code>DataResult</code> list. |
| ExecuteSPNonQuery(string, IEnumerable<ColumnBase>?, CancellationToken) | Executes a stored procedure without returning results. |
| ExecuteSQLNonQuery(string, CancellationToken) | Executes a SQL command without returning results. |
| ExecuteSQLNonQuery(List<string>, CancellationToken) | Executes multiple SQL scripts without returning results. |
| Dispose() | Releases resources. |
| DisposeAsync() | Asynchronously releases resources. |

## Remarks
None.

## See Also
-

