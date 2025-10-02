# OIDC Implementation Plan (Current Status & Next Tasks)

A — Persistence & Core Services
- ETag cache, Certificate cache, JWKS service: COMPLETE
- JwksProvider (fetch + validate via named client): COMPLETE
- Backchannel client auth handler (IdP): COMPLETE (Basic + private_key_jwt; local cert/JWKS fallback)
- Authorization code store (in-memory, PKCE): COMPLETE
- PushedAuthorizationService (PAR store): COMPLETE
- Centralized HttpClients (named OIDC/JWKS): COMPLETE
- Typed OIDC HTTP client (`OIDCHttpClient`): COMPLETE (used by client for WellKnown, PAR, Token)
- ApplicationOidcActiveSessions entity: COMPLETE (app/user/device/sid, optional IdP refresh)

B — Login / SSO Creation
- IdP interactive login and SSO session: COMPLETE
  - `AuthenticationService` creates IdP SSO session; `sid` available in server claims
- Client local-login guard: COMPLETE
- Client callback logic (code → tokens → principal): COMPLETE
  - `OIDCClientService.HandleSignInOidcCallback` returns principal, expiresUtc, optional `idpRefreshToken`, and propagated `local_device_id`
- Client callback HTTP endpoint: COMPLETE
  - `/oidc-client/callback` issues the local session via `IAuthenticationService.SignInAsync`
  - JIT provisioning and per-device refresh issuance are executed via `IAuthenticator.HandleExternalSignIn` (MicroMAuthenticator)
  - Returns SPA token envelope (`access_token`, `token_type`, `expires_in`, `refresh-token` currently empty; cookie is set server-side)

C — Authorization Flow (PAR + Authorize)
- IdP PAR/Authorize flow: COMPLETE (request_uri TTL, strict redirect, PKCE)
- Client PAR forwarder: COMPLETE (AllowAnonymous, CORS; strongest-first auth; typed client used)
  - SPA-provided `local_device_id` is captured at login and persisted in HMAC-protected state cookie (not forwarded to IdP)

D — EndSession / Single Logout (SLO)
- IdP end_session + backchannel logout emission: PENDING
- Client backchannel logout receiver: PENDING
- Client local logoff → IdP end_session: PENDING

E — Session Binding & Security
- State & nonce: COMPLETE (cookie-based store/validate; HMAC-protected)
- Nonce verification against id_token: PENDING (IdP must emit nonce)
- sid propagation (authorize → id_token → client): COMPLETE
  - Client stores `sid` in claims; DB linkage via `ApplicationOidcActiveSessions`: PENDING
- Local device binding: COMPLETE
  - SPA posts `local_device_id` to OIDC login; value is stored in state cookie and recovered at callback, used to bind the local session/device.
- Local sessions per device with local refresh: PARTIAL/COMPLETE
  - Per-device refresh token issuance and HttpOnly cookie are implemented in `MicroMAuthenticator.HandleExternalSignIn`
  - `/auth/refresh` path already rotates/validates per-device refresh tokens (server-side). Response body’s `refresh-token` from callback remains empty; refresh cookie is authoritative.

F — OIDC Client API Surface
- GET /{app}/oidc-client/jwks: COMPLETE
- POST /{app}/oidc-client/login (PAR): COMPLETE
- /{app}/oidc-client/callback (GET/POST): COMPLETE (local session issuance + device binding + JIT + per-device refresh issuance)
- POST /{app}/auth/login (IdPClient guard/wrapper): PARTIAL
- POST /{app}/auth/refresh (local refresh per device): PARTIAL/COMPLETE (server flow implemented)
- Backchannel logout endpoint: PENDING

G — Tokens & Claims
- id_token validation (iss/aud/exp/signature via JWKS): COMPLETE
- azp claim inclusion: COMPLETE
- sid claim inclusion: COMPLETE
- Refresh token lifecycle: PARTIAL/COMPLETE
  - Per-device issuance at callback (cookie); rotation on refresh implemented
  - DB linkage of `sid` and optional IdP refresh token storage: PENDING
- Well-known identity constants (no string literals): COMPLETE
  - Added `local_device_id` constant and wiring through state cookie

H — Tests
- PAR auth variants, PKCE, id_token failures, sid propagation, SLO fan-out, callback E2E: PENDING
- CLIENT per-device refresh flow; SLO by sid vs sub; backchannel idempotency (jti): PENDING
- Device ID propagation E2E (SPA → PAR → state cookie → callback → session binding): PENDING
- JIT provisioning path (user missing → provisioned → refresh issued → claims loaded): PENDING

I — Operational / Observability
- CORS per app: COMPLETE
- CSRF protections, rate limiting, metrics, tracing: PARTIAL/PENDING

Recent changes
- Implemented JIT provisioning and per-device refresh issuance at OIDC callback via `IAuthenticator.HandleExternalSignIn` (MicroMAuthenticator).
- OIDC callback refactored to use authenticator external sign-in and to issue local session consistently.
- Introduced explicit result types for state/nonce and OIDC client flows (removed tuples).
- Implemented propagation of `local_device_id` using the HMAC-protected state cookie; used to bind device at callback.

Next tasks (priority)
1) DB links for sessions
   - Upsert `ApplicationOidcActiveSessions` with (`app_id`, `username`, `device_id`, `sid`) on callback
   - Optionally store IdP `refresh_token` (encrypted) and manage background refresh if needed

2) CLIENT /auth/refresh alignment
   - Ensure SPA behavior is consistent with cookie-based refresh; optionally include `refresh-token` in callback response body for parity with local login

3) EndSession / SLO
   - CENTRAL: implement `/oauth2/endsession` + backchannel `logout_token` emission
   - CLIENT: implement `/oidc-client/backchannel-logout` to validate token (CENTRAL JWKS), terminate sessions by `sid` or fallback to username/sub, delete refresh entries; enforce idempotency via `jti` replay cache

4) Nonce claim wiring
   - IdP: emit `nonce` in `id_token`; CLIENT: validate against stored nonce

5) Tests
   - E2E: callback → JIT → per-device refresh → DB link; PAR/PKCE error paths; SLO fan-out; nonce validation; logout jti replay; device-id propagation

6) Security/ops
   - Encrypt any stored IdP `refresh_token` at rest; rotate on refresh; delete on logout/backchannel
   - Rate limit OIDC endpoints; add metrics/logging around PAR/Token/Callback; SameSite=Lax for cookies where appropriate
