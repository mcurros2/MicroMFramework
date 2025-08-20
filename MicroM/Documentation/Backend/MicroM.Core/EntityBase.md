# Class: MicroM.Core.EntityBase

## Overview
Abstract base class providing initialization, data access helpers, and action execution for entities.

## Methods
| Method | Description |
|:------------|:-------------|
| Init(IEntityClient? ec, IMicroMEncryption? encryptor = null) | Initializes the entity with a data client and optional encryptor. |
| ExecuteAction(string action_name, DataWebAPIRequest args, MicroMOptions? Options, IWebAPIServices? API, IMicroMEncryption? encryptor, CancellationToken ct, string? app_id = null) | Executes a named entity action. |
| DeleteData(CancellationToken ct, bool throw_dbstat_exception = false, ...) | Deletes data using the configured client. |
| ExecuteView(CancellationToken ct, ViewDefinition view, int? row_limit = null, ...) | Executes a view stored procedure. |
| GetData(CancellationToken ct, ...) | Retrieves data using the configured client. |
| InsertData(CancellationToken ct, bool throw_dbstat_exception = false, ...) | Inserts data using the configured client. |
| LookupData(CancellationToken ct, string? lookup_name = null, ...) | Performs a lookup operation. |
| UpdateData(CancellationToken ct, bool throw_dbstat_exception = false, ...) | Updates data using the configured client. |
| ExecuteProcessDBStatus(CancellationToken ct, ProcedureDefinition proc, ...) | Executes a procedure returning database status. |
| ExecuteProc(CancellationToken ct, ProcedureDefinition proc, ...) | Executes a procedure and returns result sets. |

## Properties
| Property | Type | Description |
|:------------|:-------------|:-------------|
| Def | EntityDefinition | Definition describing entity metadata. |
| Client | IEntityClient | Data access client. |
| Encryptor | IMicroMEncryption? | Optional encryptor used for data operations. |
| Actions | Dictionary<string, Action> | Registered custom actions. |

## Remarks
Serves as the foundation for concrete entity implementations that interact with the database.

## See Also
- [Entity](Entity.md)
- [EntityDefinition](EntityDefinition.md)
