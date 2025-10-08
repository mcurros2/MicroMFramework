# OIDC Implementation Plan (Current Status & Next Tasks)

Reference
See “MicroM OIDC Multi-Tenant Flow (PAR + PKCE, SSO/SLO)” (`core/Documentation/MicroM OIDC multi tenant flow.md`).

A — Persistence & Core Services
- Core caches & JWKS: COMPLETE (`EtagCacheService`, `ApplicationCertificateCacheService`, `JwksService`)
- JWKS validation helper: COMPLETE (`JwksProvider.ValidateIdTokenAsync`)
- Backchannel client auth (IdP): COMPLETE
- Authorization code store (PKCE + nonce + redirect_uri + expiry): COMPLETE
- PAR store & forwarding: COMPLETE
- Centralized HttpClients + typed OIDC client: COMPLETE
- Active OIDC session persistence: COMPLETE (refactored)
  - Entity: `ApplicationOidcActiveSessions`
  - Session id column: `vc_oidc_session_id` (string; replaces previous Guid-based column)
  - Subject column: `vc_oidc_sub` (nullable, indexed) persisted directly from IdP `sub`
  - Refresh token column enlarged to `VarChar(2048)` (`vc_oidc_refreshtoken`, encrypted)
  - Unique constraint: `(c_application_id, vc_oidc_session_id)` (one active sid per app)
  - One row per (app, username, device); sid rotation updates the existing row
  - Query procs: `aos_getSessionsBySID`, `aos_getSessionsByUser`, `aos_getSessionsBySUB`
  - Delete helpers: `aos_deleteUserSessions`, `aos_deleteSessionSID`, `aos_deleteAllSessions`

B — Login / SSO Creation
COMPLETE (`/oidc-client/auth-callback`)

C — Authorization Flow
COMPLETE (PAR + Authorize + PKCE + state/nonce)

D — EndSession / Single Logout (SLO)
STATUS: IN PROGRESS (session layer + replay cache complete)
Policy: Backchannel logout performs user-wide (all devices) invalidation per application, not just the single sid row.
Outstanding:
- IdP `endsession` endpoint + logout_token issuance per client
- Client backchannel endpoint logic
- Refresh token invalidation batch proc

E — Session Binding & Security
COMPLETE (state/nonce HMAC, TTL, device binding, persisted sid & sub)

F — OIDC Client API Surface
Core endpoints in place; logout endpoints (backchannel / front) PENDING

G — Tokens & Claims
COMPLETE (iss, aud, exp, signature, nonce, sid, azp handled)

H — Tests
PENDING (no automated coverage for SLO & replay yet)

I — Operational / Observability
PENDING (rate limiting, metrics, structured logs)

Recent changes
- Replaced GUID sid with string `vc_oidc_session_id`
- Added `vc_oidc_sub` (indexed) for sub-only logout fallback
- Added session enumeration & delete procs (sid/user/sub)
- Enlarged refresh token storage to 2048 chars
- Normalized blank subject → null
- Replay cache implemented (simplified service)
  - Service: `OIDCReplayCacheService` (`IOIDCReplayCacheService`)
  - Fixed TTL = 10m, clock skew = 2m, max JTI length = 256
  - In-memory (IMemoryCache) key prefix `oidc:logout:jti:`
- Clarified SLO: global user sweep per app

Known gaps before release
- IdP `endsession` + logout_token generation & dispatch
- Client backchannel logout handling + refresh invalidation
- Refresh token invalidation proc (user/device scope)
- Replay handling integration in backchannel endpoint
- Unit/integration tests (state/nonce tamper, SLO, replay)
- Metrics (logout tokens processed, sessions removed, replays)
- Rate limiting on sensitive endpoints
- Optional IdP endpoints (userinfo / introspect / revoke)
- Admin API for subject-wide purge (DB ready, API missing)

## SLO Implementation Roadmap

Phase 0 — Contracts & Naming (COMPLETE)

Phase 1 — Session Layer (COMPLETE)

Phase 2 — Replay Cache (COMPLETE)
- Implemented simplified fixed-option cache (`OIDCReplayCacheService`)

Phase 3 — Backchannel Logout (Client) (NEXT)
- Implement `HandleBackchannelLogoutAsync` in `OIDCClientService`
  - Parse/validate logout_token (signature, iss, aud, events, iat)
  - Extract `sid` & `sub`; prefer sub for global sweep
  - Replay check via `IOIDCReplayCacheService`
  - Enumerate + delete sessions (`aos_deleteUserSessions`) + invalidate refresh tokens (new proc)
  - Idempotent success (200) for replay / unknown user
  - Structured logging

Phase 4 — Endpoints
- POST `/{app}/oidc-client/backchannel-logout`
- GET  `/{app}/oidc-client/front-logout`
- (Optional) initiation endpoint

Phase 5 — IdP Endsession
- `POST /{app}/oauth2/endsession`
- sid → sub → enumerate apps → issue logout_token per app (include both sid & sub where possible)

Phase 6 — Tests
- Replay cache unit tests
- Logout token validation matrix
- Integration (end-to-end SLO)
- Negative & idempotency coverage

Phase 7 — Metrics & Observability
- Counters & latency
- Structured logs with correlation fields

## Immediate Next Tasks

1. Implement backchannel logout logic (`HandleBackchannelLogoutAsync`) using replay cache (Phase 3)
2. Expose backchannel and front logout endpoints
3. Add refresh invalidation stored proc + service integration
4. Implement IdP `endsession` + logout_token issuance
5. Add initial SLO-focused tests (replay, user sweep, idempotency)
6. Add metrics & structured logging
7. Apply rate limiting to key endpoints

## Adjusted Task Status
- Replay cache: COMPLETE
- Session enumeration (sid/sub/user): COMPLETE
- Refresh invalidation: PENDING
- Backchannel & endsession endpoints: PENDING

Release gating
- Backchannel + endsession with replay protection
- User-wide invalidation verified (sub fallback)
- Core tests (state/nonce, token validation, SLO idempotency)
- Metrics/logging baseline

Risks
- Without refresh invalidation: refresh tokens might survive SLO
- Without metrics/logging: limited visibility into coverage & failures
- Without rate limiting: increased abuse surface


