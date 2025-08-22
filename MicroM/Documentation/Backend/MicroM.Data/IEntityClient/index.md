# Interface: MicroM.Data.IEntityClient
## Overview
Defines database operations for entity-based clients.

## Methods
| Method | Description |
|:------------|:-------------|
| Clone(...) | Creates a shallow clone with new connection parameters. |
| Connect(...) | Opens the database connection. |
| Disconnect() | Closes the database connection. |
| BeginTransaction(...) | Begins a transaction. |
| CommitTransaction(...) | Commits the current transaction. |
| RollbackTransaction(...) | Rolls back the current transaction. |
| ExecuteSP(...) | Executes a stored procedure and returns results. |
| ExecuteSQL(...) | Executes raw SQL and returns results. |
| ExecuteSPNonQuery(...) | Executes a stored procedure without results. |
| ExecuteSQLNonQuery(...) | Executes SQL without results. |

## Remarks
None.

## See Also
- [DatabaseClient](../DatabaseClient/index.md)
