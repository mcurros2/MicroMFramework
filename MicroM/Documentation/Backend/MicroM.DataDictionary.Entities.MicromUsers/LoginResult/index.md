# Class: MicroM.DataDictionary.Entities.MicromUsers.LoginResult
## Overview
Represents the response returned after a successful login.

**Inheritance**
object -> LoginResult

## Example Usage
```csharp
var result = new MicroM.DataDictionary.Entities.MicromUsers.LoginResult();
```
## Properties
| Property | Type | Description |
|:------------|:-------------|:-------------|
| email | string? | Email associated with the user. |
| username | string | Username of the authenticated user. |
| refresh_token | string? | Refresh token issued for the session. |
| client_claims | Dictionary&lt;string, string&gt; | Client-specific claims. |
| authenticator_result | AuthenticatorResult | Result of the authenticator challenge. |

## See Also
- [LoginAttemptResult](../LoginAttemptResult/index.md)
- [LoginAttemptStatus](../LoginAttemptStatus/index.md)
