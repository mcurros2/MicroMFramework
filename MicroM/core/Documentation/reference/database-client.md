# Reference: Data Access Layer

The Data Access Layer (DAL) is responsible for all communication with the SQL Server database. The key components are located in the `MicroM.Data` namespace. It provides a high-level, entity-based way to interact with data, abstracting away the underlying ADO.NET complexity.

## `DatabaseClient`

This is the concrete class that manages the connection to the SQL Server database.

*   **Purpose**: To open/close connections, execute raw SQL queries, and manage transactions.
*   **Usage**: You typically instantiate a `DatabaseClient` with the connection details for your database. For entity operations, you would then pass this client to your `Entity<T>` classes.

```csharp
using DatabaseClient ec = new(server: "...", user: "...", ...);
await ec.Connect(ct);
// ... perform operations ...
await ec.Disconnect();
```

## `IEntityClient`

This interface is implemented by `DatabaseClient` and represents the contract for a client that can perform entity-based operations.

*   **Purpose**: To provide a stable interface for entity classes to use, allowing for dependency injection and easier testing.
*   **Key Concept**: When you instantiate an entity class to perform an operation, you pass an `IEntityClient` to its constructor. The entity then uses this client to communicate with the database.

```csharp
var persons = new Persons(ec); // 'ec' is an IEntityClient
persons.Def.c_person_id.Value = 1;
await persons.GetData(ct);

Console.WriteLine(persons.Def.vc_person_name.Value);
```

## `Entity<T>` Data Methods

The `Entity<T>` base class provides the core methods for performing CRUD operations. When you call these methods on your entity class (e.g., `Persons`), they use the `IEntityClient` to execute the corresponding stored procedures that were generated from your `EntityDefinition`.

### Key Methods:

*   **`GetDataAsync()`**: Fetches a single record from the database based on the current values of the primary key columns.
*   **`UpdateAsync()`**: Inserts or updates a record. The framework automatically determines whether to call the `iupdate` (insert/update) or `update` stored procedure.
*   **`DeleteAsync()`**: Deletes a record based on the current primary key values.
*   **`BrowseAsync()`**: Executes a view's stored procedure to get a list of records. This is used for populating tables and lists in the UI.

These methods return a `DataResult` object, which contains information about the success of the operation, any messages returned from the database, and the data itself.
