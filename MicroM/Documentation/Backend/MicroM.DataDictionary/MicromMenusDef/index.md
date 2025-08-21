# Class: MicroM.DataDictionary.MicromMenusDef
## Overview
Schema definition for MicroM menus.

**Inheritance**
EntityDefinition -> MicromMenusDef

**Implements**
None

## Constructors
| Constructor | Description |
|:------------|:-------------|
| MicromMenusDef() | Initializes a new instance. |

## Properties
| Property | Type | Description |
|:------------|:-------------|:-------------|
| c_menu_id | Column&lt;string&gt; | Primary key column for the menu identifier. |
| vc_menu_name | Column&lt;string&gt; | Descriptive name of the menu. |
| dt_last_route_updated | Column&lt;DateTime&gt;? | Timestamp of the last route update. |
| mme_brwStandard | ViewDefinition | Browse view keyed by menu ID. |

## Remarks
None.

## See Also
-

