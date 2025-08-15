# Applications

Stores configuration for each MicroM application.

## Columns
- `c_application_id` (PK)
- `vc_appname`
- `vc_appurls` – array of frontend URLs (fake)
- `vc_apiurl`
- `vc_server`
- `vc_user`
- `vc_password` – encrypted
- `vc_database`
- `vc_app_admin_user`
- `vc_app_admin_password` – encrypted
- `vc_JWTIssuer`
- `vc_JWTAudience`
- `vc_JWTKey`
- `i_JWTTokenExpirationMinutes`
- `i_JWTRefreshExpirationHours`
- `i_AccountLockoutMinutes`
- `i_MaxBadLogonAttempts`
- `i_MaxRefreshTokenAttempts`
- `c_authenticationtype_id` – category [`AuthenticationTypes`](../../CategoriesDefinitions/AuthenticationTypes.md)
- `vc_assembly1` … `vc_assembly5` – embedded assemblies (fake)
- `b_createdatabase`, `b_dropdatabase`, `b_adminuserhasrights`, `b_appdbexists`, `b_appuserexists`, `b_serverup` – action/status flags (fake)
- `c_identity_provider_role_id` – category [`IdentityProviderRole`](../../CategoriesDefinitions/IdentityProviderRole.md)
- `vc_oidc_url_wellknown`, `vc_oidc_url_jwks`, `vc_oidc_url_authorize`, `vc_oidc_url_token_backchannel`, `vc_oidc_url_endsession`

## Relationships
- Category links to `AuthenticationTypes` and `IdentityProviderRole`.

## Typical Usage
Represents the core configuration for an application including database credentials, JWT settings and optional OpenID Connect endpoints. Used when creating or updating application records and bootstrapping databases.
