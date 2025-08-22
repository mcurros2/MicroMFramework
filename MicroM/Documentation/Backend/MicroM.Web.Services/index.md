# Namespace: MicroM.Web.Services
## Overview
Web-related service implementations.

## Classes
| Class | Description |
|:------------|:-------------|
| [WebAPIBaseExtensions](WebAPIBaseExtensions/index.md) | Provides extension methods to register MicroM Web API services. |
| [WebAPIServices](WebAPIServices/index.md) | Central service locator exposing common MicroM web services. |
| [FileDetails](FileDetails/index.md) | Metadata about an uploaded file. |
| [UploadFileResult](UploadFileResult/index.md) | Outcome of a file upload with identifiers and errors. |
| [FileUploadService](FileUploadService/index.md) | Uploads files to storage and updates related metadata. |
| [ServeFileResult](ServeFileResult/index.md) | File stream and content type returned when serving a file. |
| [MicroMEncryption](MicroMEncryption/index.md) | Encrypts and decrypts data using X509 certificates. |
| [AuthenticationService](AuthenticationService/index.md) | Coordinates login, token refresh, and recovery operations. |
| [ImageThumbnailService](ImageThumbnailService/index.md) | Generates resized image thumbnails. |
| [EmailServiceDestination](EmailServiceDestination/index.md) | Email recipient with address, name, and tags. |
| [EmailServiceItem](EmailServiceItem/index.md) | Email message details to queue for sending. |
| [EmailServiceTags](EmailServiceTags/index.md) | Key-value pair used to personalize email templates. |
| [EmailServiceConfigurationData](EmailServiceConfigurationData/index.md) | SMTP configuration settings for email sending. |
| [EmailService](EmailService/index.md) | Queues and sends emails using configured SMTP settings. |
| [EmailHostedService](EmailHostedService/index.md) | Hosted service that processes the email queue. |
| [MemoryQueueHostedService](MemoryQueueHostedService/index.md) | In-memory background task queue with hosted processing. |
| [TaskStatusInfo](TaskStatusInfo/index.md) | Status and timestamps for a queued task. |
| [QueueStatusInfo](QueueStatusInfo/index.md) | Counts of queued and running background tasks. |
| [QueueItem](QueueItem/index.md) | Represents a task queued for background processing. |
| [BackgroundTaskQueue](BackgroundTaskQueue/index.md) | Executes queued background tasks with concurrency control. |
| [MicroMAppConfigurationProvider](MicroMAppConfigurationProvider/index.md) | Loads and caches application configurations and types. |
| [SSOClientConfiguration](SSOClientConfiguration/index.md) | Client settings for SSO integration. |
| [SSOTokenResult](SSOTokenResult/index.md) | Tokens and expiration details returned from SSO. |
| [SSOAuthenticatorResult](SSOAuthenticatorResult/index.md) | Result of SSO authentication including claims. |
| [EntitiesService](EntitiesService/index.md) | Provides database access and operations for entities. |
| [MicroMCookieManager](MicroMCookieManager/index.md) | Applies tenant-specific paths to authentication cookies. |
| [MicroMCookiesManagerSetup](MicroMCookiesManagerSetup/index.md) | Configures authentication to use the custom cookie manager. |

## Interfaces
| Interface | Description |
|:------------|:-------------|
| [IWebAPIServices](IWebAPIServices/index.md) | Interface for Web API services. |
| [IFileUploadService](IFileUploadService/index.md) | Interface for file upload service. |
| [IMicroMEncryption](IMicroMEncryption/index.md) | Interface for MicroM encryption. |
| [IAuthenticationService](IAuthenticationService/index.md) | Interface for authentication service. |
| [IThumbnailService](IThumbnailService/index.md) | Interface for thumbnail service. |
| [IEmailService](IEmailService/index.md) | Interface for email service. |
| [IMemoryQueueHostedService](IMemoryQueueHostedService/index.md) | Interface for memory queue hosted service. |
| [IBackgroundTaskQueue](IBackgroundTaskQueue/index.md) | Interface for background task queue. |
| [IMicroMAppConfiguration](IMicroMAppConfiguration/index.md) | Interface for MicroM app configuration. |
| [IIdentityProviderService](IIdentityProviderService/index.md) | Interface for identity provider service. |
| [IEntitiesService](IEntitiesService/index.md) | Interface for entities service. |

## Enums
| Enum | Description |
|:------------|:-------------|
| [QueueTaskStatus](QueueTaskStatus/index.md) | Queue task status values. |

## Remarks
None.

## See Also
-
