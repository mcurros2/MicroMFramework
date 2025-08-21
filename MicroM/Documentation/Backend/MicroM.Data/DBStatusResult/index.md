# Class: MicroM.Data.DBStatusResult
## Overview
Aggregates multiple database statuses.

**Inheritance**
object -> DBStatusResult

**Implements**
None

## Example Usage
```csharp
var result = new MicroM.Data.DBStatusResult();
```
## Constructors
| Constructor | Description |
|:------------|:-------------|
| DBStatusResult() | Creates an empty status result. |

## Properties
| Property | Type | Description |
|:------------|:-------------|:-------------|
| Failed | bool | Indicates if any status represents a failure. |
| AutonumReturned | bool | Indicates if an auto-number value was returned. |
| Results | List<DBStatus>? | Collection of status entries. |

## Remarks
None.

## See Also
- [DBStatus](../DBStatus/index.md)
