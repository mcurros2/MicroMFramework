# OIDC Implementation Plan (Status & Next Tasks)

Reference  
See “MicroM OIDC Multi-Tenant Flow (PAR + PKCE, SSO/SLO)” (core/Documentation/MicroM OIDC multi tenant flow.md).

A — Core features and persistence
- Discovery/JWKS/ETag: COMPLETE
- PAR + PKCE + Authorization Code: COMPLETE
- Pairwise sub derivation (per client pepper): COMPLETE
- IdP-side client session persistence: COMPLETE
  - `/oauth2/token` persists per-client session with sid, sub, IdP refresh token, UTC expiration.
  - Sentinel device_id `oidc` for subject-wide refresh.
- Client-side session persistence on callback: COMPLETE
- Encrypted refresh-token lookup: COMPLETE
- Audience (aud = client_id): COMPLETE
- UTC handling for IdP refresh expiration: COMPLETE
- Client refresh by SID: COMPLETE

B — IdP grants
- authorization_code: COMPLETE
- refresh_token: COMPLETE

C — Client (RP) flows
- Login flow (PAR → authorize → token → callback): COMPLETE
- Local per-device refresh: COMPLETE
- IdP refresh support: COMPLETE
- Refresh fallback diagnostic: COMPLETE

D — Logout (SLO)
- Front-channel logout: COMPLETE
- Back-channel logout receiver: COMPLETE
- EndSession fan-out with deterministic sid inclusion: COMPLETE

E — Security & hardening
- State/nonce, PKCE, client authentication: COMPLETE
- PKCE ‘plain’ gated by `OIDCAllowPkcePlain`: COMPLETE
- Encrypted IdP refresh tokens at rest: COMPLETE
- Logging scrub (tokens, assertions, logout_token metadata only): COMPLETE

F — Encrypted Tokens
- Signed id_token always; optional JWE when client key supports: COMPLETE
- Client decryption path: COMPLETE
- Encryption negotiation diagnostics: PARTIAL

Phases
1. IdP JWE wrapping: COMPLETE
2. Client decryption path: COMPLETE
3. Diagnostics (negotiation specifics): PARTIAL
4. UserInfo / request object encryption: IN PROGRESS
   - Request object: PARTIAL (signed, basic alg enforcement; encryption supported only with RSA_OAEP; request_uri flow integrated; ECDH disabled)
   - UserInfo encryption: PENDING
5. Policy flags removed (capability-driven): COMPLETE

Updated cryptographic policy (Refactor: WellKnownProvider & JwksProvider)
- ECDH key agreement: REMOVED (not supported for now).
- RSA key encryption algorithms advertised: RSA-OAEP ONLY (RSA1_5 disabled).
- Content encryption algorithms reduced to GCM only (A256GCM, A192GCM, A128GCM) — CBC-HS* disabled.
- Request object signing / client assertion acceptance: RS*, PS*, ES* independent of IdP cert.
- IdP signing algorithms (id_token/userinfo) depend on IdP cert key type (RSA vs EC). Current: RSA only.
- `request_uri_parameter_supported`: FALSE (PAR mandatory).
- ECDH_ES / ECDH_ES+A256KW removed from metadata.
- RSA_OAEP_256 excluded (unchanged rationale: interop + marginal benefit).
- UserInfo encryption not yet advertised (pending implementation).

Key selection rules (updated)
- ID Token signing: Based on IdP cert (RSA: RS/PS; EC: ES).
- ID Token encryption (to client): RSA-OAEP only (current policy).
- Request Object encryption (to IdP): RSA-OAEP only (ECDH future enhancement gate).
- Content encryption preference: A256GCM > A192GCM > A128GCM.

G — JWKS caching & conditional fetches
- Shared JWKS cache + ETag reuse: COMPLETE
  - Integrated `IJWKSFetchCacheService` in OIDCClientService (authorization, refresh, backchannel logout).
  - Conditional GET with If-None-Match; 304 reuse implemented.
  - Kid-miss forced refresh logic in id_token validation path.
  - Unified protected header parsing for JWS (3-part) and JWE (5-part) to expose `kid` and enforce signing algorithms (reject HS*, none).
  - Metrics emitted: hit/miss/forced_refresh/not_modified, key count, ETag transitions.
  - Legacy direct fetch path deprecated (still present but unused by client flows).

