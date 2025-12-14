# MicroM OIDC Multi-Tenant Flow (PAR + PKCE, SSO/SLO)

This document describes how OIDC works across multiple applications (tenants) hosted in a single backend (api.com), with one application acting as the Identity Provider (IdP) and others acting as OIDC clients.

Scope
- IdP supports Single Sign-On (SSO) and Single Logout (SLO) only.
- Client apps do not collect credentials; they initiate OIDC using PAR and rely on IdP’s login SPA.

Terminology
- CENTRAL: IdP app (role: IdPServer), frontend: idp.com, backend base: api.com/microm/central
- CLIENT: OIDC client app (role: IdPClient), frontend: client.com, backend base: api.com/microm/client
- SPA: Frontend of each app (idp.com / client.com), talking to its own backend (api.com)

0) Persistence model
- Configuration DB (shared)
  - Applications: per-app configuration and security settings (connection info, JWT settings, role, certificates). Also returns client registrations via `app_GetOIDCClients`.
  - ApplicationOidcClients, MicromApplicationCertificates, MicromApplicationApiKeys: support client registrations and certificate material.
- Application DB (per app)
  - MicromUsers, MicromUsersDevices: local identities and per-device refresh tokens for local sessions.
  - ApplicationOidcActiveSessions: IdP session correlation per app. Columns include:
    - `c_application_id`, `vc_username`, `c_device_id`, `c_session_id` (PKs)
    - `vc_oidc_session_id` (sid), `vc_oidc_refreshtoken` (optional, encrypted), `dt_refresh_expiration`
  - CLIENT upserts a row on OIDC callback to link sid ↔ user/device; optionally stores IdP refresh_token for background claim refresh.

1) Endpoints per application
- CENTRAL (IdPServer)
  - Discovery: GET api.com/microm/central/oidc/.well-known/openid-configuration
  - JWKS: GET api.com/microm/central/oidc/jwks
  - PAR: POST api.com/microm/central/oauth2/par
  - Authorize: GET api.com/microm/central/oauth2/authorize
  - Token: POST api.com/microm/central/oauth2/token
  - End Session (SLO): POST api.com/microm/central/oauth2/endsession
  - IdP login (credentials): IdP SPA at idp.com/login posts to api.com/microm/central/auth/login and validates user and password according to configured Authenticator
  - IdP logoff: POST api.com/microm/central/auth/logoff (triggers endsession)

- CLIENT (IdPClient)
  - Client JWKS: GET api.com/microm/client/oidc-client/jwks (for private_key_jwt if used)
  - Client PAR forwarder: POST api.com/microm/client/oidc-client/par (SPA → backend; backchannel to CENTRAL /par; returns redirect_uri)
  - Client callback (redirect_uri): GET/POST api.com/microm/client/oidc-client/callback
  - Local login: POST api.com/microm/client/auth/login
	- If app is IdPClient, this endpoint does not accept credentials; it validates authorize params and forwards to /oidc-client/par (PAR), returning PAR result to the SPA.
  - Local refresh: POST api.com/microm/client/auth/refresh
	- Uses CLIENT-issued refresh tokens (per device) to renew the local session without contacting CENTRAL
  - Local logoff: POST api.com/microm/client/auth/logoff (if OIDC client, call IdP endsession)
  - Backchannel logout receiver: POST api.com/microm/client/oidc-client/backchannel-logout

2) High-level flow (SSO vs no SSO)
- Client SPA needs a session → SPA POSTs to api.com/microm/client/oidc-client/par with authorize params (scope, state, nonce, redirect_uri, code_challenge=S256).
- Client backend forwards to CENTRAL /oauth2/par (client_secret_basic or private_key_jwt). IdP responds with request_uri.
- SPA redirects to CENTRAL /oauth2/authorize?client_id=CLIENT&request_uri=...
  - If user has IdP SSO → IdP immediately redirects to CLIENT redirect_uri with code & state.
  - Else → IdP redirects to IdP login SPA (idp.com/login) → posts credentials to api.com/microm/central/auth/login → IdP creates SSO session and resumes authorize → redirects with code & state.
- CLIENT backend receives code at redirect_uri, validates state, exchanges code at CENTRAL /oauth2/token with PKCE, validates id_token, provisions/updates the local user if needed, and creates local session (HttpOnly, Secure, SameSite).
- SPA proceeds with local session.
- CLIENT maintains local sessions per device via its own refresh tokens (Strategy B) and minimizes calls to CENTRAL.

2.1) Local refresh (per device) with optional IdP background refresh
- On successful CLIENT callback:
  - JIT provision user (if needed), create local cookie session (claims include `sub`, `sid`, `azp`), issue a CLIENT refresh token per device (MicromUsersDevices).
  - Upsert ApplicationOidcActiveSessions with (`c_application_id`, `vc_username` or `sub`, `c_device_id`, `ui_oidc_session_guid_id`), optionally `vc_oidc_refreshtoken` + `dt_refresh_expiration` if background IdP refresh is desired.
- /auth/refresh (CLIENT):
  - Validate/rotate the CLIENT refresh token (per device) and re-issue local cookie; do not call CENTRAL. Same as APPs that valdiate locally (IDPDisabled).
    - If local refresh fails and an IdP refresh_token exists, attempt refresh at CENTRAL as a one-time fallback; otherwise instruct SPA to re-initiate OIDC via /oidc-client/par.
  - IdP refresh expiration is configured on the IdP at a longer cadence (e.g., 30 days). CLIENT should not issue a refresh token with an expiration greater than the IdP refresh expiration.
  - CLIENT issues its own refresh token with a shorter expiration (e.g., 1 day) and rotates it on each /auth/refresh.
  - If local refresh fails and an IdP refresh_token exists, attempt a one-time IdP refresh; otherwise instruct SPA to re-initiate OIDC via /oidc-client/par.
