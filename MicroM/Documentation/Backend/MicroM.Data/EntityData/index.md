# Class: MicroM.Data.EntityData
## Overview
Provides common operations for interacting with entity data and procedures.

**Inheritance**
object -> EntityData

**Implements**
[IEntityData](../IEntityData/index.md)

## Example Usage
```csharp
var data = new EntityData(client, def);
```
## Remarks
None.

## Constructors
| Constructor | Description |
|:------------|:-------------|
| EntityData(IEntityClient ec, EntityDefinition def, IMicroMEncryption? encryptor = null) | Creates the entity data helper. |

## Properties
| Property | Type | Description |
|:------------|:-------------|:-------------|
| EntityClient | IEntityClient | Client used for database operations. |
| Encryptor | IMicroMEncryption? | Optional encryption service. |

## Methods
| Method | Description |
|:------------|:-------------|
| UpdateData(...) | Updates a record for the entity. |
| InsertData(...) | Inserts a record for the entity. |
| DeleteData(...) | Deletes a record for the entity. |

