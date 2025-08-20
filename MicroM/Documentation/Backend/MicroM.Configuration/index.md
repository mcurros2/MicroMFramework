# Namespace: MicroM.Configuration

## Overview
Provides core configuration abstractions and defaults for MicroM.

## Classes
| Class | Description |
|:------------|:-------------|
| [ApplicationOption](ApplicationOption.md) | Configuration settings for a MicroM application including database and authentication options. |
| [ConfigurationDefaults](ConfigurationDefaults.md) | Default configuration values used when no explicit settings are provided. |
| [DataDefaults](DataDefaults.md) | Default values for data access operations. |
| [MicroMOptions](MicroMOptions.md) | Options for configuring core MicroM behaviors and defaults. |
| [SecretsOptions](SecretsOptions.md) | Options for storing sensitive configuration values such as database credentials. |
| [SecurityDefaults](SecurityDefaults.md) | Temporary encryption keys used during startup. |

## Enums
| Enum | Description |
|:------------|:-------------|
| [DatabaseMigrationResult](DatabaseMigrationResult.md) | Result values for database migration operations. |
| [AllowedRouteFlags](AllowedRouteFlags.md) | Flags specifying unauthenticated route permissions. |

## Interfaces
| Interface | Description |
|:------------|:-------------|
| [IDatabaseSchema](IDatabaseSchema.md) | Provides methods for creating and migrating the MicroM database schema. |
| [IPublicEndpoints](IPublicEndpoints.md) | Defines routes that should be accessible without authentication. |

## Remarks
Configuration components underpin the setup and default behavior of MicroM applications.

## See Also
- [Backend Namespaces](../index.md)
