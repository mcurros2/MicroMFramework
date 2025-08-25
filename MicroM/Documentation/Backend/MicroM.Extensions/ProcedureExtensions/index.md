# Class: MicroM.Extensions.ProcedureExtensions
## Overview
Helpers for populating procedure parameters from dictionaries and column collections.

**Inheritance**
object -> ProcedureExtensions

**Implements**
None

## Example Usage
```csharp
proc.SetParmsValues(values);
```
## Methods
| Method | Description |
|:------------|:-------------|
| SetParmsValues(ProcedureDefinition proc, Dictionary<string, object> values) | Sets procedure parameters from a dictionary of values. |
| SetParmsValues(ProcedureDefinition proc, IReadonlyOrderedDictionary<ColumnBase> cols) | Sets procedure parameters from a column collection. |

## Remarks
None.

