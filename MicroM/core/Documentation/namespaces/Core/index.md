# Namespace: MicroM.Core

The `MicroM.Core` namespace contains the most fundamental and foundational classes of the framework. These components provide the basic building blocks upon which all other layers and functionalities are built. They are abstract and not tied to any specific application logic.

## Key Classes

### `EntityDefinition`
This is an abstract base class that serves as the foundation for the **Data Dictionary pattern**.

*   **Purpose**: To hold the metadata for a data entity, such as a database table. It is designed to contain properties that define columns, keys, and relationships.
*   **Key Concept**: This class is **generic**. It is not tied to any specific location or type of entity. Both the framework's internal entities (in `MicroM.DataDictionary`) and an application's own custom entities (in the application's own project) inherit from this class. It is the common ancestor that allows the rest of the framework (like the database generators and API controllers) to work with any entity in a standardized way.

### `Entity<T>`
This is the base class for all entity objects that are used to interact with data. It is a generic class, where `T` must be a class that inherits from `EntityDefinition`.

*   **Purpose**: To provide the methods for performing data operations like `GetDataAsync`, `UpdateAsync`, and `DeleteAsync`.
*   **Usage**: When you define a custom entity (e.g., `Persons`), you create two classes: `PersonsDef : EntityDefinition` (the metadata) and `Persons : Entity<PersonsDef>` (the interactive object).

### `EntityActionBase`, `EntityActionResult`
These classes form the basis of the framework's action and result pattern, which is used for handling operations in a structured way.

### `CryptClass`, `CRC32`, `X509Encryptor`
A set of utility classes that provide core services for encryption, hashing, and data integrity checks used throughout the framework.

### `IInit`, `InitBase`
These provide a standardized way (`IInit`) for components to declare that they require initialization, and a base implementation (`InitBase`) to simplify the process. This is used to ensure components are properly configured before use.
