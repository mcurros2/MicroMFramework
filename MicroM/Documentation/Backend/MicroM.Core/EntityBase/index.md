# Class: MicroM.Core.EntityBase
## Overview
Base class for entities providing initialization and data access helpers.

**Inheritance**
[InitBase](InitBase/index.md) -> EntityBase

**Implements**
None

## Example Usage
```csharp
var entity = new MyEntity();
entity.Init(client);
```
## Properties
| Property | Type | Description |
|:------------|:-------------|:-------------|
| Def | EntityDefinition | Entity definition. |
| Client | IEntityClient | Data access client. |
| Encryptor | IMicroMEncryption? | Encryption provider. |
| Actions | Dictionary<string, Action> | Registered actions. |

## Methods
| Method | Description |
|:------------|:-------------|
| Init(IEntityClient? ec, IMicroMEncryption? encryptor) | Initializes the entity. |
| ExecuteAction(string action_name, DataWebAPIRequest args, MicroMOptions? Options, IWebAPIServices? API, IMicroMEncryption? encryptor, CancellationToken ct, string? app_id) | Executes a registered action. |
| DeleteData(...) | Deletes entity data. |
| ExecuteView(...) | Executes a view and returns results. |
| GetData(...) | Retrieves entity data. |
| GetData<T>(...) | Retrieves entity data mapped to a type. |
| InsertData(...) | Inserts entity data. |
| LookupData(...) | Performs a lookup. |
| UpdateData(...) | Updates entity data. |
| ExecuteProcessDBStatus(...) | Executes a procedure returning DBStatus. |
| ExecuteProc(...) | Executes a procedure returning results. |
| ExecuteProc<T>(...) | Executes a procedure returning mapped results. |
| ExecuteProcSingleColumn<T>(...) | Executes a procedure returning a single column. |
| ExecuteProcSingleRow<T>(...) | Executes a procedure returning a single row. |

## Remarks
None.

## See Also
- [InitBase](InitBase/index.md)
