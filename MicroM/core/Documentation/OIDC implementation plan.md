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
4. Request object encryption: COMPLETE (Request Object JWE decrypt path implemented)
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
- Fixed well-known `request_parameter_supported` vs runtime mismatch (PAR-only enforcement).
- Implemented Request Object JWE decrypt path (JwksProvider.DecryptRequestObjectAsync + AuthorizeEndpointProvider usage).

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
- Confirm RSA-only stance for request object key management (document rationale; consider ECDH future-proofing).
- Deeper encryption negotiation diagnostics (candidate ordering, chosen, reason codes, exclusion logging).
- Performance counters only in diagnostics (sign/encrypt/decrypt timings, payload sizes, JWKS cache latency benefit, forced refresh frequency).
- Request object policy enhancements (nonce enforcement when openid scope present, restricted claims, size already enforced).
- Documentation: crypto reductions, signing alg enforcement, kid-miss logic, explicit non-support for UserInfo / introspection / revocation.
- Structured selection logging for id_token vs request object (negotiation trace records).
- Key rotation handling for encrypted IdP refresh tokens (re-encryption or multi-key decryption strategy).

Release readiness
- Core flows, JWKS caching, logout, encryption policies: COMPLETE.
- Blocking for next milestone: enhanced diagnostics, performance telemetry, documentation, tests (tests deferred until diagnostics complete).

Status TL;DR
- Foundations COMPLETE, JWKS caching COMPLETE.
- Non-mandatory endpoints (UserInfo, introspection, revocation) explicitly OUT OF SCOPE.
- Remaining focus: diagnostics depth, performance metrics, documentation & tests (tests deferred).

H — Signing & encryption alignment
- IdP signing based on cert: COMPLETE
- client_assertion & request object signing enforcement (asymmetric only): COMPLETE
- id_token JWS signing alg enforcement (no HS*/none): COMPLETE
- UserInfo encryption/signing metadata: NOT APPLICABLE (endpoint out of scope)

Next focus (adjusted)
1. Confirm RSA-only stance and finalize request object negotiation diagnostics (trace records, exclusion reasons, candidate ordering).
2. Encryption negotiation diagnostics (candidate set + selection + exclusion reasons).
3. Performance telemetry only in diagnostics (crypto timings, payload size deltas, JWKS cache metrics).
4. Documentation update (policy reductions, out-of-scope endpoints, alg enforcement, kid refresh flow).
5. Negative/interop test suite (alg rejection, request object invalid cases, forced refresh scenarios) — DEFERRED until diagnostics complete; add minimal smoke tests before release.

Acceptance criteria (updated)
- Well-known does not advertise UserInfo / introspection / revocation as supported (or includes clear documentation note if retained).
- Request object supports RSA-OAEP encrypted JWE (decrypt & validate) and enforced signing algorithms. (IMPLEMENTED)
- Diagnostics produce structured negotiation records: {context, candidates, selected, exclusions, reasonCodes}.
- Performance counters recorded and queryable (encryption timings, payload sizes, jwks cache hit ratios).
- Tests assert rejection of ECDH, CBC-HS*, RSA1_5, HS*, none; verify kid-miss triggers single forced refresh — TESTS DEFERRED UNTIL DIAGNOSTICS COMPLETE.
- Documentation explicitly lists out-of-scope endpoints and rationale.

---

## Urgent Bug Fixes (TOP priority) (updated)

The items below are immediate, blocking fixes discovered during the recent IdP / OIDC refactor. Statuses reflect recent refactor work. Tests remain deferred until diagnostics and documentation are completed.

1. COMPLETED: Enforce PAR at `/oauth2/authorize` when `require_pushed_authorization_requests = true`. Reject non‑PAR authorize calls that do not include `request_uri`.  
   - Implemented in: `AuthorizeEndpointProvider.ValidateAndOverrideWithPARAuthorizationRequest` (early `invalid_request` if `request_uri` missing).  
   - Rationale: Align runtime with metadata (`request_uri_parameter_supported = false`).

2. COMPLETED: PKCE “plain” verification bug. When `OIDCAllowPkcePlain` is enabled, `code_challenge_method = plain` validates `code_verifier == code_challenge`.  
   - Fixed in: `MemoryAuthorizationCodeService.ValidateAndConsumeAuthorizationCode` (added `plain` branch alongside `S256`).

3. COMPLETED: `private_key_jwt` audience mismatch on non‑default ports. Token endpoint audience now includes scheme, host and port.  
   - Fixed in: `IdPBackchannelAuthenticationHandler.AuthenticatePrivateKeyJwtAsync` using `Request.Scheme`, `Request.Host.Value`, `Request.PathBase`, `Request.Path`.

