# Reference: Web Services API

The `MicroM.Web.Services` namespace contains a collection of high-level services that provide common application functionalities. These services are typically registered for dependency injection when the application starts up and are used within the Web API controllers or other services.

Below is an overview of the most important services.

## `IAuthenticationService`

*   **Purpose**: The primary service for managing user authentication and authorization. It acts as a facade over the lower-level `IAuthenticator` implementations.
*   **Key Responsibilities**:
    *   Handling user login and logoff.
    *   Managing JWT creation and validation.
    *   Handling access token refresh logic.
    *   Coordinating password recovery and reset flows.
*   **Usage**: This service is used by the `AuthenticationController` to expose the authentication endpoints. You would typically interact with its exposed API rather than using the service directly.

## `IEmailService`

*   **Purpose**: Provides a reliable way to send emails. It features a queuing mechanism to ensure emails are sent without blocking the main application thread.
*   **Key Features**:
    *   **Templating**: Can use email templates stored in the database. Templates can include placeholders (`tags`) that are dynamically replaced with data.
    *   **Queuing**: Emails are first submitted to a database queue. A background process then picks them up and sends them, improving application performance and resilience.
    *   **Provider Agnostic**: The default implementation uses MailKit, but the interface allows for other providers to be used.
*   **Usage**:
    ```csharp
    // Example of queuing an email
    await emailService.QueueEmail(appId, new EmailServiceItem { ... }, ct);
    ```

## `IFileUploadService`

*   **Purpose**: Manages the process of uploading files, storing them, and associating them with entities.
*   **Key Features**:
    *   **Chunked Uploads**: Supports uploading large files in smaller chunks.
    *   **Processing**: Can perform post-upload processing, such as generating thumbnails for images.
    *   **Storage**: Integrates with the `FileStore` entity in the Data Dictionary to track file metadata.
*   **Usage**: The `FileController` uses this service to provide file upload API endpoints.

## `ISecurityService`

*   **Purpose**: A lower-level service that deals with claims and permissions.
*   **Key Responsibilities**:
    *   Retrieving the permissions (allowed API routes) for a given user based on their user groups.
    *   Providing methods to check if a user has access to a specific route.
*   **Usage**: This service is used by the framework's authorization middleware to protect API endpoints. It's less common for application developers to interact with this service directly, but it's central to how security is enforced.
