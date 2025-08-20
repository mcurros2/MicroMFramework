# Class: MicroM.Configuration.SecretsOptions

## Overview
Options for storing sensitive configuration values such as database credentials.

## Constructors
| Constructor | Description |
|:------------|:-------------|
| SecretsOptions() | Creates a new instance with default values. |

## Properties
| Property | Type | Description |
|:------------|:-------------|:-------------|
| ConfigSQLUser | string? | SQL user for the configuration database. |
| ConfigSQLPassword | string? | Password for the configuration database user. |

## Remarks
Use external storage to protect secrets in production environments.

## See Also
- [ConfigurationDefaults](ConfigurationDefaults.md)
