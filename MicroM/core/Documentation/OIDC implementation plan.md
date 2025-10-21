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
- Logging: PARTIAL (token values largely removed; review remaining traces for secrets)
- Rate limiting/metrics: PENDING

Recent changes
- Added generic `IOIDCHttpClient.PostFormUrlEncodedAsync` and used it for IdP backchannel fan-out.
- `OIDCHttpClientPostResponse` extended with `IsSuccessStatusCode`; all callers adapted.
- Implemented IdP endsession with correct issuer, signing, claims, and session purge.
- Implemented client backchannel logout controller endpoint.

Known gaps before release
- Safeguards and telemetry (rate limits and counters).
- Comprehensive tests for SLO and refresh paths.
- Final log review to ensure no secrets/tokens are emitted.

Priority next tasks
1) Safeguards and telemetry
   - Rate-limit `/auth/refresh`, IdP endsession fan-out, and client backchannel handling.
   - Counters for logout fan-out (sent/succeeded/failed) and backchannel outcomes (success/replay/fail).
   - Ensure all logs avoid printing tokens or secrets.
2) Tests
   - Backchannel: valid token invalidates sessions; replay/idempotency; issuer/audience/signature/events validations.
   - Front-channel: end_session URL build and callback handling.
   - IdP endsession fan-out: valid JWTs per client; handles delivery failures gracefully.
   - Regression: login/refresh (local + IdP fallback), SID/SUB continuity and session purge.
3) Optional robustness
   - In `HandleEndSession`, if `ClientAPPID` is empty, fall back to the configuration key to ensure valid `aud`/`sub` derivation.
   - Optionally include `sid` in `logout_token` when available (spec allows `sid` or `sub`).

Status TL;DR
- IdP: grants and endsession fan-out complete with correct signing and claims.
- Client: refresh flows complete; SLO front-channel and backchannel fully wired; pending telemetry and tests.


