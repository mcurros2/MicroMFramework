# Class: MicroM.DataDictionary.ConfigurationDBDef
## Overview
Schema definition for configuration database settings.

**Inheritance**
EntityDefinition -> ConfigurationDBDef

## Constructors
| Constructor | Description |
|:------------|:-------------|
| ConfigurationDBDef() | Initializes a new instance. |

## Properties
| Property | Type | Description |
|:------------|:-------------|:-------------|
| c_confgidb_id | Column<string> | Primary identifier for the configuration record. |
| vc_configsqlserver | Column<string> | SQL server hosting the configuration database. |
| vc_configsqluser | Column<string> | SQL login used for configuration operations. |
| vc_configsqlpassword | Column<string?> | Password for the configuration SQL login. |
| vc_configdatabase | Column<string> | Name of the configuration database. |
| vc_certificatethumbprint | Column<string> | Thumbprint of the certificate used for encryption. |
| vc_certificatepassword | Column<string> | Password protecting the certificate key. |
| vc_certificatename | Column<string> | Display name of the certificate. |
| b_adminuserhasrights | Column<bool> | Indicates administrator rights. |
| b_configdbexists | Column<bool> | Indicates if the configuration database exists. |
| b_configuserexists | Column<bool> | Indicates if the configuration user exists. |
| b_secretsconfigured | Column<bool> | Indicates if secrets have been configured. |
| b_defaultcertificate | Column<bool> | Indicates if the default certificate was used. |
| b_thumbprintconfigured | Column<bool> | Indicates if a certificate thumbprint is configured. |
| b_thumbprintfound | Column<bool> | Indicates if the configured thumbprint was found. |
| b_certificatefound | Column<bool> | Indicates if a certificate was found. |
| b_secretsfilevalid | Column<bool> | Indicates if the secrets file is valid. |
| b_recreatedatabase | Column<bool> | Indicates whether the database should be recreated. |
| dfg_brwStandard | ViewDefinition | Default browse view definition. |

## See Also
- [ConfigurationDB](../ConfigurationDB/index.md)
