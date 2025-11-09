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
  - private_key_jwt alg selection (runtime): COMPLETE (dynamic preference based on discovery + key type).
  - Audience for assertions: PAR → PAR endpoint; Token/Refresh → token endpoint.
  - client_assertion sent in form for private_key_jwt (no Authorization header).
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
- Logging: PARTIAL (further scrub pending)
- Rate limiting: COMPLETE (global per-node limiter optional/pending)
- Metrics/counters: Using ASP.NET Core built-in meters.

Recent changes
- private_key_jwt alg selection enforced from discovery across PAR, token and refresh — COMPLETE (runtime).
- Diagnostics model refactor — COMPLETE.
- Client diagnostics (PAR, authorize metadata, token, optional endpoints) — COMPLETE.
- IdP diagnostics (configuration + client endpoints) — COMPLETE.

New plan updates
- Do NOT force PKCE S256-only; allow any method explicitly advertised in discovery. Discovery will remain S256-only unless configuration adds `plain`.
- Expand advertised signing algorithm lists to reflect actual certificate capabilities and selection logic (include ES256/384/512, PS256/384/512, RS256/384/512 as applicable).
- Advertise `subject_types_supported` including `pairwise` (runtime uses pairwise derivation).
- Align diagnostics “not advertised” outcomes to return “skipped” without errors.
- Align Client PAR diagnostic alg selection with runtime logic (reuse `GetClientAuthorizationHeader`).
- Ensure nonce generation uses base64url everywhere (runtime + diagnostics).
- Optional inclusion of `sid` in `logout_token` when available.
- Fallback to configuration key when `ClientAPPID` missing during end-session fan-out.
- Clean up unused `device_authorization_endpoint` mapping (`idpRefreshURL`) or repurpose for future device flow.
- Strengthen logging scrub (no secrets/tokens).

Diagnostics conventions (no change except skip behavior):
- Any failed assertion is an error.
- Optional endpoints: if not advertised → Result = “not advertised; skipped” (IsSuccess = true, no Errors).
- Use `Base64UrlEncoder` (or equivalent) for state/nonce/PKCE (update nonce runtime).
- Use `WellknownIdentityConstants` for all parameter names.

Status (completed)
- IdP diagnostics: COMPLETE
- OIDC Client diagnostics: COMPLETE (pending improvements listed below)

Pending tasks (NEW + existing)
- Implement remaining client diagnostics:
  - `TestCallbackEndpointAsync`
  - `TestRefreshFallbackAsync`
  - `TestBackchannelReceiverAsync`

Known gaps before release
- Comprehensive tests for SLO, refresh fallback, and rate-limit behaviors.
- Final log scrub for secrets/tokens.
- Optional global per-node limiter.

Priority next tasks (ordered, atomic; separated by IdP vs Client)

IdP tasks
1. Discovery: Add `"pairwise"` to `subject_types_supported`.
2. Discovery: Expand `token_endpoint_auth_signing_alg_values_supported`, `revocation_endpoint_auth_signing_alg_values_supported`, `introspection_endpoint_auth_signing_alg_values_supported`, and `id_token_signing_alg_values_supported` to include all algorithms supported by the certificate & policy (ES256/384/512, RS256/384/512, PS256/384/512). Deduplicate / order preferred first.
3. PKCE policy: Reflect allowed methods in discovery only (currently S256). If configuration enables `plain`, add it; otherwise reject `plain` at validation.
4. `PushedAuthorizationProvider.SelectAssertionAlg`: Add support for PS384 (present but not yet explicitly preferred), ensure deterministic preference sequence across RSA/ECDSA. Document algorithm preference.
5. `ValidateRequest` / `ValidateSignInForm`: Restrict accepted `code_challenge_method` to only those advertised; if discovery does not list `plain`, return error for `plain`.
6. EndSession: Add `sid` claim when available; implement fallback for empty `ClientAPPID` using dictionary key.
7. Logging scrub: Redact tokens (id_token, refresh_token, logout_token, client_assertion) and Authorization headers in all IdP paths.
8. Tests: Add MSTest suite for modified discovery (subject_types, alg lists), and end-session fan-out with `sid`.

Client tasks
1. State/Nonce: Change nonce generation to base64url-safe method (align with diagnostics). Ensure both state and nonce use the same helper (update `StateAndNonceService`).
2. PAR diagnostic (`ClientPARCheck`): Reuse `GetClientAuthorizationHeader` to select algorithm based on discovery + cert. Include selected alg in diagnostic Result.
3. Diagnostics skip behavior: Update all client diagnostics to return success “not advertised; skipped” instead of error when endpoint missing (EndSession/UserInfo/Revocation/Introspection/PAR if not required).
4. Clean up `ClientDiagnosticsContext.idpRefreshURL`: Remove or rename (keep for future device flow). Ensure unused value not misleading in results.
5. Callback diagnostic (`TestCallbackEndpointAsync`): Implement by simulating token exchange with a mock/invalid code to verify error semantics + state cookie path.
6. Refresh fallback diagnostic (`TestRefreshFallbackAsync`): Simulate local refresh failure then IdP refresh success/failure paths.
7. Backchannel receiver diagnostic (`TestBackchannelReceiverAsync`): Issue a signed synthetic logout_token (valid + replay + expired) and validate client responses.
8. Nonce validation test: Add test confirming base64url nonce flows through id_token and fails when mismatched.
9. Logging scrub: Redact token values, assertions, refresh tokens, logout_token in client logs.
10. Tests: Rate limiter scenarios (authorize, front-logout, backchannel posts, refresh fallback).

Shared tasks
1. Update documentation (this file) with new tasks and policy (DONE).
2. Expand unit/integration tests for algorithm selection across PAR, token, refresh (client + IdP).
3. Introduce structured logging enrichment for diagnostic results (correlate app_id + diagnostic_id).
4. Optional: Introduce configuration switch to enable `plain` PKCE (default off) controlling both discovery advertisement and validation logic.
5. Optional: Implement global per-node rate limiter if post-observation indicates need.

Release readiness criteria (updated)
- All discovery fields accurately reflect runtime capabilities (subject types + algs + PKCE methods).
- Optional endpoints produce “skipped” diagnostics with zero errors when not advertised.
- Nonce/state both base64url and validated through callback/id_token.
- Logging scrub passes manual inspection (no secrets or token values).
- Algorithm selection deterministic and consistent between diagnostics and runtime.
- Test coverage: SLO fan-out + backchannel variations + refresh fallback + rate limiting partitions.

Status TL;DR
- IdP: Stable; pending advertisement refinements (pairwise + expanded alg lists), PKCE policy alignment, end-session robustness, logging scrub.
- Client: Core flows stable; diagnostics need skip behavior correction, improved PAR alg selection parity, additional callback/refresh/backchannel tests, nonce normalization.
- Rate limiting: Implemented; targeted tests pending.


