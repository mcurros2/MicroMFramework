# Class: MicroM.Configuration.ApplicationOption
## Overview
Configuration settings for a MicroM application including database and authentication options.

**Inheritance**
object -> ApplicationOption

**Implements**
None

## Example Usage
```csharp
var options = new MicroM.Configuration.ApplicationOption();
```
## Properties
| Property | Type | Description |
|:------------|:-------------|:-------------|
| ApplicationID | string | Unique identifier for the application. |
| ApplicationName | string | Display name for the application. |
| SQLServer | string | SQL Server connection information. |
| SQLUser | string | User name for the SQL Server connection. |
| SQLPassword | string | Password for the SQL Server connection. |
| SQLDB | string | Database name to connect to. |
| JWTIssuer | string | JWT issuer and audience settings. |
| JWTAudience | string? | JWT token audience. |
| JWTKey | string | Symmetric key used to sign JWT tokens. |
| JWTTokenExpirationMinutes | int | Access token expiration in minutes. |
| JWTRefreshExpirationHours | int | Refresh token expiration in hours. |
| AccountLockoutMinutes | int | Lockout duration after repeated failed logins. |
| MaxBadLogonAttempts | int | Maximum failed login attempts before lockout. |
| MaxRefreshTokenAttempts | int | Maximum refresh token attempts before requiring re-login. |
| AuthenticationType | string? | Authentication mechanism used by the application. |
| IdentityProviderRoleType | string? | Role type expected from identity provider. |
| IdentityProviderClients | List<string> | Valid identity provider client identifiers. |
| FrontendURLS | List<string> | Allowed frontend URLs for cross-origin requests. |

## Remarks
None.

