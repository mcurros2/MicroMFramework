# Interface: MicroM.Data.IEntityClient

## Overview
Defines operations for connecting to SQL Server, managing transactions, and executing queries for entity data access.

## Properties
| Property | Type | Description |
|:--|:--|:--|
| ConnectionState | ConnectionState | Current connection state. |
| ConnectionString | string | Connection string to the server. |
| WebUser | string | User name associated with web requests. |
| ServerClaims | Dictionary<string, object>? | Claims provided by the server. |

## Methods
| Method | Description |
|:--|:--|
| Connect(ct, ...) / Disconnect() | Opens or closes the connection. |
| BeginTransaction(ct) / CommitTransaction(ct) / RollbackTransaction(ct) | Transaction management. |
| ExecuteSP(...) / ExecuteSQL(...) | Execute stored procedures or SQL text. |
| ExecuteSPChannel(...) / ExecuteSQLChannel(...) | Stream results over channels. |

## Remarks
Includes nested `AutoMapperMode` enum and `MapResult<T>` delegate for custom mapping.
