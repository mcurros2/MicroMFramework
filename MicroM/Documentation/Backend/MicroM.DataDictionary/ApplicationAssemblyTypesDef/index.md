# Class: MicroM.DataDictionary.ApplicationAssemblyTypesDef
## Overview
Definition for application assembly type records.

**Inheritance**
EntityDefinition -> ApplicationAssemblyTypesDef

## Constructors
| Constructor | Description |
|:------------|:-------------|
| ApplicationAssemblyTypesDef() | Initializes the definition with default metadata. |

## Fields
| Field | Type | Description |
|:------------|:-------------|:-------------|
| c_application_id | Column&lt;string&gt; | Application identifier. |
| c_assembly_id | Column&lt;string&gt; | Assembly identifier. |
| i_order | Column&lt;int&gt; | Display order of the assembly type. |
| c_assemblytype_id | Column&lt;string&gt; | Assembly type identifier. |
| apt_brwStandard | ViewDefinition | Standard browse view definition. |
| APTGetCode | APTGetCode | Procedure helper to retrieve assembly type code. |

## See Also
- [ApplicationAssemblyTypes](../ApplicationAssemblyTypes/index.md)
