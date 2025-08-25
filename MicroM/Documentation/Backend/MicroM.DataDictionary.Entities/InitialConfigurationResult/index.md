# Class: MicroM.DataDictionary.InitialConfigurationResult
## Overview
Represents the result of the initial configuration validation process.

**Inheritance**
EntityActionResult -> InitialConfigurationResult

## Example Usage
```csharp
var result = new MicroM.DataDictionary.InitialConfigurationResult();
```
## Properties
| Property | Type | Description |
|:------------|:-------------|:-------------|
| ConfigDBValid | bool | Indicates whether the configuration database values are valid. |
| ConfigUserameValid | bool | Indicates whether the configuration username is valid. |
| ConfigPasswordValid | bool | Indicates whether the configuration password is valid. |
| ConfigDBExists | bool | Indicates whether the configuration database exists. |
| ConfigUserExists | bool | Indicates whether the configuration user exists. |
| AdminUserHasRights | bool | Indicates whether the administrator user has rights. |
| CertificateThumbprint | string? | Certificate thumbprint used for encryption. |
| CertificatePath | string? | Path to the certificate file. |
| CertificatePassword | string? | Password for the certificate. |
| ConfigSQLServer | string? | SQL Server instance configured for the application. |
| ConfigSQLServerDB | string? | Configuration database name. |
| ConfigSQLUser | string? | SQL user name for configuration. |
| ConfigSQLPassword | string? | SQL password for configuration. |

## See Also
- [ConfigurationDB](../ConfigurationDB/index.md)
