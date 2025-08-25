# Class: MicroM.DataDictionary.ConfigurationDB
## Overview
Runtime entity for accessing and validating configuration database settings.

**Inheritance**
Entity<ConfigurationDBDef> -> ConfigurationDB

## Constructors
| Constructor | Description |
|:------------|:-------------|
| ConfigurationDB() | Initializes a new instance. |
| ConfigurationDB(IEntityClient ec, IMicroMEncryption? encryptor = null) | Initializes with a database client and optional encryptor. |

## Methods
| Method | Description |
|:------------|:-------------|
| GetData(CancellationToken ct, MicroMOptions? options = null, Dictionary<string, object>? server_claims = null, IWebAPIServices? api = null, string? app_id = null) | Retrieves configuration data using the provided options and claims. |
| UpdateData(CancellationToken ct, bool throw_dbstat_exception = false, MicroMOptions? options = null, Dictionary<string, object>? server_claims = null, IWebAPIServices? api = null, string? app_id = null) | Updates configuration data using the provided options and claims. |

## See Also
- [ConfigurationDBDef](../ConfigurationDBDef/index.md)
- [InitialConfigurationResult](../InitialConfigurationResult/index.md)
