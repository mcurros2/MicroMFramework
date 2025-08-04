# Reference: Data Dictionary API

The Data Dictionary is the cornerstone of the MicroM framework. The components in the `MicroM.DataDictionary` namespace are used to define the structure and metadata of your application's data.

## Key Classes and Components

### `EntityDefinition`
The base class for all entity definitions.

*   **Purpose**: To act as a container for column definitions and metadata for a single database table.
*   **Key Members**:
    *   `EntityDefinition(string prefix, string name)`: The constructor sets the table's short prefix and full name.
    *   Properties of type `Column<T>`: Each public readonly property of this type defines a column in the table.
    *   Properties of type `EntityForeignKey<TP,TC>`: Define foreign key relationships.
    *   Properties of type `ViewDefinition`: Define views (for browsing/listing data).

### `Column<T>`
Represents a column in a database table.

*   **Purpose**: To define the data type, name, and constraints of a column.
*   **Usage**: It is almost always used with an extension method to specify the type and properties.
*   **Common Extensions**:
    *   `.PK(autonum: ...)`: Marks the column as a primary key.
    *   `.Text(size: ...)`: Defines a string column (`NVARCHAR`).
    *   `.Integer()`, `.BigInt()`: Define integer columns.
    *   `.Boolean()`: Defines a bit column.
    *   `.DateTime()`, `.Date()`: Define date/time columns.
    *   `.Decimal(precision, scale)`: Defines a fixed-point decimal column.

### `EntityForeignKey<TParent, TChild>`
Defines a foreign key relationship between two entities.

*   **Purpose**: To formally declare a link between a child table and a parent table. This is used to generate SQL `FOREIGN KEY` constraints.
*   **Key Members**:
    *   `key_mappings`: A collection that maps the column(s) in the child entity to the corresponding key column(s) in the parent entity.

### `ViewDefinition`
Defines a database view, which is primarily used for creating efficient queries for lists or browse screens.

*   **Purpose**: To specify a pre-defined set of columns that should be returned by a list query.
*   **Usage**: You can define multiple views for a single entity, such as a "standard view" for general lists and a "detailed view" for admin panels.

### `UsersGroupDefinition` & `MenuDefinition`
These classes are part of the Data Dictionary and are used to define the application's security and authorization rules. See the [Authentication Guide](./guides/authentication.md) for more details.
