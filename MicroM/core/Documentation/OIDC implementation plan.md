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
- Audience for id_token: COMPLETE (aud = client_id).
- UTC handling for IdP refresh expiration: COMPLETE
- Client refresh by SID: COMPLETE

B — IdP grants
- authorization_code grant: COMPLETE
- refresh_token grant: COMPLETE

C — Client (RP) flows
- OIDC client login (PAR → authorize → token → callback): COMPLETE
- Local per-device refresh (client DB): COMPLETE
- IdP refresh support in client service: COMPLETE
- Client refresh fallback wiring: COMPLETE

D — Logout (SLO)
- Front-channel logout (client): COMPLETE
- Back-channel logout receiver (client): COMPLETE
- IdP endsession/backchannel fan-out: COMPLETE

E — Security & hardening
- State/nonce, PKCE, client authentication: COMPLETE
  - Runtime state cookie is HMAC’ed, single-use.
  - Nonce will be updated to base64url (see tasks).
  - PKCE enforcement: currently accepts `S256` (preferred) and `plain`; plan updated to NOT enforce S256-only — acceptance will track discovery-advertised methods.
- Encrypted OIDC refresh tokens at rest: COMPLETE
- Logging: PARTIAL (scrub pending)
- Rate limiting: COMPLETE
- Metrics/counters: Using ASP.NET Core built-in meters.

F — Encrypted Tokens (scope)
Goal: Support optional JWE encryption of id_token (and later userinfo / request objects) using asymmetric crypto based on client/IdP certificates. No symmetric/shared-secret encryption for OIDC. Note: `app.JWTKey` is only for local API JWT use, not for OIDC.

Current status (UPDATED):
- Discovery advertises asymmetric-only `id_token_encryption_alg_values_supported` and `id_token_encryption_enc_values_supported`.
- Removed unsupported RSA_OAEP_256 from advertisement (library / .NET 8 does not provide `RsaOaep256` constant).
- EC key encryption order: ECDH_ES_A256KW > ECDH_ES.
- RSA key encryption order: RSA_OAEP > RSA1_5.
- Subject types advertisement implemented directly as constant `[public, pairwise]`: COMPLETED.
- IdP id_token issuance implemented: always signed; encrypts (JWE) when certificate supports RSA-OAEP or ECDH-ES(+A256KW). COMPLETED.
- Client decrypt path: COMPLETED.

Planned phases (adjusted) — do not remove
1. Phase 1 (IdP): Implement JWE wrapping (auto by cert). COMPLETED
2. Phase 2 (Client): Add decryption path; handle signed or signed+encrypted. COMPLETED
3. Phase 3 (Diagnostics): Verify encryption negotiation (incl. absence of RSA_OAEP_256). PENDING
4. Phase 4 (UserInfo / request object) encryption (optional). PENDING
5. Phase 5: Policy flags dropped (selection fully capability-driven). COMPLETED

Key selection rules (implemented; asymmetric only):
- Key Encryption (alg):
  - RSA: RSA-OAEP > RSA1_5 (RSA_OAEP_256 removed as unsupported).
  - EC: ECDH-ES+A256KW > ECDH-ES.
  - Symmetric algorithms (dir, AxxxKW/GCMKW, PBES2-*) NOT supported.
- Content Encryption (enc) preference: A256GCM > A256CBC-HS512 > A192GCM > A192CBC-HS384 > A128GCM > A128CBC-HS256 (IdP currently emits first supported; discovery lists full ordered set).

G — JWKS caching and conditional fetches (UPDATED)
Status: COMPLETE
- Shared JWKS cache service: COMPLETED
  - `IJWKSFetchCacheService` + `JWKSFetchCacheService` cache per `jwks_uri` (raw JSON, parsed `OIDCJwksResponse`, materialized `kid → SecurityKey`).
  - TTL-based refresh via `ConfigurationDefaults.JwksCacheDurationSeconds` (prevents indefinite staleness).
  - Rotation handling: on `kid` miss, force-refresh once and retry.
  - Manual invalidation supported.
- ETag cache TTL: COMPLETED
  - `IEtagCacheService` and `EtagCacheService` support TTL for sync/async; entries track `CachedUtc`/`ExpiresUtc` and honor serve-stale-on-error.
- HTTP client conditional GET: COMPLETED
  - `OIDCHttpClient.GetWellKnownJsonAsync` and `GetJwksJsonAsync` accept optional `ifNoneMatch`, send `If-None-Match`, capture `ETag`, and handle `304 Not Modified` (`NotModified=true`).
  - Size guards: well-known (32KB), JWKS (64KB).
