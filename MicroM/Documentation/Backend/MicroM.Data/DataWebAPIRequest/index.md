# Class: MicroM.Data.DataWebAPIRequest
## Overview
Container for Web API data requests.

**Inheritance**
object -> DataWebAPIRequest

**Implements**
None

## Example Usage
```csharp
var request = new MicroM.Data.DataWebAPIRequest();
```
## Constructors
| Constructor | Description |
|:------------|:-------------|
| DataWebAPIRequest() | Creates an empty request container. |

## Properties
| Property | Type | Description |
|:------------|:-------------|:-------------|
| ParentKeys | Dictionary<string, object>? | Parent key values for the operation. |
| Values | Dictionary<string, object> | Values for the target entity. |
| RecordsSelection | List<Dictionary<string, object>> | Records selected for processing. |
| ServerClaims | Dictionary<string, object>? | Optional server claim values. |

## Remarks
None.

## See Also
-
