# MicroM Web Backend Overview

This document summarizes the main components that compose the MicroM Web backend.

## Controllers

### AuthenticationController
- **Routes:** `/auth-api-status`, `/{app_id}/auth/login`, `/{app_id}/auth/logoff`, `/{app_id}/auth/recoverpassword`, `/{app_id}/auth/recoveryemail`, `/{app_id}/auth/refresh`.
- **Responsibilities:** health check, login, logoff, password recovery and token refresh.
- **Key methods:** `Login`, `Logoff`, `RecoverPassword`, `RecoveryEmail`, `RefreshToken`.

### EntitiesController
- **Routes:** `/{app_id}/ent/{entityName}/action/{actionName}`, `/{app_id}/ent/{entityName}/get`, `/{app_id}/ent/{entityName}/insert`, `/{app_id}/ent/{entityName}/update`, `/{app_id}/ent/{entityName}/delete`, plus endpoints for lookups, views, imports and procedures.
- **Responsibilities:** executes actions, CRUD operations and special procedures for entities.
- **Key methods:** `Action`, `Get`, `Insert`, `Update`, `Delete`, `Lookup`, `View`, `Proc`, `Process`.

### FileController
- **Routes:** `/{app_id}/serve/{fileguid}`, `/{app_id}/thumbnail/{fileguid}/{maxSize?}/{quality?}`, `/{app_id}/tmpupload`.
- **Responsibilities:** serves stored files or thumbnails and handles temporary uploads.
- **Key methods:** `Serve`, `ServeThumbnail`, `Upload`.

### IdentityProviderController
- **Routes:** OIDC and OAuth2 endpoints such as `/{app_id}/oidc/.well-known/openid-configuration`, `/{app_id}/oidc/jwks`, `/{app_id}/oauth2/authorize`, `/{app_id}/oauth2/token`, and related endpoints.
- **Responsibilities:** placeholder for future identity provider support.

### PublicController
- **Routes:** `/{app_id}/public/{entityName}/...` exposing entity actions, CRUD and lookups to anonymous users.
- **Responsibilities:** invokes entity logic without authentication using a public username.
- **Key methods:** `PublicAction`, `PublicGet`, `PublicInsert`, `PublicUpdate`, `PublicDelete`, `PublicLookup`, `PublicView`, `PublicProc`, `PublicProcess`.

### Route Convention
- **Component:** `MicroMRouteConvention` applies a base path prefix to all MicroM controllers.

## Authentication

### WebAPIJsonWebTokenHandler
- Generates encrypted JWT tokens and validates them using per‑application settings.

### MicroMAuthenticationProvider
- Selects the authenticator implementation (built‑in or SQL Server based) and exposes decrypted server claims.

### MicroMAuthenticator & SQLServerAuthenticator
- Handle user login, refresh tokens (via cookies), and lockout logic for their respective authentication modes.

### MicroMCookieManager
- Rewrites cookie paths so that authentication cookies are scoped per application.

## Services

### AuthenticationService
- Orchestrates login, logoff, password recovery and token refresh flows.

### EntitiesService
- Creates database connections, instantiates entity objects and executes entity operations.

### FileUploadService
- Queues uploads, stores files on disk and generates image thumbnails.

### EmailService
- Queues outgoing emails and processes the delivery queue using SMTP.

### SecurityService
- Caches group/route mappings and checks whether a user is authorized for a given path.

### EncryptionService
- Wraps certificate‑based encryption and decryption utilities.

### IdentityProviderService
- Interface reserved for future OIDC/OAuth2 logic such as well‑known metadata, JWKS and token exchange.

## Debugging & Middleware

- **DebugRoutesMiddleware:** returns the list of registered routes when a debugging URL is requested.
- **DependencyInjectionDebug:** logs events from the .NET dependency injection system for troubleshooting.

