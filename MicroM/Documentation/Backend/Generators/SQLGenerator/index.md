# SQL Generator

The SQL generator creates scripts for tables, views, and CRUD stored procedures based on entity definitions.

## Workflow

1. Build a `TemplateValues` instance with table names, columns, and parameters.
2. Apply tokens to the templates defined in `Templates.cs` (e.g. `VIEW_TEMPLATE`, `DROP_TEMPLATE`).
3. Use helpers in the `Extensions` namespace to post‑process the result.

## Key Extension Methods

- `TableExtensions.AsCreateTable` – produces `CREATE TABLE` and index scripts.
- `TableExtensions.IsTableCreated` – checks whether an entity's table already exists.
- `ColumnExtensions.AsSQLTypeString` – converts column metadata to SQL type declarations.
- `ColumnExtensions.ToSQLName` – converts PascalCase names to snake_case SQL identifiers.

These methods, together with the templates, automate generation of consistent SQL Server artifacts.

