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
4. Request object encryption: IN PROGRESS (UserInfo removed from scope)
5. Policy flags removed (capability-driven): COMPLETE

Updated cryptographic policy (Refactor: WellKnownProvider & JwksProvider)
- ECDH key agreement: REMOVED (not supported for now).
- RSA key encryption algorithms advertised: RSA-OAEP ONLY (RSA1_5 disabled).
- Content encryption algorithms reduced to GCM only (A256GCM, A192GCM, A128GCM) — CBC-HS* disabled.
- Request object signing / client assertion acceptance: RS*, PS*, ES* independent of IdP cert.
- IdP signing algorithms (id_token) depend on IdP cert key type (RSA vs EC). Current: RSA only.
- `request_uri_parameter_supported`: FALSE (PAR mandatory).
- ECDH_ES / ECDH_ES+A256KW removed from metadata.
- RSA_OAEP_256 excluded (unchanged rationale: marginal benefit vs complexity).
- UserInfo encryption not advertised (endpoint explicitly OUT OF SCOPE).

Key selection rules (updated)
- ID Token signing: Based on IdP cert (RSA: RS/PS; EC: ES).
- ID Token encryption (to client): RSA-OAEP only.
- Request Object encryption (to IdP): RSA-OAEP only (ECDH deferred).
- Content encryption preference: A256GCM > A192GCM > A128GCM.

G — JWKS caching & conditional fetches
- Shared JWKS cache + ETag reuse: COMPLETE
  - Integrated `IJWKSFetchCacheService` everywhere (authorization, refresh, backchannel logout).
  - Conditional GET (If-None-Match) + 304 reuse.
  - Kid-miss forced refresh logic.
  - Unified header parsing (JWS/JWE) to expose `kid` and enforce signing alg policy.
  - Metrics: hit/miss/forced_refresh/not_modified/key_count/etag transitions.
  - Legacy direct fetch path deprecated.

Recent changes (UPDATED)
- WellKnownProvider refactor: split IdP signing vs accepted client signing lists.
- Removal of ECDH and CBC-HS* from metadata.
- Disabled RSA1_5 advertisement.
- Unified client signing capability list (RS*/PS*/ES*).
- `request_uri_parameter_supported` set to false (PAR mandated).
- Centralized request object alg validation.
- JWKS cache integration & kid-based forced refresh.
- JWS/JWE protected header parsing + signing alg enforcement.

Diagnostics conventions
- No raw tokens/assertions/logout_token.
- State/nonce metadata only.
- JWKS diagnostics expose ServerETag / NotModified / SentIfNoneMatch.
- Structured error codes: unsupported_encryption_alg, unsupported_signing_alg, kid_miss.

Out-of-Scope (Explicit Non-Implementation)
- UserInfo endpoint (not mandatory for core authorization code + id_token flow).
- Introspection endpoint.
- Revocation endpoint.
Rationale: Current clients only require authorization code, id_token issuance, refresh handling, and logout. Excluding non-mandatory endpoints reduces surface area, operational complexity, and cryptographic exposure. Discovery document may be adjusted later to omit these endpoints or retain placeholders with documentation stating non-support.

Pending tasks (adjusted)
- Request object FULL encryption (JWE decrypt path; keep RSA-only or re-evaluate ECDH).
- Deeper encryption negotiation diagnostics (candidate ordering, chosen, reason codes, exclusion logging).
- Performance counters only in diagnostics (sign/encrypt/decrypt timings, payload sizes, JWKS cache latency benefit, forced refresh frequency).
- Request object policy enhancements (nonce enforcement when openid scope present, restricted claims, size already enforced).
- Documentation: crypto reductions, signing alg enforcement, kid-miss logic, explicit non-support for UserInfo / introspection / revocation.
- Test suite: negative alg cases (ECDH attempt, RSA1_5, HS*, none; kid-miss forced refresh; disallowed request object scenarios).
- Structured selection logging for id_token vs request object (negotiation trace records).

Release readiness
- Core flows, JWKS caching, logout, encryption policies: COMPLETE.
- Blocking for next milestone: request object JWE decrypt, enhanced diagnostics, performance telemetry, documentation, tests.

Status TL;DR
- Foundations COMPLETE, JWKS caching COMPLETE.
- Non-mandatory endpoints (UserInfo, introspection, revocation) explicitly OUT OF SCOPE.
- Remaining focus: request object encryption completion, diagnostics depth, performance metrics, documentation & tests.

H — Signing & encryption alignment
- IdP signing based on cert: COMPLETE
- client_assertion & request object signing enforcement (asymmetric only): COMPLETE
- id_token JWS signing alg enforcement (no HS*/none): COMPLETE
- UserInfo encryption/signing metadata: NOT APPLICABLE (endpoint out of scope)

Next focus (adjusted)
1. Request object encryption/decryption completion (JWE decrypt path, confirm RSA-only stance).
2. Encryption negotiation diagnostics (candidate set + selection + exclusion reasons).
3. Performance telemetry only in diagnostics (crypto timings, payload size deltas, JWKS cache metrics).
4. Documentation update (policy reductions, out-of-scope endpoints, alg enforcement, kid refresh flow).
5. Negative/interop test suite (alg rejection, request object invalid cases, forced refresh scenarios).
6. Decision gate for future ECDH enablement (record rationale & criteria).

Acceptance criteria (updated)
- Well-known does not advertise UserInfo / introspection / revocation as supported (or includes clear documentation note if retained).
- Request object supports RSA-OAEP encrypted JWE (decrypt & validate) and enforced signing algorithms.
- Diagnostics produce structured negotiation records: {context, candidates, selected, exclusions, reasonCodes}.
- Performance counters recorded and queryable (encryption timings, payload sizes, jwks cache hit ratios).
- Tests assert rejection of ECDH, CBC-HS*, RSA1_5, HS*, none; verify kid-miss triggers single forced refresh.
- Documentation explicitly lists out-of-scope endpoints and rationale.


