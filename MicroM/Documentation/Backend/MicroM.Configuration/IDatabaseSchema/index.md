# Interface: MicroM.Configuration.IDatabaseSchema
## Overview
Provides methods for creating and migrating the MicroM database schema.

## Methods
| Method | Description |
|:------------|:-------------|
| GetEntitiesTypes | Get the entity types to build the schema for. |
| CreateDBSchemaAndProcs | Create or alter schema and stored procedures. |
| GrantPermissions | Grant permissions for the generated schema. |
| CreateMenus | Create menu entries for the generated entities. |
| MigrateDatabase | Migrate the database to the latest schema version. |

## Remarks
None.

## See Also
- [DatabaseMigrationResult](../DatabaseMigrationResult/index.md)
