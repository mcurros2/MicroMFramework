# Namespace: MicroM.Web

The `MicroM.Web` namespace contains all the components necessary to expose the framework's functionality as a web service over HTTP. It is built on top of ASP.NET Core and provides controllers, services, and a complete authentication system.

## Key Sub-Namespaces and Components

### `MicroM.Web.Authentication`
This is the heart of the framework's security system.

*   **Purpose**: To provide robust, out-of-the-box authentication and authorization.
*   **Key Classes**:
    *   `IAuthenticator`: An interface defining the contract for an authentication provider.
    *   `MicroMAuthenticator`: The default implementation of `IAuthenticator`, which handles credential verification, token generation, and password recovery logic against the `MicromUsers` entity.
    *   **JWT & Cookie Managers**: Classes responsible for creating, validating, and managing JWT access tokens and secure refresh token cookies.

### `MicroM.Web.Controllers`
This namespace provides the API endpoints.

*   **Purpose**: To expose the data entities via a RESTful API.
*   **Key Features**:
    *   **Convention-based Routing**: The framework automatically generates routes for your entities, so you don't have to create controller classes for basic CRUD operations. For an entity named `Persons`, it will create endpoints like `/api/Persons/get`, `/api/Persons/update`, etc.
    *   `AuthenticationController`: A built-in controller that exposes endpoints for login, logout, token refresh, and password recovery.
    *   `FileController`: A built-in controller for handling file uploads and downloads.

### `MicroM.Web.Services`
This namespace contains high-level services that encapsulate business logic.

*   **Purpose**: To provide reusable logic that can be injected into controllers or other services.
*   **Key Services**:
    *   `IAuthenticationService`: A facade over the `IAuthenticator` that simplifies authentication logic for the controller layer.
    *   `IEmailService`: A service for queuing and sending emails, often used for password recovery or notifications.
    *   `IFileUploadService`: A service that handles the logic for processing and storing uploaded files.
    *   `ISecurityService`: A service that checks user permissions against the defined `MenuDefinition` and `UsersGroupDefinition` rules.
