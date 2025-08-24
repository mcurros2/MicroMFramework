# Namespace: MicroM.Web.Authentication
## Overview
Authentication utilities for web apps.

## Classes
| Class | Description |
|:------------|:-------------|
| [BrowserDeviceIDService](BrowserDeviceIDService/index.md) | Builds device IDs from the current HTTP context. |
| [MicroMCorsPolicyProvider](MicroMCorsPolicyProvider/index.md) | Generates CORS policies based on MicroM configuration. |
| [UserPasswordHasher](UserPasswordHasher/index.md) | Hashes and verifies user passwords. |
| [UserRecoveryEmail](UserRecoveryEmail/index.md) | Holds user information for password recovery emails. |
| [UserRecoverPassword](UserRecoverPassword/index.md) | Carries data needed to reset a user's password. |
| [MicroMAuthenticator](MicroMAuthenticator/index.md) | Handles login and refresh token logic for MicroM accounts. |
| [AccountLockout](AccountLockout/index.md) | Tracks failed logins and refresh tokens to enforce lockouts. |
| [SQLServerAuthenticator](SQLServerAuthenticator/index.md) | Authenticates users with SQL Server credentials and refresh tokens. |
| [MicroMClientClaimTypes](MicroMClientClaimTypes/index.md) | Defines client claim type constants. |
| [MicroMServerClaimTypes](MicroMServerClaimTypes/index.md) | Defines server claim type constants. |
| [WebAPIJwtPostConfigurationOptions](WebAPIJwtPostConfigurationOptions/index.md) | Registers a custom JWT handler after configuration. |
| [TokenResult](TokenResult/index.md) | Holds a generated JWT and its descriptor. |
| [WebAPIJsonWebTokenHandler](WebAPIJsonWebTokenHandler/index.md) | Creates and validates encrypted JWT tokens for APIs. |
| [UserLogin](UserLogin/index.md) | Model for user credentials and device ID. |
| [MicroMAuthenticationProvider](MicroMAuthenticationProvider/index.md) | Selects an authenticator based on application settings. |
| [AuthenticatorResult](AuthenticatorResult/index.md) | Stores outcomes of an authentication attempt. |
| [UserRefreshTokenRequest](UserRefreshTokenRequest/index.md) | Contains data for requesting a new access token. |

## Interfaces
| Interface | Description |
|:------------|:-------------|
| [IDeviceIdService](IDeviceIdService/index.md) | Interface for device ID service. |
| [IAuthenticationProvider](IAuthenticationProvider/index.md) | Interface for authentication provider. |
| [IAuthenticator](IAuthenticator/index.md) | Interface for authenticator. |

## Remarks
None.

