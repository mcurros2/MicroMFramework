# Class: MicroM.DataDictionary.ConfigurationDB
## Overview
Entity used to manage configuration database settings.

**Inheritance**
Entity<ConfigurationDBDef> -> ConfigurationDB

## Constructors
| Constructor | Description |
|:------------|:-------------|
| ConfigurationDB() | Default constructor. |
| ConfigurationDB(IEntityClient, IMicroMEncryption?) | Creates a ConfigurationDB entity with the specified client and encryptor. |

## Methods
| Method | Description |
|:------------|:-------------|
| GetData(CancellationToken, MicroMOptions?, Dictionary<string, object>?, IWebAPIServices?, string?) | Retrieves configuration data. |
| UpdateData(CancellationToken, bool, MicroMOptions?, Dictionary<string, object>?, IWebAPIServices?, string?) | Updates configuration data. |

## See Also
- [ConfigurationDBDef](../ConfigurationDBDef/index.md)
- [ConfigurationDBHandlers](../ConfigurationDBHandlers/index.md)
