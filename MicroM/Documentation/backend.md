# Backend Architecture

The MicroM backend is built on .NET 8 and is organized into modular packages.

## Core Modules

- **Configuration** – manages application and database configuration, including security settings.
- **Data & DataDictionary** – describes entities, columns, and relationships used to generate SQL and API endpoints.
- **Database** – contains helpers for SQL Server scripts, stored procedures, and schema management.
- **Web** – services and conventions that power the Web API, authentication, file handling, and other HTTP features.
- **Generators** – utilities that produce SQL scripts and frontend components.
- **ImportData and Excel** – helpers for reading and importing data from external sources.
- **Validators and Extensions** – common validation logic and extension methods shared across the framework.

## Workflow

1. Define entities and relationships in the DataDictionary.
2. Use Generators to create database scripts and API endpoints.
3. Host the generated API through the Web layer with built-in authentication and file services.
4. Optionally leverage ImportData and Excel helpers for batch operations.

The modular design allows projects to include only the pieces they need while keeping the overall framework lightweight.
