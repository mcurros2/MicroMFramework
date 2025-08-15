# Data Layer Overview

This section covers the core pieces used by MicroM to access SQL Server.

## Main Classes

### `DatabaseClient`
Handles SQL Server connections, transactions, and stored procedure execution. It maps results to objects and exposes connection settings like server, database, and credentials.

### `EntityData`
Provides CRUD helpers for an `EntityDefinition`. It prepares parameters from column metadata, executes entity stored procedures, and maps results back into columns.

### `Column`
Represents a parameter or field with associated `SQLServerMetadata`. Derived from `ColumnBase`, it validates values, supports overrides, and holds flags such as insert/update/delete.

### `ProcedureDefinition`
Describes a stored procedure and its parameters. It can be initialized from column templates and supports lookup/import flags as well as read-only execution.

### `EntityFilter`
A lightweight container identifying a named filter entity. It ties filter definitions to a specific `EntityBase` type for query composition.

### `SQLServerMetadata`
Encapsulates SQL type details: `SqlDbType`, size, precision, scale, output/nullable flags, encryption, and array support.

### `SQLTypeConverter`
Static mappings between CLR types and `SqlDbType` values. It checks type compatibility, infers prefixes, and converts in both directions.

## Result Types

### `DataResult`
Container for tabular results with headers, type information, and record collections. Used for generic procedure or query responses.

### `DBStatusResult`
Aggregates `DBStatus` entries returned from update/insert/delete operations and tracks failure or autonumber information.

### `LookupResult`
Simple record containing a description for lookups performed against related entities.

## Filter and Constraint Helpers

### `EntityFilter`
Defines reusable named filters to scope data retrieval based on another entity.

### `EntityUniqueConstraint`
Declares a set of columns that must be unique for an entity, enabling validation and index generation.

