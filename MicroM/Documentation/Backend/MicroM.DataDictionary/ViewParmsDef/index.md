# Class: MicroM.DataDictionary.ViewParmsDef
## Overview
Schema definition for view parameters.

**Inheritance**
EntityDefinition -> ViewParmsDef

## Constructors
| Constructor | Description |
|:------------|:-------------|
| ViewParmsDef() | Initializes a new instance. |

## Properties
| Property | Type | Description |
|:------------|:-------------|:-------------|
| c_object_id | Column<string> | Identifier of the object that owns the view. |
| c_proc_id | Column<int> | Identifier of the procedure. |
| c_viewparm_id | Column<int> | Primary key for the view parameter. |
| vc_parmname | Column<string> | Name of the parameter. |
| i_columnmapping | Column<int?> | Column mapping identifier. |
| vc_compoundgroup | Column<string?> | Compound group name. |
| i_compoundposition | Column<int?> | Position within the compound group. |
| bt_compoundkey | Column<bool> | Indicates membership in a compound key. |
| bt_browsingkey | Column<bool> | Indicates use as a browsing key. |
| vip_brwStandard | ViewDefinition | Default browse view for parameters. |
| FKObjects | EntityForeignKey<Procs, ViewParms> | Relationship to procedure entity. |

## See Also
- [ViewParms](../ViewParms/index.md)
