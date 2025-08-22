# Class: MicroM.Data.ProcedureDefinition
## Overview
Describes a stored procedure and its parameters for entity operations.

**Inheritance**
object -> ProcedureDefinition

**Implements**
None

## Example Usage
```csharp
var proc = new ProcedureDefinition("my_proc");
```
## Remarks
None.

## Constructors
| Constructor | Description |
|:------------|:-------------|
| ProcedureDefinition(string? name = "", bool readonly_locks = false, bool is_lookup = false, params ColumnBase[] parms) | Creates a procedure definition. |
| ProcedureDefinition(bool readonly_locks = false, bool is_lookup = false, bool is_import = false, params string[] parms) | Creates a definition from column names. |

## Properties
| Property | Type | Description |
|:------------|:-------------|:-------------|
| Name | string | Name of the procedure. |
| Parms | Dictionary<string, ColumnBase> | Parameters defined for the procedure. |
| ReadonlyLocks | bool | Indicates whether read-only locks are used. |

## Methods
| Method | Description |
|:------------|:-------------|
| AddParmFromCol(...) | Adds a parameter from a column definition. |
| AddParmsFromCols(...) | Adds multiple parameters. |

## See Also
-
