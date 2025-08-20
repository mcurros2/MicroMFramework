# Interface: MicroM.Configuration.IDatabaseSchema

## Overview
Provides methods for creating and migrating the MicroM database schema.

## Methods
| Method | Description |
|:------------|:-------------|
| GetEntitiesTypes(IEntityClient ec, CancellationToken ct) | Get the entity types to build the schema for. |
| CreateDBSchemaAndProcs(IEntityClient ec, CustomOrderedDictionary<DatabaseSchemaCreationOptions<EntityBase>> entities, CancellationToken ct, bool create_or_alter = true, bool create_if_not_exists = true) | Create or alter schema and stored procedures. |
| GrantPermissions(IEntityClient ec, CustomOrderedDictionary<DatabaseSchemaCreationOptions<EntityBase>> entities, string login_or_group, CancellationToken ct) | Grant permissions for the generated schema. |
| CreateMenus(IEntityClient ec, CustomOrderedDictionary<DatabaseSchemaCreationOptions<EntityBase>> entities, CancellationToken ct) | Create menu entries for the generated entities. |
| MigrateDatabase(IEntityClient ec, CancellationToken ct) | Migrate the database to the latest schema version. |

## Remarks
Implementations manage schema creation, permissions, and migrations.

## See Also
- [DatabaseMigrationResult](DatabaseMigrationResult.md)
