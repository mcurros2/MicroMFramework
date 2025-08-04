# Namespace: MicroM.DataDictionary

The `MicroM.DataDictionary` namespace contains the entity definitions for the **internal system tables** that the framework uses to provide its core features. It also contains classes for configuring application security and menus.

**Crucially, this namespace is for internal framework use. You should not add your own application's entity definitions to this namespace.** Instead, you should create your own entities in a separate application project, using the classes here as an example of how to implement the Data Dictionary pattern.

## Key System Entities

This namespace provides a set of pre-built entities for common application requirements. When you use the framework, the database schema for these entities will be created for you.

*   **`MicromUsers`**: The entity for storing user accounts, credentials, and profile information. It is central to the authentication system.
*   **`FileStore`**: Entities related to the file management system, used for tracking uploaded files and their metadata.
*   **`EmailService`**: Entities for the email queuing system, including `EmailServiceQueue` and `EmailServiceTemplates`.
*   **`Applications`**: Entities for managing application configurations, especially in multi-tenant scenarios.
*   **`MicromMenus`**: Entities for storing application menu structures and permissions, linking users and groups to the routes they can access.

## Configuration Classes

In addition to entities, this namespace contains the classes used to define application security and navigation in code.

*   **`UsersGroupDefinition`**: Used to define a security group or role. You create subclasses of this to represent the roles in your application (e.g., `Administrators`, `Viewers`).
*   **`MenuDefinition`**: Used to define a menu structure for your application. Instances of `MenuItemDefinition` within a menu are used to grant access to specific API routes.
*   **`CategoryDefinition` / `StatusDefinition`**: Classes that help you define application-wide categories and status codes in a structured, reusable way.
