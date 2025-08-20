# Class: MicroM.Configuration.SecurityDefaults

## Overview
Provides temporary encryption material used during application startup for securing tokens and sensitive data.

## Fields
| Field | Type | Description |
|:--|:--|:--|
| TempEncryptionIV | byte[] | Initialization vector valid for the application's lifetime. |
| TempEncryptionKey | byte[] | Symmetric key valid for the application's lifetime. |

## Remarks
These values are generated at runtime and should not be persisted.

## See Also
- [AllowedRouteFlags](AllowedRouteFlags.md)
- [Backend Namespaces](../index.md)
