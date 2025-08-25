# Class: MicroM.DataDictionary.ConfigurationDBDef
## Overview
Schema definition for configuration database settings.

**Inheritance**
EntityDefinition -> ConfigurationDBDef

## Constructors
| Constructor | Description |
|:------------|:-------------|
| ConfigurationDBDef() | Initializes the configuration database definition. |

## Fields
| Field | Type | Description |
|:------------|:-------------|:-------------|
| c_confgidb_id | Column<string> | Fixed identifier for configuration database records. |
| vc_configsqlserver | Column<string> | SQL Server host name. |
| vc_configsqluser | Column<string> | SQL Server user name. |
| vc_configsqlpassword | Column<string?> | SQL Server password. |
| vc_configdatabase | Column<string> | Configuration database name. |
| vc_certificatethumbprint | Column<string> | Certificate thumbprint used for encryption. |
| vc_certificatepassword | Column<string> | Password for the certificate. |
| vc_certificatename | Column<string> | File name of the certificate. |
| b_adminuserhasrights | Column<bool> | Indicates whether the admin user has rights. |
| b_configdbexists | Column<bool> | Indicates whether the configuration database exists. |
| b_configuserexists | Column<bool> | Indicates whether the configuration user exists. |
| b_secretsconfigured | Column<bool> | Indicates whether secrets have been configured. |
| b_defaultcertificate | Column<bool> | Indicates whether a default certificate is being used. |
| b_thumbprintconfigured | Column<bool> | Indicates whether a thumbprint is configured. |
| b_thumbprintfound | Column<bool> | Indicates whether the configured thumbprint is found. |
| b_certificatefound | Column<bool> | Indicates whether the certificate file is found. |
| b_secretsfilevalid | Column<bool> | Indicates whether the secrets file is valid. |
| b_recreatedatabase | Column<bool> | Indicates whether the database should be recreated. |
| dfg_brwStandard | ViewDefinition | Standard browse view. |

## See Also
- [ConfigurationDB](../ConfigurationDB/index.md)
