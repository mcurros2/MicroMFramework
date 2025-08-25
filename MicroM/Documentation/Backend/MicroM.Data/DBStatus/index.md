# Class: MicroM.Data.DBStatus
## Overview
Represents the status returned from a database operation.

**Inheritance**
object -> DBStatus

**Implements**
None

## Example Usage
```csharp
var status = new MicroM.Data.DBStatus(MicroM.Data.DBStatusCodes.OK);
```
## Constructors
| Constructor | Description |
|:------------|:-------------|
| DBStatus(DBStatusCodes status, string? message = null) | Initializes with a status code and optional message. |
| DBStatus() | Initializes with default values. |

## Properties
| Property | Type | Description |
|:------------|:-------------|:-------------|
| Status | DBStatusCodes | Status code for the operation. |
| Message | string? | Optional descriptive message. |

## Remarks
None.

## See Also
- [DBStatusCodes](../DBStatusCodes/index.md)
