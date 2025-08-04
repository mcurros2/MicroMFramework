# Namespace: MicroM.Configuration

The `MicroM.Configuration` namespace contains classes that are used to configure the behavior of the MicroM framework. It follows the .NET Options pattern, allowing for strongly-typed configuration that can be loaded from `appsettings.json` or other configuration sources.

## Key Classes and Interfaces

### `MicroMOptions`
This is the root options class for the framework. It contains settings that apply globally across the framework, such as the base path for the API.

### `ApplicationOption`
This class holds the configuration for a single application, which is particularly useful in multi-tenant environments.

*   **Purpose**: To define all the necessary settings for one application instance, including its ID, database connection details (`SQLServer`, `SQLDB`, etc.), and behavior settings (e.g., JWT expiration times, account lockout policies).

### `SecretsOptions`
A dedicated class for holding sensitive information, such as API keys or encryption keys, that should be loaded from a secure source like Azure Key Vault or .NET User Secrets.

### `IDatabaseSchema`
An interface that your application can implement to define its database schema. This includes specifying which entities, menus, and user groups should be created and managed by the framework's database tools.

### `IPublicEndpoints`
An interface your application can implement to designate specific API routes as public, meaning they can be accessed without authentication. This is useful for things like a public-facing data view or a "contact us" form.

### `ConfigurationDefaults` / `DataDefaults`
These static classes provide the default values for various framework settings, ensuring that the system can function with minimal explicit configuration.
