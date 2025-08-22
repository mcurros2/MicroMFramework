# Class: MicroM.Data.SystemStandardProceduresSuffixs
## Overview
Contains standard suffixes used for system stored procedures.

**Inheritance**
object -> SystemStandardProceduresSuffixs

**Implements**
None

## Example Usage
```csharp
var type = SystemStandardProceduresSuffixs.GetProcStandardType("test_update");
```
## Remarks
None.

## Constructors
| Constructor | Description |
|:------------|:-------------|
| None | Static class cannot be instantiated. |

## Properties
| Property | Type | Description |
|:------------|:-------------|:-------------|
| _update | string | Suffix for update procedures. |
| _drop | string | Suffix for drop procedures. |
| _get | string | Suffix for get procedures. |
| _brwStandard | string | Suffix for standard view procedures. |
| _lookup | string | Suffix for lookup procedures. |
| _iupdate | string | Suffix for incremental update procedures. |
| _idrop | string | Suffix for incremental drop procedures. |

## Methods
| Method | Description |
|:------------|:-------------|
| GetProcStandardType(string? proc_name) | Determines the standard procedure type. |

