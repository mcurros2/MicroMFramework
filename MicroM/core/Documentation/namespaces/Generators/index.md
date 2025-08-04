# Namespace: MicroM.Generators

The `MicroM.Generators` namespace contains the framework's code generation components. These tools are designed to automate the creation of repetitive boilerplate code, reading from the C# entity definitions as a single source of truth.

## Key Generators

### `SQLGenerator`
This component is responsible for automatically generating SQL Server code based on your `EntityDefinition` classes.

*   **Purpose**: To create a complete set of database objects required for full CRUD (Create, Read, Update, Delete) functionality without requiring you to write any SQL manually.
*   **What it Generates**:
    *   `CREATE TABLE` scripts.
    *   Primary Key, Foreign Key, and Unique constraints.
    *   Stored Procedures for each CRUD operation (e.g., `usp_MyEntity_get`, `usp_MyEntity_iupdate`).
    *   Stored Procedures for any defined Views (e.g., `usp_MyEntity_brwStandard`).
*   **Usage**: This generator is typically invoked automatically by the `DatabaseManagement` class when you initialize or update your application's database.

### `ReactGenerator`
This component scaffolds frontend code for a React and TypeScript application.

*   **Purpose**: To accelerate UI development by generating components and API client code that are already wired up to the backend.
*   **What it Generates**:
    *   TypeScript `interface` definitions that mirror your C# entity properties.
    *   API client functions for calling the backend endpoints.
    *   React components (using the Mantine component library) for forms and data tables.
*   **Usage**: This generator is intended to be run as a command-line tool pointed at your compiled project assembly. It reads the entity metadata and outputs the corresponding `.ts` and `.tsx` files into your frontend source directory.
