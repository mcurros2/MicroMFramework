# Static Class: MicroM.Generators.SQLGenerator.TableExtensions

## Overview
Generates DDL and permission scripts for entity tables and foreign keys.

## Methods
| Method | Description |
|:--|:--|
| IsTableCreated | Checks if the table for an entity exists. |
| AsCreateTable | Builds SQL script to create the entity's table and indexes. |
| AsGrantExecutionToEntityProcsScript | Produces SQL grants for generated procedures. |

## Remarks
Uses entity metadata to emit complete SQL for schema creation and permission management.
