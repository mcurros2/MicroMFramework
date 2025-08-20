# Class: MicroM.Data.DatabaseClient

## Overview
Implements database connectivity for `IEntityClient`, providing connection management, transactions, and execution helpers for SQL Server.

## Methods
| Method | Description |
|:--|:--|
| Connect(ct, ...) | Opens the database connection with optional settings. |
| Disconnect() | Closes the connection, rolling back stray transactions. |
| BeginTransaction(ct) / CommitTransaction(ct) / RollbackTransaction(ct) | Manages SQL transactions. |
| ExecuteSP(...) | Executes a stored procedure returning results. |
| ExecuteSQL(...) | Runs raw SQL statements. |

## Remarks
Used by data-layer classes to interact with SQL Server with error handling and logging.
