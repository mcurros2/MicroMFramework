# Core Concepts

The MicroM Framework is built on a few core concepts that enable its data-driven and convention-over-configuration approach. Understanding these ideas is key to using the framework effectively.

## 1. The Data Dictionary Pattern

The **Data Dictionary** is the most important concept in MicroM. Instead of defining your database schema in SQL and then creating a C# model to match it, you do the reverse. You define your entire data model—tables, columns, types, relationships, and constraints—directly in C# code.

This C# representation of your schema serves as the single source of truth for your application's data structure. The framework uses this pattern for both its internal system tables and for the application-specific tables you will create.

## 2. `EntityDefinition` and `Entity`

The two most fundamental classes for implementing the Data Dictionary pattern are `EntityDefinition` and `Entity`.

*   **`EntityDefinition`**: This is a base class used to define the **metadata** of an entity (its columns, keys, and relationships).
*   **`Entity<T>`**: This is a base class for objects that you **interact with** to perform data operations like `Get`, `Update`, and `Delete`.

Both of these classes are generic and are defined in the `MicroM.Core` namespace. For a more detailed explanation, see the [MicroM.Core Namespace documentation](../namespaces/Core/index.md).

### Example of the Pattern

When building an application, you will create your entity models in your own project (e.g., a separate `Entities` class library). For each entity, you will create two classes:

**1. The Definition Class (the "What"):**
```csharp
// In YourProject/Entities/Persons.cs
public class PersonsDef : EntityDefinition
{
    public PersonsDef() : base("pers", nameof(Persons)) { }

    public readonly Column<string> c_person_id = Column<string>.PK(autonum: true);
    public readonly Column<string> vc_person_name = Column<string>.Text();
    // ... other column definitions
}
```

**2. The Interactive Class (the "How"):**
```csharp
// Also in YourProject/Entities/Persons.cs
public class Persons : Entity<PersonsDef>
{
    public Persons() : base() { }
    public Persons(IEntityClient ec, IMicroMEncryption? encryptor = null) : base(ec, encryptor) { }
}
```
The framework discovers these classes at runtime and uses their metadata to generate the database and API endpoints.

## 3. Convention over Configuration

The framework heavily relies on conventions to reduce the amount of configuration and boilerplate code you need to write.

*   **API Routing**: The `Web` layer automatically creates API endpoints for your entities based on their definitions. A `Persons` entity will get endpoints like `api/persons/get`, `api/persons/update`, etc., without you needing to write a controller.
*   **Database Objects**: The `Database` layer knows how to generate standard SQL tables and stored procedures for all discovered entities.
*   **UI Generation**: The `Generators` can create React components that are pre-configured to interact with the conventional API endpoints.
