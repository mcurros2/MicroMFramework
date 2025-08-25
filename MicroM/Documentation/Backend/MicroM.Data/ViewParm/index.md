# Class: MicroM.Data.ViewParm
## Overview
Represents a parameter used in a ViewDefinition.

**Inheritance**
object -> ViewParm

**Implements**
None

## Example Usage
```csharp
var parm = new ViewParm(col);
```
## Remarks
None.

## Constructors
| Constructor | Description |
|:------------|:-------------|
| ViewParm(ColumnBase column, int column_mapping = -1, string compound_group = "", int compound_position = -1, bool compound_key = false, bool browsing_key = false) | Creates the view parameter. |

## Properties
| Property | Type | Description |
|:------------|:-------------|:-------------|
| ColumnMapping | int | Column mapping position. |
| CompoundGroup | string | Group identifier for compound keys. |
| CompoundPosition | int | Position within a compound key group. |
| CompoundKey | bool | Indicates participation in a compound key. |
| BrowsingKey | bool | Indicates whether used as browsing key. |
| Column | ColumnBase | Underlying column definition. |

## Methods
| Method | Description |
|:------------|:-------------|
| None | |

