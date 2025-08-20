# Namespace: MicroM.Database

## Overview
Contains helpers for managing SQL Server databases, schema generation, custom scripts, and permissions for MicroM applications.

## Classes
| Class | Description |
|:--|:--|
| [ApplicationDatabase](ApplicationDatabase.md) | High-level operations for provisioning and updating application databases. |
| [ConfigurationDatabaseSchema](ConfigurationDatabaseSchema.md) | Builds the configuration database schema and procedures. |
| [CustomScript](CustomScript.md) | Represents a custom SQL script and its classification. |
| [DataDictionarySchema](DataDictionarySchema.md) | Registers entities in the data dictionary and creates supporting objects. |
| [DatabaseManagement](DatabaseManagement.md) | Utility methods for managing SQL Server databases and logins. |
| [DatabaseSchema](DatabaseSchema.md) | Creates tables and procedures for entity schemas. |
| [DatabaseSchemaCreationOptions](DatabaseSchemaCreationOptions.md) | Options for generating or altering schema for entity instances. |
| [DatabaseSchemaCustomScripts](DatabaseSchemaCustomScripts.md) | Extracts and classifies custom SQL scripts. |
| [DatabaseSchemaExtensions](DatabaseSchemaExtensions.md) | Extension methods for schema generation helpers. |
| [DatabaseSchemaPermissions](DatabaseSchemaPermissions.md) | Generates route entries and grants execution permissions. |
| [DatabaseSchemaProcedures](DatabaseSchemaProcedures.md) | Creates standard and custom stored procedures for entities. |
| [DatabaseSchemaTables](DatabaseSchemaTables.md) | Creates missing tables and related schema objects. |

## Enums
| Enum | Description |
|:--|:--|
| [SQLProcStandardType](SQLProcStandardType.md) | Standard categories for generated stored procedures. |
| [SQLScriptType](SQLScriptType.md) | SQL object types recognized by custom scripts. |

## Remarks
These components coordinate schema creation and maintenance tasks for MicroM applications.

## See Also
- [Backend Namespaces](../index.md)

