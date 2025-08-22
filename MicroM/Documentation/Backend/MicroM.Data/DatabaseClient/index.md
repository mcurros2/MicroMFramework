# Class: MicroM.Data.DatabaseClient
## Overview
SQL Server implementation of IEntityClient.

**Inheritance**
object -> DatabaseClient

**Implements**
[IEntityClient](../IEntityClient/index.md)

## Example Usage
```csharp
using var client = new DatabaseClient("server","db");
```
## Remarks
None.

## Constructors
| Constructor | Description |
|:------------|:-------------|
| DatabaseClient(string server, string db, string user, string password, bool integratedSecurity, int connectionTimeoutSecs, ILogger? logger, Dictionary<string, object>? serverClaims) | Initializes with connection parameters. |
| DatabaseClient(DatabaseClient dbc, string newServer, string newDb, int connectionTimeoutSecs, ILogger? logger, Dictionary<string, object>? serverClaims) | Initializes from an existing client. |

## Properties
| Property | Type | Description |
|:------------|:-------------|:-------------|
| WebUser | string | Authenticated web user. |
| QueryTimeout | int | Default query timeout in seconds. |

## Methods
| Method | Description |
|:------------|:-------------|
| Clone(...) | Creates a shallow clone with new connection parameters. |
| Connect(...) | Opens the database connection. |
| Disconnect() | Closes the database connection. |
| BeginTransaction(...) | Begins a transaction. |
| CommitTransaction(...) | Commits the current transaction. |
| RollbackTransaction(...) | Rolls back the current transaction. |

## See Also
- [IEntityClient](../IEntityClient/index.md)
