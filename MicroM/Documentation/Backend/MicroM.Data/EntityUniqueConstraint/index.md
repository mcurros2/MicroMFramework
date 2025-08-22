# Class: MicroM.Data.EntityUniqueConstraint
## Overview
Represents a unique constraint composed of one or more columns.

**Inheritance**
object -> EntityUniqueConstraint

**Implements**
None

## Example Usage
```csharp
var uq = new EntityUniqueConstraint("UQ_Test", col1);
```
## Remarks
None.

## Constructors
| Constructor | Description |
|:------------|:-------------|
| EntityUniqueConstraint(string name = "", params ColumnBase[] keys) | Creates a constraint from column definitions. |
| EntityUniqueConstraint(string name = "", params string[] keys) | Creates a constraint from column names. |

## Properties
| Property | Type | Description |
|:------------|:-------------|:-------------|
| Keys | string[] | Columns that form the constraint. |
| Name | string | Name of the constraint. |

## Methods
| Method | Description |
|:------------|:-------------|
| None | |

## See Also
-
