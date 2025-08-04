# Guide: Defining Entities

Defining entities is the first and most crucial step in building an application with MicroM. An **Entity Definition** is a C# class that describes a database table, and it's the source from which your database schema, API endpoints, and even UI components are generated.

This guide provides a detailed look at how to create robust entity definitions **in your own application project**. These definitions should not be placed inside the `MicroM.Core` framework project itself.

## Basic Structure

Every entity definition must inherit from the `MicroM.Core.EntityDefinition` base class, and have a corresponding `Entity<T>` class for interaction. These classes should be part of your application's data model project (e.g., a separate `Entities` class library).

```csharp
using MicroM.Core;
using MicroM.Data;

// 1. The Definition Class (The "What")
// This class defines the metadata of your table.
public class MyEntityDef : EntityDefinition
{
    // The constructor sets the table's prefix and name.
    // The prefix should be short and unique (e.g., 3 characters).
    public MyEntityDef() : base("mye", nameof(MyEntity)) { }

    // ... column definitions go here ...
}

// 2. The Interactive Class (The "How")
// This class is used to perform actions like Get, Update, Delete.
public class MyEntity : Entity<MyEntityDef>
{
    // These constructors are required to connect the entity to the data client.
    public MyEntity() : base() { }
    public MyEntity(IEntityClient ec, IMicroMEncryption? encryptor = null) : base(ec, encryptor) { }
}
```

## Defining Columns

Columns are defined as public `readonly` properties of type `MicroM.Data.Column<T>`. The framework provides a rich set of extension methods to specify the column's data type and constraints.

### Common Column Types

| C# Type          | Extension Method      | SQL Server Type |
| ---------------- | --------------------- | --------------- |
| `string`         | `.Text(size: ...)`    | `NVARCHAR(size)`|
| `int`            | `.Integer()`          | `INT`           |
| `long`           | `.BigInt()`           | `BIGINT`        |
| `bool`           | `.Boolean()`          | `BIT`           |
| `decimal`        | `.Decimal(p, s)`      | `DECIMAL(p,s)`  |
| `DateTime`       | `.DateTime()`         | `DATETIME2`     |
| `DateOnly`       | `.Date()`             | `DATE`          |

### Primary Keys (PK)

Use the `.PK()` extension to mark a column as the primary key.

```csharp
// An auto-incrementing integer primary key
public readonly Column<int> c_id = Column<int>.PK(autonum: true);

// A string-based primary key (e.g., user-defined code)
public readonly Column<string> c_product_code = Column<string>.PK(size: 10);
```

### Nullable Columns

To define a column that can accept `NULL` values, use a nullable C# type (e.g., `string?`, `int?`) and pass `nullable: true` to the constructor or extension method.

```csharp
public readonly Column<string?> vc_description = Column<string?>.Text(nullable: true);
```

### Foreign Keys (FK)

Defining a foreign key relationship is a two-step process:

1.  **Define the FK Column:** Create a column in the child entity that will store the key from the parent entity.
2.  **Define the `EntityForeignKey`:** Add a property of type `EntityForeignKey<TParent, TChild>` to establish the formal relationship. This is used by the framework to generate SQL constraints.

**Example:** Assume we have a `Departments` entity. We want to create an `Employees` entity that belongs to a department.

```csharp
// In a theoretical DepartmentsDef.cs
public readonly Column<int> c_department_id = Column<int>.PK(autonum: true);

// In your EmployeesDef.cs
public class EmployeesDef : EntityDefinition
{
    /* ... other columns ... */

    // 1. Define the column to hold the foreign key value.
    public readonly Column<int> c_department_id = new();

    // 2. Define the foreign key relationship itself.
    public readonly EntityForeignKey<Departments, Employees> FK_Department = new(
        key_mappings: [new(
            parentColName: nameof(DepartmentsDef.c_department_id),
            childColName: nameof(c_department_id)
        )]
    );
}
```
This explicit definition allows MicroM to understand the relationships between your data, which is essential for generating correct queries and maintaining data integrity.
