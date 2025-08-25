# Class: MicroM.Data.EntityForeignKeyBase
## Overview
Base functionality for defining foreign key relationships between entities.

**Inheritance**
object -> EntityForeignKeyBase

**Implements**
None

## Example Usage
```csharp
// Used as a base for specific foreign keys
```
## Remarks
None.

## Constructors
| Constructor | Description |
|:------------|:-------------|
| EntityForeignKeyBase(string name, Type parent_type, Type child_type, bool fake, bool do_not_create_index, List<BaseColumnMapping>? key_mappings) | Initializes the base relationship. |

## Properties
| Property | Type | Description |
|:------------|:-------------|:-------------|
| Name | string | Name of the relationship. |
| ParentEntityType | Type | Parent entity type. |
| ChildEntityType | Type | Child entity type. |
| KeyMappings | List<BaseColumnMapping> | Column mappings forming the key. |
| Fake | bool | Indicates a fake relationship. |
| DoNotCreateIndex | bool | Indicates whether an index is not created. |

## Methods
| Method | Description |
|:------------|:-------------|
| AddLookup(...) | Registers a lookup for the relationship. |

