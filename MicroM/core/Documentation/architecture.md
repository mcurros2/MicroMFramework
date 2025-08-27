# Architecture Overview
The MicroM framework is designed with a modular architecture to facilitate the development of SQL Database Centric Applications. Below is an overview of its key components and their interactions.

## Key concepts

### Entities
- Defined in C# using [EntityDefinition](../Core/EntityDefinition.cs) and [Entity](../Core/Entity.cs) classes.
- Represent database tables and their relationships.
- Support CRUD operations through standard methods implemented in [EntityBase](../Core/EntityBase.cs) Data property, [EntityData](../Data/EntityData.cs) and [IIEntityData](../Data/IIEntityData.cs).
- Data operations are carried executing stored procedures in the database.
- Are the default storage unit for the framework.

#### Database conventions
- Each entity has a short mnemonic code (typically 4 characters) used as a prefix for stored procedures and other database objects.
- Each entity corresponds to a database table named using the class name transformed from Camel Case to lowercase table name separated by underscores.
- Suffix for common stored procedures:
  - `{menomonic}_update` for Insert and update operations
  - `{menomonic}_drop` for Delete operations
  - `{menomonic}_get` for Get (single record) operations
  - `{menomonic}_lookup` for single record friendly description lookup operations
  - `{menomonic}_brwStandard` for the standard View (multiple records)
  - `{menomonic}_brw{view name}` for Views (multiple records)
- Example for the entity `CustomersOrders`
  - Mnemonic `cust`
  - Table name `customers_orders`.
  - Insert/Update stored procedure `cust_update`
  - Delete stored procedure `cust_drop`
  - Get stored procedure `cust_get`
  - Lookup stored procedure `cust_lookup`
  - Standard View stored procedure `cust_brwStandard`

### The DataDictionary namespace
- Contains Entities for system and support tables.
- Each APP has its own set of system tables.
- Is composed by these entities:
    - SystemProcs
    - Categories
    - CategoriesValues>
    - Status
    - StatusValues
    - MicromRoutes
    - MicromUsersLoginHistory
    - MicromUsersGroups
    - MicromUsers
    - MicromUsersCat
    - MicromUsersDevices
    - MicromUsersGroupsMembers
    - MicromMenus
    - MicromMenusItems
    - MicromMenusItemsAllowedRoutes
    - MicromUsersGroupsMenus
    - FileStoreProcess
    - FileStore
    - FileStoreStatus
    - EmailServiceConfiguration
    - EmailServiceQueue
    - EmailServiceQueueStatus
    - EmailServiceTemplates
    - ImportProcess
    - ImportProcessErrors
    - ImportProcessStatus
	- ApplicationOidcServerSessions

### The MicroM Configuration Database
- Contains the configuration for each application.
- Stores the MicroM API configuration data.
- Each application can be hosted in a different database server.
- Each application can have its own database.
- The configuration database is created during the installation process.
- Is composed by these entities:
	- Applications
	- ApplicationsCat
	- ApplicationsAssemblies
	- ApplicationAssemblyTypes
	- ApplicationsUrls
	- ApplicationOidcClients
	- ApplicationOidcServer
	- EntitiesAssemblies
	- EntitiesAssembliesTypes

### Aplications
- Each application is defined in the MicroM Configuration Database.
- Each application has its own set of entities.
	- You need to configure the path to the assembly containing the entities for the application.
	- You need to configure the path to the MicroMCore assembly for the application.
- You need to configure the desired authenticator for the app.
- Other configuration options are available.

### API Controllers
- The API is multi-tenant by design.
- Uses routing conventions for each controller.
- Root is `/microm/{app_id}/`
- Authentication controller `/microm/{app_id}/auth/`
- Files controller `/microm/{app_id}/serve/`, `/microm/{app_id}/tmpupload/`, `/microm/{app_id}/thumbnail/`
- Entities controllers `/microm/{app_id}/ent/{entity_name}/{insert|update|delete|get|lookup|view|proc|process|import}`
- OIDC controller `/microm/{app_id}/oidc/`, `/microm/{app_id}/oauth2/`

### Authentication
- Token handling
	- [WebAPIJsonWebTokenHandler](../Web/Authentication/JWTHandling/WebAPIJsonWebTokenHandler.cs) Provides Jwt encrypted tokens and claims, valid for each application.
	- [MicroMCookieManager](../Web/Authentication/CookieHandling/MicroMCookieManager.cs) implements ICookieManager for handling cookies per tenant in the WebAPI (determines the cookie path - /microm/{app_id}_}/).
- Interfaces
	- [IAuthenticator](../Web/Authentication/AuthenticationProvider/IAuthentication.cs) for implementing authenticators.
	- [IAuthenticationProvider](../Web/Authentication/AuthenticationProvider/IAuthenticationProvider.cs) for implementign an authetication provider (calls the configured app authenticator).
	- [IAuthenticationService](../Web/Services/AuthenticationService/IAuthenticationService.cs) for the authentication service (implements common authentication functions, login, logoff, etc.).
	- [IAuthenticationController](../Web/Authentication/AuthenticationController/IAuthenticationController.cs) for the authentication controller (exposes authentication endpoints).
- Resulting implementation flow
	- The [AuthenticationController](../Web/Authentication/AuthenticationController/AuthenticationController.cs)
		- calls the configured [IAuthenticationService](../Web/Services/AuthenticationService/IAuthenticationService.cs).
		- The [AuthenticationService](../Web/Services/AuthenticationService/AuthenticationService.cs)
			- calls the configured [IAuthenticationProvider](../Web/Authentication/AuthenticationProvider/IAuthenticationProvider.cs).
			- calls the configured JsonWebTokenHandler ([WebAPIJsonWebTokenHandler](../Web/Authentication/JWTHandling/WebAPIJsonWebTokenHandler.cs)).
			- The [IAuthenticationProvider](../Web/Authentication/AuthenticationProvider/IAuthenticationProvider.cs) calls the configured [IAuthenticator](../Web/Authentication/AuthenticationProvider/IAuthentication.cs).
			- The [IAuthenticator](../Web/Authentication/AuthenticationProvider/IAuthentication.cs) implements the actual authentication logic (check user credentials, etc.).
		- Returns the login result object:
			- access_token: the JWE token.
			- token_type: "Bearer"
			- expires_in: token expiration time in seconds.
			- refresh_token: the refresh token.
			- The client claims.
			- The encrypted claims include [MicroMServerClaimTypes](../Web/Authentication/Claims/MicroMServerClaimTypes.cs) and any additional claims provided by the [IAuthenticator](../Web/Authentication/AuthenticationProvider/IAuthentication.cs).
			- The client claims include [MicroMClientClaimTypes](../Web/Authentication/Claims/MicroMClientClaimTypes.cs) and any additional claims provided by the [IAuthenticator](../Web/Authentication/AuthenticationProvider/IAuthentication.cs).

### The control panel
- A web application for managing the MicroM Configuration Database.
- Allows creating, configuring and managing applications.
- Generates code for entities.
	- Standard SQL procedures (SQL).
	- Frontend Entity Definition and Entity (TS).
	- Scaffolded frontend Form (TSX).
