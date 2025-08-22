# Class: MicroM.Data.EntityLookup
## Overview
Defines lookup behavior for retrieving records via views and procedures.

**Inheritance**
object -> EntityLookup

**Implements**
None

## Example Usage
```csharp
var lookup = new EntityLookup("view", "proc", 0, 1);
```
## Remarks
None.

## Constructors
| Constructor | Description |
|:------------|:-------------|
| EntityLookup(string view, string lookup, int id_index, int description_index, string? key_parameter = null, string compound_key_group = "") | Creates a lookup definition. |

## Properties
| Property | Type | Description |
|:------------|:-------------|:-------------|
| ViewName | string | View used for the lookup. |
| LookupProcName | string | Procedure to execute the lookup. |
| KeyParameter | string? | Parameter used for compound keys. |
| CompoundKeyGroup | string | Group identifier for compound keys. |
| DescriptionColumnIndex | int | Index of the description column. |
| IDColumnIndex | int | Index of the ID column. |

## Methods
| Method | Description |
|:------------|:-------------|
| None | |

## See Also
-
