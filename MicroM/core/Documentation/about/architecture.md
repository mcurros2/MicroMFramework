# Framework Architecture

The MicroM Core Framework is designed with a layered architecture to promote separation of concerns, making applications easier to develop, maintain, and scale. Each layer has a distinct responsibility, from data definition and access to business logic and API exposure.

Below is an overview of the key layers and components, which are reflected in the source code's directory structure.

### 1. `Core`
This is the heart of the framework, containing the most fundamental classes and utilities. It provides the generic base classes, like `EntityDefinition`, that enable the Data Dictionary pattern.
*   **For more details, see the [MicroM.Core Namespace documentation](../namespaces/Core/index.md).**

### 2. `DataDictionary`
This layer contains the C# definitions for the framework's own **internal system entities**, such as `MicromUsers` for authentication and `FileStore` for managing uploads.
*   **Key Concept**: This directory provides out-of-the-box functionality and serves as a concrete example of the Data Dictionary pattern. It is **not** where you define your application's own data model.
*   **For more details, see the [MicroM.DataDictionary Namespace documentation](../namespaces/DataDictionary/index.md).**

### 3. `Data`
The Data Abstraction Layer (DAL) is responsible for all communication with the database. It translates requests from the upper layers into SQL commands and returns data in a structured format.
*   **For more details, see the [MicroM.Data Namespace documentation](../namespaces/Data/index.md).**

### 4. `Database`
This component uses the entity definitions (both internal and application-specific) to manage the physical database schema.
*   **Schema Generation**: Contains logic (`DatabaseManagement`) to create and update the SQL Server database, including tables, views, and stored procedures, based on C# entity definitions.
*   **Custom Scripts**: Allows for the inclusion of custom SQL scripts for more complex database logic.

### 5. `Generators`
A key feature of the framework, this layer contains code generators that reduce boilerplate and development time.
*   **`SQLGenerator`**: Automatically creates SQL stored procedures for CRUD operations for all defined entities.
*   **`ReactGenerator`**: Scaffolds frontend components based on entity definitions.

### 6. `Web`
This is the top layer, responsible for exposing the framework's functionality as a web service. It's built on ASP.NET Core and includes controllers, services, and a robust authentication module.
