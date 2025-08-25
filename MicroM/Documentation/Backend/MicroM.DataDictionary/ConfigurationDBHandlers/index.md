# Class: MicroM.DataDictionary.ConfigurationDBHandlers
## Overview
Helper methods for managing configuration database settings and secrets.

## Methods
| Method | Description |
|:------------|:-------------|
| ReadConfigurationDBParms(string, CancellationToken) | Reads configuration database parameters from the encrypted secrets file. |
| HandleGetData(ConfigurationDB, MicroMOptions, Dictionary<string, object>, CancellationToken) | Retrieves configuration database settings and status information. |
| HandleUpdateData(ConfigurationDB, bool, MicroMOptions, Dictionary<string, object>, IWebAPIServices?, CancellationToken) | Updates configuration database settings and creates the database if required. |

## See Also
- [ConfigurationDB](../ConfigurationDB/index.md)
