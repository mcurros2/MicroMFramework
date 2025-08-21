# Class: MicroM.DataDictionary.Entities.MicromUsers.LoginAttemptResult
## Overview
Represents the result of a login attempt including status and optional tokens.

**Inheritance**
object -> LoginAttemptResult

## Example Usage
```csharp
var attempt = new MicroM.DataDictionary.Entities.MicromUsers.LoginAttemptResult();
```
## Properties
| Property | Type | Description |
|:------------|:-------------|:-------------|
| Status | LoginAttemptStatus | Status of the login attempt. |
| Message | string? | Optional message describing the result. |
| RefreshToken | string? | Refresh token issued when the attempt succeeds. |

## See Also
- [LoginAttemptStatus](../LoginAttemptStatus/index.md)
- [LoginResult](../LoginResult/index.md)
