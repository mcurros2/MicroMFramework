# Class: MicroM.Database.DatabaseSchemaTables

## Overview
Creates missing database tables for entities and related lookup objects.

## Methods
| Method | Description |
|:--|:--|
| GetEntitiesInexistingTables | Returns names of tables that do not exist for given entities. |
| CreateEntitiesInexistentTables | Creates tables that are missing in the database. |

## Remarks
Works alongside `DatabaseSchemaProcedures` to fully materialize entity schemas.

