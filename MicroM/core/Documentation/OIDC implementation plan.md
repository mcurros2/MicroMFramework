# OIDC Implementation Plan (Status & Next Tasks)

Reference  
See “MicroM OIDC Multi-Tenant Flow (PAR + PKCE, SSO/SLO)” (core/Documentation/MicroM OIDC multi tenant flow.md).

A — Core features and persistence
- Discovery/JWKS/ETag: COMPLETE
- PAR + PKCE + Authorization Code: COMPLETE
- Pairwise sub derivation (per client pepper): COMPLETE
- IdP-side client session persistence: COMPLETE
  - `/oauth2/token` persists per-client session with sid, sub, IdP refresh token, UTC expiration via `ApplicationOidcActiveSessions.CreateOrUpdateIdPSession`.
  - Uses stable sentinel device_id `oidc` (subject-wide refresh).
- Client-side session persistence on callback: COMPLETE
  - Per-device local refresh via `usr_updateLoginAttempt`.
  - Links IdP sid/sub in `ApplicationOidcActiveSessions.CreateOrUpdateExternalSignInSession`.
- Encrypted refresh-token lookup: COMPLETE
  - OIDC refresh tokens stored encrypted; lookups pass encrypted value; mapper decrypts on read.
- Audience for id_token: COMPLETE (aud = client_id).
- UTC handling for IdP refresh expiration: COMPLETE
  - Generated with `DateTimeOffset.UtcNow.AddHours(app.OIDCRefreshTokenExpirationHours)`, stored UTC, validated with `DateTime.UtcNow`.
- Client refresh by SID: COMPLETE
  - `ApplicationOidcActiveSessions.GetSessionBySID(app_id, sid)` returns a unique row with decrypted `vc_oidc_refreshtoken`.

B — IdP grants
- authorization_code grant: COMPLETE
- refresh_token grant: COMPLETE

C — Client (RP) flows
- OIDC client login (PAR → authorize → token → callback): COMPLETE
- Local per-device refresh (client DB): COMPLETE
  - Device-bound table `MicromUsersDevices (c_user_id, c_device_id)` and proc `usd_refreshToken`.
  - Cookie-first validation; rotation updates cookie.
- IdP refresh support in client service: COMPLETE
  - `IOIDCClientService.RefreshIdpToken(app, sid, device_id, ct)` + JWKS validation + encrypted rotation persisted.
- Client refresh fallback wiring: COMPLETE
  - `AuthenticationService.HandleRefreshToken` tries local first, then IdP refresh on failure for IDPClient apps.

D — Logout (SLO)
- Front-channel logout (client): COMPLETE
  - Service: `OIDCClientService.BuildEndSessionRequest`, `HandleFrontChannelLogout`.
  - Controller: `GET {app_id}/oidc-client/front-logout`.
- Back-channel logout receiver (client): COMPLETE
  - Service: `OIDCClientService.HandleBackchannelLogout` validates logout_token (iss, aud=client app_id, signature, iat/replay cache, events, sid/sub), resolves sub from sid if needed, and deletes sessions by sub.
  - Controller: `POST {app_id}/oidc-client/back-logout` implemented (reads `logout_token`, maps response codes).
- IdP endsession/backchannel fan-out: COMPLETE
  - `IdentityProviderService.HandleEndSession` signs per-client `logout_token` (pairwise `sub`) and POSTs to each client’s backchannel URL; purges sessions by `sub`.
  - Controller: `POST {app_id}/oauth2/endsession` implemented; issuer computed consistent with discovery.

E — Security & hardening
- State/nonce, PKCE S256, client authentication: COMPLETE
- Encrypted OIDC refresh tokens at rest: COMPLETE
- Logging: PARTIAL
  - Sensitive token values removed from logs (values omitted). SQL parameter tracing now redacts sensitive parameter values by name/pattern.
