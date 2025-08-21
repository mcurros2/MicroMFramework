# Class: MicroM.Core.EntityChecker
## Overview
Utilities to validate entity definitions and relationships.

**Inheritance**
object -> EntityChecker

**Implements**
None

## Example Usage
```csharp
EntityChecker.CheckEntities();
```
## Methods
| Method | Description |
|:------------|:-------------|
| CheckEntity(Type entity_type) | Validates a single entity type. |
| GetProperties(Type entity) | Retrieves column, view and procedure properties. |
| CheckEntities(Assembly? asm, string? assembly_name) | Validates all entities in an assembly. |

## Remarks
None.

## See Also
-