- Tokens remain server-side only; never return IdP refresh_token to the SPA.

3) CLIENT local login behavior (guard)
- If app is configured as IdPClient, the local login endpoint (api.com/microm/client/auth/login) must:
  - Not accept credentials.
  - Return 400 (or a specific code) instructing the SPA to start OIDC flow via /oidc-client/par.

4) Just-In-Time (JIT) provisioning at CLIENT
- When CLIENT validates id_token:
  - If user does not exist in MicromUsers, create it using IdP username/subject and a random password.
  - Map user to CLIENT tenant and persist minimal profile.
  - Create local session and persist the IdP session identifier (sid) if present.
- Additionally:
  - Persist `sub` and `sid` in local session (claims).
  - Issue a CLIENT refresh token per device (MicromUsersDevices) and set HttpOnly, Secure cookie.
  - Upsert ApplicationOidcActiveSessions with (`c_application_id`, `vc_username` or `sub`, `c_device_id`, `ui_oidc_session_guid_id`), and optionally store `vc_oidc_refreshtoken` (encrypted) + `dt_refresh_expiration`.

5) Session correlation (sid)
- IdP login (CENTRAL) creates/maintains an SSO session in ApplicationOidcActiveSessions, identified by a GUID (sid).
- IdP should:
  - Bind sid into the issued authorization code (server side).
  - Include sid in id_token so CLIENT can link local sessions to the IdP session.
- CLIENT should store sid in its local session for SLO correlation.
- Notes:
  - Each device/browser profile typically has its own sid; multiple CLIENT apps authorized within the same browser session share the same sid.
  - If backchannel `logout_token` has `sid`: terminate only sessions linked to that `sid`; if only `sub`/username: terminate all device sessions for the user at the CLIENT.

6) Logout (SLO)
- IdP-initiated SLO:
  - CENTRAL endsession terminates IdP SSO session and POSTs a backchannel logout_token (JWT) to each CLIENT’s configured URLBackchannelLogout.
  - CLIENT validates logout_token (CENTRAL JWKS), finds local sessions by sid/sub, invalidates them (idempotent by jti).
  - On CLIENT: for each matched record, clear the device’s local cookie session, revoke its MicromUsersDevices refresh token, and delete related ApplicationOidcActiveSessions rows.
- Client-initiated logout:
  - CLIENT local logoff clears local session and, when configured as OIDC client, initiates IdP endsession (back-channel) at CENTRAL.
  - CENTRAL endsession performs backchannel as above.
  - Detailed documentation flow in `OIDC SLO flow.md`.
  
7) Security considerations
- PAR: Use client_secret_basic or private_key_jwt (preferred). Redirect URIs are strictly normalized and validated.
- PKCE: Always use S256.
- State/nonce: Enforce validation. For protected POSTs, use a “buffer and resume” pattern (state carries a resume token).
- JWKS/ETag: IdP/client serve JWKS with ETag; cache with appropriate TTL; rotate keys safely.
- Backchannels:
  - IdP backchannel auth handler supports BASIC and private_key_jwt. Hosted clients can validate via local certificate cache; external clients via URLClientJWKS.
- CSRF/CORS/rate limiting:
  - Client’s /oidc-client/par is called by the SPA (front-channel). Use CORS allowlist and CSRF protections. Do not gate it behind IdP backchannel auth.
- Additional:
  - CLIENT refresh tokens are per device; enforce rotation and throttle validation attempts to mitigate abuse.
  - If storing IdP refresh_token in ApplicationOidcActiveSessions, keep it server-only and encrypted; rotate on refresh; delete on logout/backchannel.
  - Cookie SameSite=Lax is recommended for OIDC redirect flows.
  - Maintain a jti replay cache for backchannel `logout_token` idempotency.

8) Error handling
- Invalid client, redirect mismatches, expired PAR, PKCE failures → return proper OAuth errors and HTTP 400; unauthorized client auth → 401 with WWW-Authenticate if applicable.
- JWKS not configured → return 404 to reduce info leakage.
- CLIENT /auth/refresh failures:
  - If IdP refresh_token exists and refresh succeeds → update local session silently.
  - Otherwise, instruct the SPA to re-initiate OIDC via /oidc-client/par.

## target_link_uri handling

We support passing `target_link_uri` as part of the pushed authorization request (PAR). Purpose: when an RP needs the IdP to redirect the user to a different client-managed endpoint after authorization (for example application-specific deep links), the RP can include a validated `target_link_uri` in the PAR.

Rules:
- The RP may send `target_link_uri` in the PAR body to the public client endpoint `/microm/{app_id}/oidc-client/par`. The client-side wrapper forwards it to the IdP PAR pipeline.
- The IdP will validate `target_link_uri`:
  - Must be an absolute URI (scheme://host[:port]/path).
  - Its origin (scheme + host + port) must match one of the client's registered redirect URIs or be explicitly allowed for that client.
  - If validation fails, the PAR is rejected with a structured error using reason code `target_link_uri_invalid`.
- If validated, `target_link_uri` is persisted with the PAR and consumed at authorization completion to produce the final redirect to the RP.

Security notes:
- Do not accept `target_link_uri` values that point to unknown origins or non-HTTPS origins in production.
- Log only reason codes; never log full `target_link_uri` in diagnostics or telemetry to avoid leakage in logs.
