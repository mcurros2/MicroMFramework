# Class: MicroM.DataDictionary.EntitiesAssembliesDef
## Overview
Definition for assembly records.

**Inheritance**
EntityDefinition -> EntitiesAssembliesDef

## Constructors
| Constructor | Description |
|:------------|:-------------|
| EntitiesAssembliesDef() | Initializes the assembly definition. |

## Fields
| Field | Type | Description |
|:------------|:-------------|:-------------|
| c_assembly_id | Column<string> | Identifier of the assembly. |
| vc_assemblypath | Column<string> | Path to the assembly file. |
| eas_brwStandard | ViewDefinition | Standard browse view. |
| UNAssemblies | EntityUniqueConstraint | Unique constraint enforcing path uniqueness. |
| eas_dropUnusedAssemblies | ProcedureDefinition | Procedure removing unused assemblies. |

## See Also
- [EntitiesAssemblies](../EntitiesAssemblies/index.md)
