# Class: MicroM.Generators.SQLGenerator.TableExtensions
## Overview
Extension methods for generating database tables and related scripts.

**Inheritance**
object -> TableExtensions

**Implements**
None

## Example Usage
```csharp
List<string> scripts = entity.AsCreateTable();
```
## Methods
| Method | Description |
|:------------|:-------------|
| IsTableCreated<T>(this T, DatabaseClient, CancellationToken) where T : EntityBase | Checks if an entity table exists. |
| AsCreateTable<T>(this T, bool, bool) where T : EntityBase | Produces SQL to create a table and indexes. |
| AsDropIndexes<T>(this T) where T : EntityBase | Generates SQL to drop existing indexes. |
| AsCreateIndexes<T>(this T) where T : EntityBase | Generates SQL to create indexes. |
| AsDropUniqueConstraints<T>(this T) where T : EntityBase | Generates SQL to drop unique constraints. |
| AsAlterUniqueConstraints<T>(this T) where T : EntityBase | Generates SQL to recreate unique constraints. |
| AsDropPrimaryKey<T>(this T) where T : EntityBase | Generates SQL to drop the primary key. |
| AsAlterPrimaryKey<T>(this T) where T : EntityBase | Generates SQL to recreate the primary key. |
| AsDropForeignKeys<T>(this T) where T : EntityBase | Generates SQL to drop foreign keys. |
| AsCreateForeignKeysIndexes<T>(this T) where T : EntityBase | Generates SQL to create foreign key indexes. |
| AsAlterForeignKeys<T>(this T, bool) where T : EntityBase | Generates SQL to recreate foreign keys. |
| AsGrantExecutionToEntityProcsScript<T>(this T, string) where T : EntityBase | Builds grant scripts for entity stored procedures. |

## Remarks
None.

