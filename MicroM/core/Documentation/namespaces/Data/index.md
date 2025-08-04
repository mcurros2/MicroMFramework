# Namespace: MicroM.Data

The `MicroM.Data` namespace provides the data access layer (DAL) for the framework. It contains the components responsible for connecting to the database, executing commands, and translating data into strongly-typed C# objects. It abstracts away the low-level details of ADO.NET.

## Key Classes and Components

### `DatabaseClient`
This is the primary class for managing a connection to a SQL Server database. It implements the `IEntityClient` interface.

*   **Purpose**: To handle the full lifecycle of database interaction: opening a connection, executing commands (like stored procedures), managing transactions, and closing the connection.
*   **Usage**: You typically create an instance of `DatabaseClient` and pass it to your entity objects, which then use it to perform their data operations.

### `IEntityClient`
This interface defines the contract for a client that can be used by entity objects (`Entity<T>`).

*   **Purpose**: It decouples the entity logic from the concrete `DatabaseClient` implementation, which is useful for testing and allows for potential future extension with other data sources.

### Column-related classes
This namespace defines the classes that represent columns in the `EntityDefinition`.
*   **`Column<T>`**: A generic class that holds the value and metadata for a column.
*   **`ColumnBase`**, **`BaseColumnMapping`**: Base classes providing common functionality for columns.
*   **`SQLTypeConverter`**: A utility class that maps C# types to their corresponding SQL Server types (e.g., `string` to `NVARCHAR`).

### `DataResult`, `DataResultSet`
These are standardized wrapper objects for returning data from the DAL.

*   **`DataResult`**: Used for operations that return a single result set (e.g., getting a single record or a list of records). It contains the data as well as status information, such as rows affected and any error messages.
*   **`DataResultSet`**: Used when an operation returns multiple result sets from a single database call.

### `EntityFilter`
A class used to define filter criteria for database queries, allowing you to build dynamic `WHERE` clauses in a structured way.
