# Class: MicroM.DataDictionary.Entities.MicromUsers.RefreshTokenResult
## Overview
Result of a refresh token request.

**Inheritance**
object -> RefreshTokenResult

## Example Usage
```csharp
var result = new MicroM.DataDictionary.Entities.MicromUsers.RefreshTokenResult();
```

## Properties
| Property | Type | Description |
|:------------|:-------------|:-------------|
| Status | LoginAttemptStatus | Outcome of the refresh attempt. |
| Message | string? | Optional message describing the result. |
| RefreshToken | string? | Newly issued refresh token. |
| RefreshExpiration | DateTime? | Expiration time for the refresh token. |

## See Also
- [LoginAttemptStatus](../LoginAttemptStatus/index.md)
- [LoginAttemptResult](../LoginAttemptResult/index.md)
