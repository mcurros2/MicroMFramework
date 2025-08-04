# Guide: Using the Code Generators

A core philosophy of the MicroM Framework is to automate repetitive tasks. The code generators are a powerful manifestation of this, allowing you to create database objects and frontend components directly from your C# `DataDictionary` definitions. This ensures consistency and dramatically speeds up development.

## SQL Generation

The SQL Generator is the most integrated of the two generators and works largely automatically. It reads your entity definitions and generates the necessary SQL Server objects.

### How It Works

When you initialize or update your application's database schema (e.g., by calling `DatabaseManagement.UpdateSchemaAsync`), the framework performs the following steps:
1.  **Reads the Data Dictionary**: It inspects all your `EntityDefinition` classes.
2.  **Generates SQL**: For each entity, it generates:
    *   `CREATE TABLE` scripts based on your column definitions.
    *   Primary Key and Foreign Key constraints.
    *   Stored Procedures for all standard CRUD operations (Create, Read, Update, Delete), including browsing/listing views.
3.  **Executes the SQL**: It applies these generated scripts to your target database, creating and updating objects as needed.

### Benefits
*   **Consistency**: Your database schema is guaranteed to match your C# data model.
*   **Productivity**: You don't need to write and maintain hundreds of repetitive stored procedures.
*   **Security**: Using stored procedures for data access is a security best practice, and MicroM handles this for you.

You can also include your own custom `.sql` scripts, which the framework will execute during the update process.

## React Frontend Generation

The React Generator helps you bootstrap your user interface. It can generate TypeScript files containing ready-to-use components for interacting with your backend entities.

### What It Generates

For a given entity, the generator can scaffold:
*   **Type Definitions**: TypeScript interfaces that match your C# entity columns.
*   **API Client Code**: Functions for calling the conventional API endpoints (e.g., `getPersons`, `updatePerson`).
*   **UI Components**: Basic React components for forms and data tables, pre-wired with validation rules and API calls. These are built using the Mantine component library.

### How to Use It

The React generator is designed to be used as part of your frontend build pipeline. While the specific invocation may vary depending on your project setup, the general principle is to run a command that points the generator at your compiled `.dll` containing the entity definitions.

The generator reads the metadata from your `DataDictionary` and outputs `.ts` and `.tsx` files into your frontend project's source tree.

**Example (conceptual):**
```bash
# This is a conceptual command. The actual tool may differ.
microm-generate --assembly ./Entities.dll --output ./frontend/src/generated
```

This would generate the necessary frontend code for all entities found in `Entities.dll` and place it in the `frontend/src/generated` directory. You can then import these generated components into your application's pages.

By using the code generators, you can build a full-stack application—from the database to the UI—from a single source of truth: your C# entity definitions.
