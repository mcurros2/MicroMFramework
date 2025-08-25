# Class: MicroM.DataDictionary.EntitiesAssembliesTypesDef
## Overview
Definition for assembly type mappings.

**Inheritance**
EntityDefinition -> EntitiesAssembliesTypesDef

## Constructors
| Constructor | Description |
|:------------|:-------------|
| EntitiesAssembliesTypesDef() | Initializes the assembly types definition. |

## Fields
| Field | Type | Description |
|:------------|:-------------|:-------------|
| c_assembly_id | Column<string> | Identifier of the assembly. |
| c_assemblytype_id | Column<string> | Identifier of the assembly type. |
| vc_assemblytypename | Column<string> | Name of the assembly type. |
| eat_brwStandard | ViewDefinition | Standard browse view. |
| eat_deleteAllTypes | ProcedureDefinition | Procedure that removes all types. |

## See Also
- [EntitiesAssembliesTypes](../EntitiesAssembliesTypes/index.md)
