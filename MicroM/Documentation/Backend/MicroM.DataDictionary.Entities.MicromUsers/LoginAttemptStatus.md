# Enum: MicroM.DataDictionary.Entities.MicromUsers.LoginAttemptStatus

## Overview
Enumerates possible outcomes when processing user login attempts or refresh tokens.

## Values
| Value | Description |
|:--|:--|
| Updated | Login attempt updated successfully. |
| InvalidRefreshToken | Provided refresh token is invalid. |
| RefreshTokenExpired | Refresh token has expired. |
| MaxRefreshReached | User exceeded maximum refresh attempts. |
| UserIDNotFound | User identifier not found. |
| AccountLocked | Account is locked. |
| AccountDisabled | Account is disabled. |
| RefreshTokenValid | Refresh token is valid. |
| LoggedInOK | Credentials valid and user logged in. |
| Unknown | Status could not be determined. |

## Remarks
Used by authentication services to track login outcomes and security policies.
