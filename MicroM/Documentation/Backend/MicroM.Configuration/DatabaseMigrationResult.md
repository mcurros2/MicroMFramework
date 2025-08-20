# Enum: MicroM.Configuration.DatabaseMigrationResult

## Overview
Result values for database migration operations.

## Members
| Member | Description |
|:------------|:-------------|
| NoMigrationNeeded | No migration was required. |
| Migrated | The schema was migrated successfully. |
| NotMigrated | The schema was not migrated. |

## Remarks
Used by `IDatabaseSchema.MigrateDatabase` to indicate migration outcomes.

## See Also
- [IDatabaseSchema](IDatabaseSchema.md)
