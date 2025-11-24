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

## Urgent Bug Fixes (TOP priority)

The items below are immediate, blocking fixes discovered during the recent IdP / OIDC refactor. All entries below are PENDING until implemented and validated.

1. COMPLETED: Make the IdP `/oauth2/authorize` endpoint accessible to browser user-agents (remove requirement for IdP client backchannel auth; change controller attribute to `AllowAnonymous`).
2. COMPLETED: Restrict client encryption algorithms to RSA-OAEP only and content encryption to GCM-only (`A256GCM`, `A192GCM`, `A128GCM`). Remove RSA1_5 and CBC-HS* usages from IdP encrypting credentials builders and any JWE generation paths.  
   - Verified in: `core/Web/Authentication/IdentityProvider/IdPClientEncryptingCredentialsCacheService/IdPClientEncryptingCredentialsCacheService.cs` — `BuildEncryptingCredentialsFromSecurityKey` now uses `SecurityAlgorithms.RsaOAEP` and only GCM content algorithms.
3. PENDING: Enforce token endpoint auth methods advertised by the IdP metadata in the backchannel authentication handler (reject `client_secret_basic` when metadata requires `private_key_jwt`).
4. PENDING: Tighten Request Object validation: require `iss == client_id` when present, enforce appropriate `aud` (authorization endpoint or issuer), and strictly validate `exp` / `nbf`.
5. PENDING: Fix IdP client encrypting credentials cache to use RSA-OAEP and GCM-only content algs; invalidate legacy RSA1_5/CBC-HMAC code paths.
6. PENDING: Ensure JWKS kid-miss logic triggers a single forced refresh and verify cache invalidation semantics across nodes. Add unit test for kid-miss forced refresh behavior.
7. PENDING: Fix audience (`aud`) construction used when validating `client_assertion` / token endpoint to include exact host:port and path base to avoid mismatch on non-default ports.
8. PENDING: Replace any internal OIDC id_token encryption using CBC-HMAC with GCM variants where OIDC policy requires it; audit internal token encryption paths for policy alignment.
9. PENDING: Ensure network and crypto calls accept and propagate `CancellationToken` (avoid using `CancellationToken.None` where caller CT should apply).
10. PENDING: Document and/or implement distributed replay cache for backchannel logout in multi-node deployments (current in-memory replay cache is node-local).
11. PENDING: Implement negative interoperability tests (reject ECDH request object attempts, RSA1_5, CBC-HS*, HS*, `none` signing).
12. PENDING: Implement tests for PAR expiry behavior, request_object oversized rejection, and request_uri matching rules.
13. PENDING: Add tests for `private_key_jwt` validation negative cases (wrong `iss`/`sub`/`aud`, expired assertion, unknown keys) and positive cases with JWKS rotation.
14. PENDING: Add diagnostics telemetry and structured negotiation records: `{context, candidates, selected, exclusions, reasonCodes}` for id_token and request_object negotiation.
15. PENDING: Add performance counters for crypto timings, payload sizes, JWKS cache latency/benefit and forced refresh frequency (diagnostics-only, queryable).
16. PENDING: Update documentation to explicitly call out out-of-scope endpoints (UserInfo, introspection, revocation) and the updated cryptographic policy.
17. PENDING: Audit logging to ensure no raw tokens/assertions/logout_token bodies are logged; confirm scrub convention coverage.
18. PENDING: CI workflow: add unit/integration tests covering PAR → authorize → token → callback, including request object JWE decrypt path and `private_key_jwt` flows.

---

## Implementation reference

Below is a concise reference to the primary files implementing the OIDC functionality described above. Links are repository-relative to help navigate the codebase.

- `core/Web/Authentication/IdentityProvider/ApplicationCertificateCacheService/ApplicationCertificateCacheService.cs` — certificate caching for IdP signing/encryption keys.
- `core/Web/Authentication/IdentityProvider/AuthorizationCodeService/MemoryAuthorizationCodeService.cs` — authorization code lifecycle (memory implementation).
- `core/Web/Authentication/IdentityProvider/AuthorizeEndpointProvider/AuthorizeEndpointProvider.cs` — authorize endpoint handling, request validation and PAR integration.
- `core/Web/Authentication/IdentityProvider/IdentityProviderService/IdentityProviderService.cs` — central IdP service orchestration and flows.
- `core/Web/Authentication/IdentityProvider/IdPBackchannelAuthenticationHandler/IdPBackchannelAuthenticationHandler.cs` — back-channel authentication and logout receiver logic.
- `core/Web/Authentication/IdentityProvider/IdPClientEncryptingCredentialsCacheService/IdPClientEncryptingCredentialsCacheService.cs` — cache for client encrypting credentials used for JWE.
- `core/Web/Authentication/IdentityProvider/IdPClientSigningKeysCacheService/IdPClientSigningKeysCacheService.cs` — cache for client signing keys (request objects, client assertions).
- `core/Web/Authentication/IdentityProvider/JwksProvider/JwksProvider.cs` — Well-known and JWKS provider, metadata production.
- `core/Web/Authentication/IdentityProvider/JwksService/JwksService.cs` — JWKS fetch & parsing logic (integration point for fetch cache).
- `core/Web/Authentication/IdentityProvider/OIDCCryptoCapabilities/OIDCCryptoCapabilities.cs` — advertised crypto capabilities and selection rules.
- `core/Web/Authentication/IdentityProvider/OIDCReplayCacheService/OIDCReplayCacheService.cs` — replay protection for request objects / nonces.
- `core/Web/Authentication/IdentityProvider/PushedAuthorizationProvider/PushedAuthorizationProvider.cs` — PAR handling and storage.
- `core/Web/Authentication/IdentityProvider/PushedAuthorizationService/PushedAuthorizationService.cs` — PAR service implementation.
- `core/Web/Authentication/IdentityProvider/StateAndNonceService/StateAndNonceService.cs` — state/nonce persistence and validation.
- `core/Web/Authentication/IdentityProvider/WellKnownProvider/WellKnownProvider.cs` — well-known document generation and policy application.
- `core/Web/Authentication/JWKSFetchCacheService/JWKSFetchCacheService.cs` — shared JWKS fetch cache with ETag / conditional GET and kid-miss logic.
- `core/Web/Authentication/JWTHandling/WebAPIJsonWebTokenHandler.cs` — JWT/JWE parsing, protected header inspection and enforcement.
- `core/Web/Authentication/OIDCClientService/OIDCClientService.cs` — client (RP) integration points and callbacks.
- `core/Web/Authentication/OIDCHttpClient/OIDCHttpClient.cs` — HTTP client wrapper used for IdP interactions.
- `core/Web/Controllers/IdentityProviderController/IdentityProviderController.cs` — API controller surface for IdP endpoints.
- `core/Web/Controllers/OIDCClientController/OIDCClientController.cs` — controller endpoints used by clients (callback, refresh, logout).
- `core/Documentation/MicroM OIDC multi tenant flow.md` — detailed flow diagram and reference for multi-tenant PAR+PKCE flow.




