# Class: MicroM.Data.EntityIndex
## Overview
Describes a database index composed of one or more columns.

**Inheritance**
object -> EntityIndex

**Implements**
None

## Example Usage
```csharp
var idx = new EntityIndex("IX_Test", col1, col2);
```
## Remarks
None.

## Constructors
| Constructor | Description |
|:------------|:-------------|
| EntityIndex(string name = "", params ColumnBase[] keys) | Creates an index from column definitions. |
| EntityIndex(string name = "", params string[] keys) | Creates an index from column names. |

## Properties
| Property | Type | Description |
|:------------|:-------------|:-------------|
| Keys | string[] | Columns that compose the index. |
| Name | string | Name of the index. |

## Methods
| Method | Description |
|:------------|:-------------|
| None | |