4. COMPLETED: Request Object semantic validations + post-decrypt encoded payload length cap (64KiB) mitigating JWE inflation.
5. COMPLETED: Client assertion replay protection for `private_key_jwt`.  
   - Implemented short‑lived replay cache via `IOIDCReplayCacheService` keyed by `jti`; enforced single‑use in `IdPBackchannelAuthenticationHandler`.

6. COMPLETED: EndSession fan‑out delivers `logout_token` even when no server‑side `sid` is found.  
   - Implemented in: `IdentityProviderService.HandleEndSession` — always includes `sub`; conditionally includes `sid` if found; purges by `sub`.

7. COMPLETED: Authorization response mix‑up mitigation. Validate `authorization_response_iss` in client callback when present.  
   - Implemented in: `OIDCClientService.HandleAuthorizationCallback` — verify `iss` param equals discovered IdP `issuer`.

8. COMPLETED: Defensive limits:
   - PAR `request` parameter length capped (64KiB encoded) in `PushedAuthorizationProvider`.
   - Post-decrypt Request Object payload cap applied (AuthorizeEndpointProvider).
   - client_assertion encoded length capped (32KiB) pre-parse and rechecked in `IdPBackchannelAuthenticationHandler`.

9. COMPLETED: Make the IdP `/oauth2/authorize` endpoint accessible to browser user-agents (`AllowAnonymous`).

10. COMPLETED: Restrict client encryption algorithms to RSA-OAEP only and content encryption to GCM-only (`A256GCM`, `A192GCM`, `A128GCM`).

11. COMPLETED: Enforce token endpoint auth methods advertised by IdP metadata in backchannel authentication handler (reject `client_secret_basic` when metadata requires `private_key_jwt`).

---

## Implementation reference

Primary files implementing the OIDC functionality:

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

Diagnostics
- `core/Web/Authentication/OIDCDiagnostics/ClientAuthorizeMetadataCheck.cs` — client-side diagnostic check that validates IdP discovery/authorization metadata
- `core/Web/Authentication/OIDCDiagnostics/ClientEncryptionMetadataCheck.cs` — client-side diagnostic check that validates IdP Encryption metadata
- `core/Web/Authentication/OIDCDiagnostics/ClientEndSessionEndpointCheck.cs` — client-side diagnostic check that validates IdP End session endpoint
- `core/Web/Authentication/OIDCDiagnostics/ClientIdpRefreshCheck.cs` — client-side diagnostic check that validates IdP Token endpoint
- `core/Web/Authentication/OIDCDiagnostics/ClientIntrospectionEndpointCheck.cs` — client-side diagnostic check that validates IdP Introspection endpoint
- `core/Web/Authentication/OIDCDiagnostics/ClientJwksCacheEffectivenessCheck.cs` — client-side diagnostic check Jwks cache effectiveness
- `core/Web/Authentication/OIDCDiagnostics/ClientPARCheck.cs` — client-side diagnostic check that validates IdP PAR endpoint
- `core/Web/Authentication/OIDCDiagnostics/ClientRefreshFallbackCheck.cs` — client-side diagnostic check that validates refresh fallback behavior
- `core/Web/Authentication/OIDCDiagnostics/ClientRevocationEndpointCheck.cs` — client-side diagnostic check that validates IdP Revocation endpoint
- `core/Web/Authentication/OIDCDiagnostics/ClientUserInfoEndpointCheck.cs` — client-side diagnostic check that validates IdP Userinfo endpoint
- `core/Web/Authentication/OIDCDiagnostics/ClientWellKnownAndJwksCheck.cs` — client-side diagnostic check that validates IdP Wellknow and Jwks endpoints

- `core/Web/Authentication/OIDCDiagnostics/IdpClientBackchannelEndpointCheck.cs` — IdP-side diagnostic check that validates configured OIDC Clients Backchannel endpoints
- `core/Web/Authentication/OIDCDiagnostics/IdpClientFrontchannelEndpointCheck.cs` — IdP-side diagnostic check that validates configured OIDC Clients Frontchannel endpoints
- `core/Web/Authentication/OIDCDiagnostics/IdpClientJwksCheck.cs` — IdP-side diagnostic check that validates configured OIDC Clients Jwks endpoints
- `core/Web/Authentication/OIDCDiagnostics/IdpClientRedirectUrisCheck.cs` — IdP-side diagnostic check that validates configured OIDC Clients Redirect URIs
- `core/Web/Authentication/OIDCDiagnostics/IdpClientRegistrationSanityCheck.cs` — IdP-side diagnostic check that validates configured OIDC Clients registration
- `core/Web/Authentication/OIDCDiagnostics/IdpConfigurationCheck.cs` — IdP-side diagnostic check that validates OIDC Identity Provider configuration
- `core/Web/Authentication/OIDCDiagnostics/IdpFrontChannelLogoutConfigCheck.cs` — IdP-side diagnostic check that validates OIDC Identity Provider Frontchannel logout configuration

