# Class: MicroM.Core.EntityChecker

## Overview
Utility class that validates entity types to ensure their definitions and properties are consistent.

## Methods
| Method | Description |
|:------------|:-------------|
| CheckEntity(Type entity_type) | Returns a string describing problems found in an entity definition. |
| GetProperties(Type entity) | Retrieves dictionaries of column, view, and procedure properties. |
| CheckEntities(Assembly? asm = null, string? assembly_name = null) | Checks all entities within an assembly and throws if inconsistencies are found. |

## Remarks
Helpful during development to catch mismatches between entity classes and their definitions.

## See Also
- [EntityDefinition](EntityDefinition.md)
- [EntityBase](EntityBase.md)
