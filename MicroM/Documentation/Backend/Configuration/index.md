# Configuration Namespace

Provides options and defaults for the MicroM framework. These settings control database connections, security, and application behavior.

## Related Types
- `ApplicationOption`
- `ConfigurationDefaults`
- `DataDefaults`
- `IDatabaseSchema`
- `IPublicEndpoints`
- `MicroMOptions`
- `SecretsOptions`

## ApplicationOption
<xref:MicroM.Configuration.ApplicationOption>

Defines application-specific settings such as database connections, JWT parameters, and allowed frontend origins.

## ConfigurationDefaults
<xref:MicroM.Configuration.ConfigurationDefaults>

Constants used when generating configuration files, including default database names, certificate details, and upload settings.

## DataDefaults
<xref:MicroM.Configuration.DataDefaults>

Defines defaults for data access operations like timeouts and row limits.

## IDatabaseSchema
<xref:MicroM.Configuration.IDatabaseSchema>

Interface for creating and migrating the MicroM database schema.

## IPublicEndpoints
<xref:MicroM.Configuration.IPublicEndpoints>

Declares routes that should be exposed without authentication.

## MicroMOptions
<xref:MicroM.Configuration.MicroMOptions>

Common configuration values:

- `DefaultConnectionTimeOutInSecs` – default SQL connection timeout.
- `DefaultCommandTimeOutInMins` – default command timeout.
- `UploadsFolder` – path for uploaded files.

## SecretsOptions
<xref:MicroM.Configuration.SecretsOptions>

Stores database credentials securely.