- Rate limiting: COMPLETE
  - Middleware wired: COMPLETE (`UseRateLimiter` placed after authentication).
  - Policies defined: COMPLETE (identity-scoped partitions: app_id, client_id, device_id, UA; global per-app catch-all).
  - Endpoint attributes applied: COMPLETE across Authentication, IdP, OIDC Client, and Public controllers (gentle limits for metadata and public GETs; stricter for mutations).
  - Service registration: COMPLETE (`AddMicroMRateLimitingPolicies` invoked in `AddMicroMApiServices`).
  - Global per-node limiter: PENDING (optional; per-app catch-all in place).
- Metrics/counters: Using ASP.NET Core built-in meters and standard RateLimiter instrumentation; no custom `System.Diagnostics.Metrics` to add.

Recent changes
- Added generic `IOIDCHttpClient.PostFormUrlEncodedAsync` and used it for IdP backchannel fan-out.
- `OIDCHttpClientPostResponse` extended with `IsSuccessStatusCode`; all callers adapted.
- Implemented IdP endsession with correct issuer, signing, claims, and session purge.
- Implemented client backchannel logout controller endpoint.
- Rate limiting wiring and policies:
  - Added `UseRateLimiter` to pipeline (after auth, before authorization).
  - Implemented global per-app catch-all limiter via `options.GlobalLimiter`.
  - Defined identity-scoped policies for `auth/refresh`, `oauth2/par`, `oauth2/token`, `oauth2/authorize`, `oauth2/endsession`, backchannel logout, public endpoints, metadata, and front-logout.
  - Applied `[EnableRateLimiting]` to all relevant actions across controllers.
  - Registered policies via `services.AddMicroMRateLimitingPolicies()`.
- Logging scrub (initial):
  - Removed refresh token values from logs in `MicroMAuthenticator`.
  - Redacted sensitive SQL parameters in `TraceSQL` (passwords, tokens, refresh, recovery_code, authorization).
- Tests:
  - Added MSTest coverage for `OIDCReplayCacheService` (Added → Replay; expired `iat` → Stale; future `iat` beyond skew → Skew; too-long `jti` → Invalid).

Known gaps before release
- Comprehensive tests for SLO, refresh paths, and rate-limit behaviors.
- Final log review to ensure no secrets/tokens are emitted across all components (Authorization headers, logout_token/id_token, refresh, passwords).
- Optional global per-node limiter if needed.
- Optional observability plumbing: expose built-in ASP.NET Core meters via OpenTelemetry/Prometheus exporters (no code in auth/oidc services required).

Priority next tasks
1) Tests (MSTest)
   - Backchannel:
     - Valid `logout_token` invalidates sessions by `sub` (and by `sid` when present).
     - Validate issuer/audience/signature/events; reject malformed or expired `iat`.
     - Rate-limit behavior: repeated backchannel posts trip the policy; 429 surfaced.
   - Front-channel:
     - End-session URL build (state/post_logout_redirect_uri) and callback handling paths.
     - Rate-limit behavior on authorize/front-logout for the same `client_id + UA`.
   - IdP endsession fan-out:
     - Signs per-client pairwise `sub`, correct `aud`, `iss`; delivers to multiple backchannels.
     - Partial failure handling doesn’t block local session purge; outcomes verified via logs.
   - Auth flows:
     - Login and refresh (local + IdP fallback), SID/SUB continuity, session purge on logout.
     - Rate-limit behavior on login/refresh/recovery endpoints per partition keys.

2) Safeguards and telemetry
   - Logging scrub: continue audit for any credential/token leakage; ensure error handlers don’t echo token-bearing payloads.

3) Optional robustness
   - In `HandleEndSession`, if `ClientAPPID` is empty, fall back to the configuration key to ensure valid `aud`/`sub` derivation.
   - Optionally include `sid` in `logout_token` when available (spec allows `sid` or `sub`).
   - Consider client-provided stable device header for AllowAnonymous endpoints to improve partition quality (reduces reliance on UA).
   - Consider enabling a global per-node limiter if needed post-observation.

Status TL;DR
- IdP: grants and endsession fan-out complete with correct signing and claims.
- Client: refresh flows complete; SLO front-channel and backchannel wired.
- Rate limiting: fully wired and applied; tests started with replay cache; remaining work focuses on broader test coverage and final log scrub.


