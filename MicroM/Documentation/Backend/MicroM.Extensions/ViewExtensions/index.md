# Class: MicroM.Extensions.ViewExtensions
## Overview
Extensions for view parameter collections.

**Inheritance**
object -> ViewExtensions

**Implements**
None

## Example Usage
```csharp
var columns = parms.ToColumnBaseEnumerable();
```
## Methods
| Method | Description |
|:------------|:-------------|
| ToColumnBaseEnumerable(Dictionary<string, ViewParm> parms) | Returns an enumerable of `ColumnBase` from view parameters. |
| FilterByName(Dictionary<string, ColumnBase> parms, string[]? include = null, string[]? exclude = null) | Filters columns by name. |

## Remarks
None.