- Wiring: COMPLETED
  - `JWKSFetchCacheService` now sends the last server ETag (tracked per `jwks_uri`) in `If-None-Match` and reuses cached body on `304`. Falls back to unconditional fetch if body is missing (rare).

New tasks (UPDATED)
- Diagnostics:
  - Review impact of encryption and signing on existing diagnostics. — PENDING 
  - Add `ClientIdTokenEncryptionCheck`. — PENDING
  - Add `IdpEncryptionCapabilityCheck` (validate advertised alg list excludes RSA_OAEP_256). — PENDING
  - Add `JwksCacheEffectivenessCheck` (initial fetch, reuse/304, rotation fallback). — PENDING
- Tests:
  - RSA cert → RSA_OAEP + A256GCM. — PENDING
  - EC cert → ECDH_ES_A256KW + A256GCM. — PENDING
  - Fallback to signed-only when no asymmetric enc capability. — PENDING
  - JWKS cache: TTL refresh, kid-miss forced refresh, and 304 path coverage. — PENDING
- Logging scrub: Ensure encrypted tokens/JWKS bodies never logged. — PENDING
- IdP integration: Use shared JWKS cache for private_key_jwt validation against client JWKS URLs. — PENDING

Recent changes (UPDATED)
- private_key_jwt alg selection parity: COMPLETE
- Discovery asymmetric encryption only: COMPLETE
- Subject types constant: COMPLETED
- IdP id_token sign/encrypt (capability-based, no config flag): COMPLETED
- Removed unsupported RSA_OAEP_256 from code + discovery: COMPLETED
- Updated encryption selection logic to iterate supported alg/enc sets: COMPLETED
- Client decryption path implemented (uses client cert during validation): COMPLETED
- JWKS shared cache service with TTL and kid-miss refresh: COMPLETED
- ETag cache service gained TTL support: COMPLETED
- OIDCHttpClient supports ETag (If-None-Match) + 304 handling; size guards: COMPLETED
- JWKS cache now uses server ETag for outbound `If-None-Match` and reuses cached body on `304`: COMPLETED

Diagnostics conventions
(No change; encryption and JWKS checks skip when not negotiated/advertised.)

Pending tasks (consolidated)
- IdP
  1. PKCE policy alignment. — PENDING
  2. EndSession: Add sid claim when available. — PENDING
  3. Logging scrub (include encryption/JWKS paths). — PENDING
  4. Tests for discovery alg ordering + encryption (no RSA_OAEP_256). — PENDING
  5. Client JWKS usage via shared cache for private_key_jwt. — PENDING
- Client
  1. Nonce/state base64url normalization. — PENDING
  2. PAR diagnostic reuse for alg selection. — PENDING
  3. Diagnostics skip behavior fix. — PENDING
  4. Encryption diagnostics (Phase 3). — PENDING
  5. Refresh fallback diagnostic. — PENDING
  6. Backchannel receiver diagnostic. — PENDING
  7. Logging scrub (include encrypted id_token handling). — PENDING
  8. Tests: encryption scenarios + rate limiting. — PENDING
- Shared
  1. Structured logging enrichment (JWKS: cache hit/miss, forced refresh, etag sent/received). — PENDING
  2. Optional PKCE plain enable switch. — PENDING
  3. Add docs for client decryption (no RSA_OAEP_256) and JWKS cache usage. — PENDING
  4. UserInfo encryption (Phase 4, optional). — PENDING
  5. Request object encryption (Phase 4, optional). — PENDING
  6. Performance benchmark for encryption and JWKS cache overhead. — PENDING

Release readiness (updated)
- Discovery matches real capabilities (no unsupported RSA_OAEP_256).
- IdP issues signed id_tokens; encrypts when possible.
- Client decryption implemented; diagnostics still pending.
- JWKS caching implemented with TTL + rotation fallback; and now also leverages HTTP 304 via server ETags.
- No secrets/tokens appear in logs (scrub pending).
- Deterministic alg/enc order implemented and to be verified by diagnostics.

Status TL;DR
- JWE for id_token implemented; RSA_OAEP_256 removed.
- JWKS caching service uses TTL, kid-miss force-refresh and server ETags for conditional GET; next: diagnostics + tests + IdP private_key_jwt JWKS integration.


