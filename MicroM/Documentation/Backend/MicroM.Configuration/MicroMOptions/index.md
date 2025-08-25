# Class: MicroM.Configuration.MicroMOptions
## Overview
Options for configuring core MicroM behaviors and defaults.

**Inheritance**
object -> MicroMOptions

**Implements**
None

## Example Usage
```csharp
var options = new MicroM.Configuration.MicroMOptions();
```
## Properties
| Property | Type | Description |
|:------------|:-------------|:-------------|
| MicroM | string | Configuration section name used in application settings. |
| DefaultConnectionTimeOutInSecs | int | Default SQL connection timeout in seconds. |
| DefaultCommandTimeOutInMins | int | Default command timeout in minutes. |
| DefaultRowLimitForViews | int | Default row limit for views. |
| ConfigSQLServer | string? | Configuration database server name. |
| ConfigSQLServerDB | string? | Name of the configuration database. |
| CertificateThumbprint | string? | Thumbprint of the certificate used for encryption. |
| UploadsFolder | string? | Folder used for uploaded files. |
| AllowedUploadFileExtensions | string[]? | Allowed file extensions for uploads. |
| MicroMAPIBaseRootPath | string? | Base path for the MicroM API. |
| MicroMAPICookieRootPath | string? | Cookie root path for the MicroM API. |
| DefaultSQLDatabaseCollation | string? | Default SQL collation for new databases. |

## Remarks
None.

