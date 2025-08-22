# Class: MicroM.Data.EntityForeignKey
## Overview
Defines a strongly-typed foreign key relationship between two entities.

**Type Parameters**
| Parameter | Description |
|:------------|:-------------|
|[TParent](../EntityForeignKey/index.md) | Parent entity type. |
|[TChild](../EntityForeignKey/index.md) | Child entity type. |

**Inheritance**
[EntityForeignKeyBase](../EntityForeignKeyBase/index.md) -> EntityForeignKey

**Implements**
None

## Example Usage
```csharp
var fk = new EntityForeignKey<Parent, Child>();
```
## Remarks
None.

## Constructors
| Constructor | Description |
|:------------|:-------------|
| EntityForeignKey(string name = "", bool fake = false, bool do_not_create_index = false, List<BaseColumnMapping>? key_mappings = null) | Creates the foreign key definition. |

## Properties
| Property | Type | Description |
|:------------|:-------------|:-------------|
| Name | string | Name of the relationship. |

## Methods
| Method | Description |
|:------------|:-------------|
| AddLookup(...) | Registers a lookup for the relationship. |

## See Also
-
