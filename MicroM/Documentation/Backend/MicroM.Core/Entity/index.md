# Class: MicroM.Core.Entity<TDefinition>
## Overview
Generic entity base providing a strongly typed definition.

### Type Parameters
| Parameter | Description |
|:------------|:-------------|
|TDefinition|Entity definition type.|

**Inheritance**
[EntityBase](EntityBase/index.md) -> Entity<TDefinition>

**Implements**
None

## Example Usage
```csharp
var entity = new MyEntity();
```
## Constructors
| Constructor | Description |
|:------------|:-------------|
| Entity() | Initializes a new entity. |
| Entity(string table_name) | Initializes the entity with a table name. |
| Entity(IEntityClient ec, IMicroMEncryption? encryptor) | Initializes the entity with a client and encryptor. |

## Properties
| Property | Type | Description |
|:------------|:-------------|:-------------|
| Def | TDefinition | Strongly typed definition. |

## Methods
| Method | Description |
|:------------|:-------------|
| None |

## Remarks
None.

## See Also
- [EntityBase](EntityBase/index.md)
