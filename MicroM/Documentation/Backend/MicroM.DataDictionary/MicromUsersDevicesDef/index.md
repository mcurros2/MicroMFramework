# Class: MicroM.DataDictionary.MicromUsersDevicesDef
## Overview
Schema definition for devices associated with MicroM users.

**Inheritance**
EntityDefinition -> MicromUsersDevicesDef

## Constructors
| Constructor | Description |
|:------------|:-------------|
| MicromUsersDevicesDef() | Initializes a new instance. |

## Properties
| Property | Type | Description |
|:------------|:-------------|:-------------|
| c_user_id | Column<string> | User identifier. |
| c_device_id | Column<string> | Device identifier. |
| vc_useragent | Column<string?> | User agent string reported by the device. |
| vc_ipaddress | Column<string?> | IP address of the device. |
| vc_refreshtoken | Column<string?> | Refresh token assigned to the device. |
| dt_refresh_expiration | Column<DateTime?> | Expiration time of the refresh token. |
| i_refreshcount | Column<int> | Number of times the token has been refreshed. |
| usd_brwStandard | ViewDefinition | Default browse view definition. |
| FKMicromUsers | EntityForeignKey<MicromUsers, MicromUsersDevices> | Relationship to the owning user. |
| usd_refreshToken | ProcedureDefinition | Procedure to refresh a device token. |

## See Also
- [MicromUsersDevices](../MicromUsersDevices/index.md)