Recent changes (UPDATED)
- WellKnownProvider refactor (split IdP signing vs accepted client signing lists).
- Removal of ECDH and CBC-HS* from metadata.
- Disabled RSA1_5 advertisement.
- Unified client signing capability list (RS*/PS*/ES*).
- `request_uri_parameter_supported` set to false (PAR mandated).
- Centralized request object alg validation in PushedAuthorizationProvider.
- JWKS cache integration (cache-first id_token & logout validation).
- Added JWS/JWE protected header parsing + signing alg enforcement.
- Kid-based forced refresh on JWKS cache miss implemented.

Diagnostics conventions
- No raw tokens/assertions/logout_token.
- State/nonce metadata only.
- JWKS diagnostics expose ServerETag / NotModified / SentIfNoneMatch.
- Signing/encryption errors return structured codes (unsupported_encryption_alg, unsupported_signing_alg, kid-miss forced refresh path).

Pending tasks
- UserInfo endpoint (signed + optional encrypted) + advertise userinfo_encryption_*.
- Request object FULL encryption (evaluate ECDH enablement or retain RSA-only; implement JWE decrypt path).
- Deeper encryption negotiation diagnostics (candidate ordering / chosen / reason codes).
- Performance counters (sign/encrypt/decrypt timings, payload sizes, JWKS cache latency benefits).
- Revocation & introspection endpoints.
- Request object policy enhancements (nonce requirement rules, restricted claims set).
- Documentation: updated crypto policy (ECDH removed, CBC-HS removed, RSA1_5 exclusion, signing alg enforcement).
- Test suite: negative alg cases (ECDH attempt, RSA1_5, HS*, none; kid-miss refresh behavior).
- UserInfo scope-based claim filtering & optional JWE.
- Structured selection logging for id_token vs request object vs userinfo (negotiation trace).

Release readiness
- Core flows and JWKS caching COMPLETE.
- Blocking items for Phase 4 completion: UserInfo, extended diagnostics, revocation/introspection, documentation, tests, performance telemetry.

Status TL;DR
- Foundations COMPLETE.
- JWKS caching & ETag reuse now COMPLETE (cache-first, forced refresh, metrics).
- Outstanding: UserInfo encryption, request object encryption extension, diagnostics depth, perf counters, revocation/introspection, docs, tests.

H — Signing & encryption alignment
- IdP signing based on cert: COMPLETE
- client_assertion & request object signing enforcement (asymmetric only): COMPLETE
- id_token JWS signing alg enforcement (no HS*/none): COMPLETE
- UserInfo encryption/signing metadata: PENDING

Next focus (adjusted)
1. Implement UserInfo (signed + optional RSA-OAEP/GCM encryption) & advertise metadata.
2. Request object encryption/decryption completion (decide on ECDH introduction; implement JWE decrypt path).
3. Encryption negotiation diagnostics (candidate sets, selection reasons, exclusion flags).
4. Performance telemetry (crypto timings, payload size deltas, JWKS cache hit ratio, forced refresh counts).
5. Revocation & introspection endpoints.
6. Documentation update (crypto reductions, signing alg enforcement, kid-miss forced refresh behavior).
7. Negative/interop test suite (alg rejection, kid-miss refresh scenarios, request object invalid cases).
8. Decision gate for future ECDH enablement.

Acceptance criteria for updated policy
- Well-known reflects reduced algorithm surface (no ECDH, no CBC-HS*, no RSA1_5, no HS*/none in signing sets).
- UserInfo implemented & advertised when complete.
- Diagnostics show candidate encryption/signing sets, exclusions (e.g. ECDH disabled), selection reason codes.
- JWKS cache metrics accessible (hit/miss/not_modified/forced_refresh/key_count).
- Tests assert rejection of ECDH, CBC-HS*, RSA1_5, HS*, none; verify kid-miss triggers forced refresh exactly once.


