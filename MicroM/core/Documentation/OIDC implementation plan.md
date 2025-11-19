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
- Client refresh fallback wiring: COMPLETE (diagnostic added)

D — Logout (SLO)
- Front-channel logout (client): COMPLETE
- Back-channel logout receiver (client): COMPLETE
- IdP endsession/backchannel fan-out: COMPLETE

E — Security & hardening
- State/nonce, PKCE, client authentication: COMPLETE
  - Runtime state cookie is HMAC’ed, single-use.
  - PKCE enforcement accepts `S256` and `plain` per discovery; acceptance tracks discovery-advertised methods.
- Encrypted OIDC refresh tokens at rest: COMPLETE
- Logging: PARTIAL (scrub infrastructure implemented; remaining: deeper encryption/JWKS path audit)
- Rate limiting: COMPLETE
- Metrics/counters: Using ASP.NET Core built-in meters.

F — Encrypted Tokens (scope)
Goal: Optional JWE encryption of id_token (and later userinfo / request objects) using asymmetric crypto (no shared-secret OIDC encryption). `app.JWTKey` only for local API JWT.
Status:
- Discovery advertises asymmetric-only alg/enc: COMPLETE
- Removed unsupported RSA_OAEP_256: COMPLETE
- EC preference: ECDH_ES_A256KW > ECDH_ES: COMPLETE
- RSA preference: RSA_OAEP > RSA1_5: COMPLETE
- Subject types `[public, pairwise]`: COMPLETE
- IdP id_token issuance (always signed; encrypts when supported): COMPLETE
- Client decrypt path: COMPLETE

Planned phases (retain all)
1. IdP JWE wrapping: COMPLETE
2. Client decryption path: COMPLETE
3. Diagnostics (encryption negotiation & absence of RSA_OAEP_256): PARTIAL
4. UserInfo / request object encryption (optional): PENDING
5. Policy flags removed (capability-driven): COMPLETE

Key selection rules (implemented)
- Key Encryption alg: RSA-OAEP > RSA1_5; ECDH-ES+A256KW > ECDH-ES; exclude RSA_OAEP_256 & all symmetric (dir/AxxxKW/PBES2-*).
- Content Encryption enc: A256GCM > A256CBC-HS512 > A192GCM > A192CBC-HS384 > A128GCM > A128CBC-HS256.

G — JWKS caching & conditional fetches
- Shared JWKS cache + TTL + kid-miss forced refresh: COMPLETE
- ETag conditional GET + 304 reuse: COMPLETE
- IdP private_key_jwt validation uses shared JWKS cache: COMPLETE

Recent changes (UPDATED)
- private_key_jwt alg selection parity: COMPLETE
- Asymmetric-only encryption advertisement: COMPLETE
- RSA_OAEP_256 removed: COMPLETE
- Encryption alg/enc iteration logic: COMPLETE
- Client decryption path: COMPLETE
- JWKS shared cache (TTL + rotation fallback): COMPLETE
- ETag service TTL: COMPLETE
- Conditional GET (well-known/JWKS) + size guards: COMPLETE
- JWKS conditional reuse on 304: COMPLETE
- Backchannel handler JWKS cache usage: COMPLETE
- Client encryption metadata diagnostic: COMPLETE
- Refresh diagnostic (JWS/JWE alg/enc reporting): COMPLETE
- JWKS cache effectiveness diagnostic: COMPLETE
- Unified SKIPPED behavior (authorize, end_session, userinfo, revocation, introspection, PAR): COMPLETE
- PAR diagnostic state/nonce base64url format reporting: COMPLETE
- Refresh fallback diagnostic: COMPLETE
- IdP client JWKS structured markers (incl. revalidation + timing): COMPLETE
- IdP client front/back-channel endpoint structured markers + timing: COMPLETE
- Diagnostics body scrubbing implemented (ScrubForDiagnostics) across client & IdP endpoint checks and PAR; raw canonical well-known/JWKS preserved: COMPLETE
- Removed logging of raw code_verifier and sensitive token artifacts in diagnostics: COMPLETE

Diagnostics conventions
- Client diagnostics probe IdP endpoints; IdP diagnostics probe registered client endpoints.
- Well-known/JWKS check: Result[0] raw discovery JSON; Result[1] raw JWKS JSON; summaries appended ≥ index 2.
- No logging of raw tokens, refresh tokens, client_assertion, logout_token, full JWKS body outside canonical slot.
- Absent endpoints → “SKIPPED: not advertised”.
- State/nonce values reported only by format/length (base64url).
- Structured markers (client_id, urls, status, duration_ms, counts) standard for IdP-side client endpoint checks.

Next tasks — Phase 3 (Diagnostics / UX)
- Client-side diagnostics: Backchannel receiver probe NOT APPLICABLE
- IdP-side diagnostics: Structured markers completed; only timing refinement optional (NOT APPLICABLE now)
- Shared:
  - Logging scrub (complete pass on encryption & JWKS internal handlers): PENDING
  - Structured JWKS UI markers (cache hit/miss, forced refresh, ETag sent/received): PENDING
  - UI help text for “Signed vs Signed+Encrypted”: PENDING

Pending tasks (retain all)
- IdP
  1. PKCE policy alignment. PENDING
  2. EndSession: include `sid` claim when available. PENDING
  3. Logging scrub (encryption/JWKS deeper paths). PENDING
  4. Optional tests for alg ordering + encryption. PENDING
  5. Client endpoint diagnostics structured markers: COMPLETE
- Client
  1. Logging scrub (encrypted id_token handling). PENDING
  2. Backchannel receiver diagnostic (client suite) NOT APPLICABLE
- Shared
  1. Structured JWKS logging enrichment (hit/miss, forced refresh, ETag events). PENDING
  2. Optional PKCE plain enable switch. PENDING
  3. Docs: client decryption (no RSA_OAEP_256) & JWKS cache usage. PENDING
  4. UserInfo encryption (Phase 4). PENDING
  5. Request object encryption (Phase 4). PENDING
  6. Performance benchmark (encryption & JWKS cache overhead). PENDING

Release readiness (updated)
- Discovery matches capabilities (no unsupported RSA_OAEP_256).
- IdP issues signed id_tokens; encrypts when possible.
- Diagnostics confirm signing/encryption usage & endpoint reachability.
- JWKS caching (TTL + rotation + 304) validated.
- Deterministic alg/enc order implemented.
- Scrub layer added; final logging audit pending.

Status TL;DR
- Core & encryption flows complete.
- Diagnostics comprehensive (structured markers + scrub).
- Remaining focus: finalize logging scrub, JWKS UI markers, documentation, optional encryption extensions.

H — Signing & encryption end-to-end alignment
- IdP key selection (RSA/ECDH) & client decryption: COMPLETE
- Discovery accuracy & client_assertion alg enforcement: COMPLETE

Actions (IdP)
- Encrypt id_token with client’s public key; prefer RSA-OAEP or ECDH-ES+A256KW; enc=A256GCM. COMPLETE

Actions (Client)
- Decrypt JWE id_token enforcing allowed alg/enc sets. COMPLETE

Actions (Shared/JWKS)
- JWKS correctness (kty, crv/x/y, kid, x5c) validated. COMPLETE


