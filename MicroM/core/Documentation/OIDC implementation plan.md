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
  - SQL procs now return `c_user_id` and `vc_oidc_sub`.
  - Lookups pass encrypted value; mapper decrypts on read.
- Audience for id_token: COMPLETE (aud = client_id).
- UTC handling for IdP refresh expiration: COMPLETE
  - Generated with `DateTimeOffset.UtcNow.AddHours(app.OIDCRefreshTokenExpirationHours)`, stored UTC, validated with `DateTime.UtcNow`.

B — IdP grants
- authorization_code grant: COMPLETE
  - Ensures sid, generates id/access tokens, persists IdP session, rotates IdP refresh as needed.
- refresh_token grant: COMPLETE
  - Validates client, looks up session by app_id + encrypted refresh token, verifies UTC expiration, rotates refresh, re-issues tokens, persists.

C — Client (RP) flows
- OIDC client login (PAR → authorize → token → callback): COMPLETE
- Local per-device refresh (client DB): COMPLETE
- Optional background IdP refresh (Strategy B): PARTIAL
  - Client persists IdP refresh token (optional).
  - Fallback logic is described; implementation in `IOIDCClientService` is pending.

D — Logout (SLO)
- Replay cache scaffolding: COMPLETE
- Back-channel logout receiver on client: PARTIAL
  - Endpoint in place; validation + data actions largely implemented.
- IdP endsession/backchannel fan-out: PENDING
- Front-channel logout: PENDING

E — Security & hardening
- State/nonce, PKCE S256, client authentication: COMPLETE
- Encrypted refresh tokens at rest: COMPLETE
- Rate limiting/metrics: PENDING

Recent changes
- IdP token endpoint refactored with provider helpers:
  - `ParseTokenRequest`, `GenerateAuthTokens`, `EnsureSID`, `UpsertIdPSession`, refresh lookup helpers.
- All SQL procs used by the mapper return `c_user_id` + `vc_oidc_sub`.
- `ApplicationOption.OIDCRefreshTokenExpirationHours` drives IdP refresh TTL.

Known gaps before release
- Implement OIDC client service “IdP refresh” (Strategy B) end-to-end.
- IdP `/oauth2/endsession` + backchannel fan-out to clients.
- Client front-channel logout.
- Tests and basic metrics/limits.

Priority next tasks (focus: client-side IdP refresh)
1) Implement IdP refresh in OIDC client service
   - Add to `IOIDCClientService`:
     - `Task<(OIDCClientCallbackResult result, string? error)> RefreshIdpTokensAsync(ApplicationOption app, string clientId, string idpRefreshToken, string sid, CancellationToken ct)`
       - Uses the client’s backchannel auth (same as PAR/token) to call IdP `/oauth2/token` with `grant_type=refresh_token`.
       - Validates id_token (iss, aud=client_id, signature).
       - Returns new id/access tokens + new IdP refresh token and expiration.
   - Implement in `OIDCClientService`:
     - Build and send refresh request (client_secret_basic or private_key_jwt).
     - Validate response tokens with IdP JWKS cache.
   - Persist on client:
     - Call `ApplicationOidcActiveSessions.CreateOrUpdateExternalSignInSession` to rotate the IdP refresh token (encrypted) and expiration in client DB.
     - Optionally update local claims/cookie if you want to surface any refreshed claims from the new id_token immediately.

2) Wire client refresh fallback
   - In client’s local `/auth/refresh` path, when local per-device refresh fails:
     - If a stored IdP refresh token exists (for the current user/device/sid), call `RefreshIdpTokensAsync`.
     - On success: update `ApplicationOidcActiveSessions` row with the rotated IdP refresh token/expiration; optionally refresh local claims/cookie.
     - On failure: instruct SPA to re-initiate OIDC via `/oidc-client/login`.

3) Add basic safeguards and telemetry
   - Rate limit refresh attempts per device and per IdP refresh token (configure via `ApplicationOption.MaxRefreshTokenAttempts` or a new setting).
   - Structured logs: client_id, user_id, sid, sub, jti (do not log tokens).
   - Counters for issued/rotated IdP refresh tokens.

4) Tests
   - Client IdP refresh happy path: rotates token, updates client DB, builds valid id/access tokens.
   - Expired refresh token handling at client: falls back to interactive OIDC.
   - IdP e2e refresh: lookup-by-encrypted-token + UTC expiry and rotation.
   - Regression: authorization_code path still persists correctly (sid continuity).

Secondary tasks
- Implement IdP `/oauth2/endsession` to terminate SSO and perform backchannel logout to each registered client.
- Client front-channel logout endpoint and integration with SPA UX.
- Basic metrics (prometheus counters) and minimal DoS protections on `/oauth2/token`.

Status TL;DR
- IdP: authorization_code and refresh_token grants done with correct UTC, audience, and encrypted refresh storage. Sessions persisted on issuance/refresh.
- Client: login/callback/local-refresh finished; IdP-refresh fallback is the next work item to complete Strategy B at the client side.


