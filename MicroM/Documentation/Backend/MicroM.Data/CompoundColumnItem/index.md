# Class: MicroM.Data.CompoundColumnItem
## Overview
Represents a column participating in a compound key, including its position.

**Inheritance**
object -> CompoundColumnItem

**Implements**
None

## Example Usage
```csharp
var item = new CompoundColumnItem(col, 0, true);
```
## Remarks
None.

## Constructors
| Constructor | Description |
|:------------|:-------------|
| CompoundColumnItem(ColumnBase col, int position, bool compound_key) | Initializes the mapping item. |

## Properties
| Property | Type | Description |
|:------------|:-------------|:-------------|
| Column | ColumnBase | The column definition. |
| Position | int | Position within the compound key. |
| CompoundKey | bool | Indicates participation in the compound key. |

## Methods
| Method | Description |
|:------------|:-------------|
| None | |

