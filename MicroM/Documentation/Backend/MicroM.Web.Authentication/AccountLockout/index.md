# Class: MicroM.Web.Authentication.AccountLockout
## Overview
Tracks failed login attempts and refresh token validation to enforce account lockouts.

**Inheritance**
object -> AccountLockout

**Implements**
None

## Remarks
Used by SQLServerAuthenticator to cache lockout state and evaluate login attempts.

## Properties
| Property | Type | Description |
|:------------|:-------------|:-------------|
| BadLogonAttempts | int | Count of consecutive failed logins; more than ten locks the account. |
| LockedUntil | DateTime? | When the current lockout expires; null means no lock. |
| RefreshToken | string? | Refresh token issued for the account. |
| RefreshTokenExpiration | DateTime? | Expiration timestamp for the refresh token. |
| RefreshTokenValidationCount | int | Number of times the refresh token has been validated. |

## Methods
| Method | Description |
|:------------|:-------------|
| isAccountLocked | Determines whether the account is currently locked. |
| unlockAccount | Clears the lockout state and resets counters. |
| incrementBadLogonAndLock | Increments failed logon counter and locks the account after threshold. |
| validateRefreshToken | Validates a refresh token against expiration and usage limits. |
| incrementRefreshTokenValidationCount | Increments the number of refresh token validations. |
| clearRefreshToken | Clears the refresh token and associated validation state. |
| setRefreshToken | Sets a new refresh token and resets its validation state. |
| getRefreshExpiration | Gets the expiration time for the current refresh token. |

## See Also
- [SQLServerAuthenticator](../SQLServerAuthenticator/index.md)
