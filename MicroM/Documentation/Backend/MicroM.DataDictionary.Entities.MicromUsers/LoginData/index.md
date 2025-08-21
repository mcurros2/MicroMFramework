# Class: MicroM.DataDictionary.Entities.MicromUsers.LoginData
## Overview
Represents authentication and status information retrieved for a user.

**Inheritance**
object -> LoginData

## Example Usage
```csharp
var data = new MicroM.DataDictionary.Entities.MicromUsers.LoginData();
```

## Properties
| Property | Type | Description |
|:------------|:-------------|:-------------|
| user_id | string | Unique identifier of the user. |
| locked | bool | Indicates whether the account is locked. |
| pwhash | string | Hash of the user's password. |
| badlogonattempts | int | Number of failed login attempts. |
| locked_minutes_remaining | int | Minutes remaining before lockout expires. |
| email | string? | Email address associated with the account. |
| username | string | Username for the account. |
| disabled | bool | Indicates whether the account is disabled. |
| refresh_token | string? | Refresh token issued to the user. |
| refresh_expired | bool | Indicates if the refresh token has expired. |
| usertype_id | string? | Identifier of the user type. |
| usertype_name | string? | Name of the user type. |
| user_groups | string? | JSON array string of groups the user belongs to. |

## See Also
- [LoginAttemptResult](../LoginAttemptResult/index.md)
- [RefreshTokenResult](../RefreshTokenResult/index.md)
