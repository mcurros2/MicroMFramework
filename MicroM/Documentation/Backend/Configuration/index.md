# Configuration Namespace

Provides options and defaults for the MicroM framework. These settings control database connections, security, and application behavior.

## Related Types
- [`ApplicationOption`](../../core/Configuration/ApplicationOption.cs)
- [`ConfigurationDefaults`](../../core/Configuration/ConfigurationDefaults.cs)
- [`DataDefaults`](../../core/Configuration/DataDefaults.cs)
- [`IDatabaseSchema`](../../core/Configuration/IDatabaseSchema.cs)
- [`IPublicEndpoints`](../../core/Configuration/IPublicEndpoints.cs)
- [`MicroMOptions`](../../core/Configuration/MicroMOptions.cs)
- [`SecretsOptions`](../../core/Configuration/SecretsOptions.cs)

## ApplicationOption
`ApplicationOption` defines application-specific settings such as database connections, JWT parameters, and allowed frontend origins【F:core/Configuration/ApplicationOption.cs†L8-L40】.

## ConfigurationDefaults
`ConfigurationDefaults` provides constants used when generating configuration files, including default database names, certificate details, and upload settings【F:core/Configuration/ConfigurationDefaults.cs†L6-L38】.

## DataDefaults
`DataDefaults` defines defaults for data access operations like timeouts and row limits【F:core/Configuration/DataDefaults.cs†L3-L38】.

## IDatabaseSchema
`IDatabaseSchema` is an interface for creating and migrating the MicroM database schema【F:core/Configuration/IDatabaseSchema.cs†L20-L37】.

## IPublicEndpoints
`IPublicEndpoints` declares routes that should be exposed without authentication【F:core/Configuration/IPublicEndpoints.cs†L3-L13】.

## MicroMOptions
`MicroMOptions` holds common configuration values such as default timeouts and upload settings【F:core/Configuration/MicroMOptions.cs†L3-L33】.

## SecretsOptions
`SecretsOptions` stores database credentials securely【F:core/Configuration/SecretsOptions.cs†L3-L13】.
