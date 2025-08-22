# Class: MicroM.Data.IEntityData
## Overview
Defines operations for manipulating entity data through stored procedures and views.

**Inheritance**
object -> IEntityData

**Implements**
None

## Example Usage
```csharp
IEntityData data = new EntityData(client, def);
```
## Remarks
None.

## Constructors
| Constructor | Description |
|:------------|:-------------|
| None | Interface contains no constructors. |

## Properties
| Property | Type | Description |
|:------------|:-------------|:-------------|
| EntityClient | IEntityClient | Client used for database operations. |
| Encryptor | IMicroMEncryption? | Optional encryption service. |

## Methods
| Method | Description |
|:------------|:-------------|
| DeleteData(...) | Deletes a record for the entity. |
| ExecuteProc(...) | Executes a stored procedure. |
| ExecuteView(...) | Executes a view. |
| InsertData(...) | Inserts a record for the entity. |
| LookupData(...) | Performs a lookup. |
| UpdateData(...) | Updates a record for the entity. |
| GetData(...) | Retrieves data and maps it. |

