# OIDC Implementation Plan (Current Status & Next Tasks)

Reference
See “MicroM OIDC Multi-Tenant Flow (PAR + PKCE, SSO/SLO)” (`core/Documentation/MicroM OIDC multi tenant flow.md`).

A — Persistence & Core Services
- Core caches & JWKS: COMPLETE (`EtagCacheService`, `ApplicationCertificateCacheService`, `JwksService`)
- JWKS validation helper: COMPLETE (`JwksProvider.ValidateIdTokenAsync`)
- Authorization code store (PKCE + nonce + redirect_uri + expiry): COMPLETE
- PAR store & forwarding: COMPLETE
- Centralized HttpClients + typed OIDC client: COMPLETE
- Active OIDC session persistence:
  - IdP-side persistence for clients: COMPLETE
    - `OauthTokenService.HandleTokenRequest` ensures `sid`, derives pairwise `sub` with client pepper, sets `aud = client_id` for id_token, issues subject-wide IdP refresh token, and persists via `ApplicationOidcActiveSessions.CreateIdPSession(client_id, user_id, existing_session_id, refresh, exp)`.
  - Client-side persistence on callback: COMPLETE
    - `MicroMAuthenticator.HandleExternalSignIn` + `ApplicationOidcActiveSessions.CreateOrUpdateExternalSignInSession` (per-device) and `usr_updateLoginAttempt` for local refresh.
  - Pairwise `sub`: COMPLETE (`GetDerivedSub(client_app_id, idp_user_id, pepper)`).
  - Note: `CreateIdPSession` must upsert (use `UpdateData`). If any branch still calls `InsertData`, switch to `UpdateData` to avoid PK conflicts.

B — Login / SSO Creation
- IdP local login: COMPLETE (`/auth/login`) sets IdP SSO cookie and (IdP-only) session row when logging into IdP itself.
- Client login via OIDC: COMPLETE (PAR → authorize → token → callback).

C — Authorization Flow
- CLIENT: COMPLETE (PAR + Authorize + PKCE + state/nonce)
- ID_TOKEN audience: COMPLETE (id_token `aud = client_id` via `GenerateJwtTokenWEBApi(..., audience: client_id)`).

D — EndSession / Single Logout (SLO)
- Replay protection: COMPLETE (`OIDCReplayCacheService`)
- Client backchannel logout: COMPLETE
  - Validate logout_token; prefer `sub`, fallback `sid`; remove sessions idempotently.
- Refresh invalidation on SLO:
  - CLIENT: COMPLETE in delete procs (`aos_deleteSessionsBySUB` invalidates refresh).
  - IdP-side: PARTIAL (rows persisted; invalidation wired once endsession/backchannel are completed).
- Front-channel logout (client): PENDING
- IdP `endsession` + backchannel fan-out: PENDING

E — Session Binding & Security
- State/nonce HMAC, TTL, device binding, persisted sid/sub: COMPLETE
- Client device_id is local-only and not propagated to IdP: COMPLETE (kept separate from OIDC refresh which is subject-wide).

F — OIDC Client API Surface
- Client JWKS, login (PAR forwarder), callback: COMPLETE
- Logout endpoints: PENDING (front-channel build/handler)

G — Tokens & Claims
- iss, aud, exp, signature, nonce, sid, azp: COMPLETE
- Pairwise sub per client with pepper: COMPLETE

H — Tests
- PENDING (IdP token refresh, backchannel e2e, replay/idempotency)

I — Operational / Observability
- PENDING (structured logs/metrics, rate limiting)

Recent changes
- IdP token endpoint now:
  - Ensures and embeds `sid` in id_token, sets `aud = client_id`.
  - Derives pairwise `sub` using client-specific pepper from registration.
  - Issues subject-wide IdP refresh token and persists per-client session at IdP via `CreateIdPSession`.
- `CreateIdPSession` accepts nullable `username`, optional `existing_session_id`, refresh token and expiration.
- `vc_username` column is nullable.
- Client callback persists per-device session and calls `usr_updateLoginAttempt` for local refresh handling.
- Client backchannel logout handler hardened.

Known gaps before release
- IdP refresh token grant (grant_type=refresh_token)
- Front-channel logout (client) + IdP endsession/backchannel fan-out
- Test coverage (refresh grant, SLO end-to-end, idempotency)
- Metrics/structured logging and endpoint rate limiting

Risks
- Ensure `CreateIdPSession` is upsert (`UpdateData`), not insert, to handle repeated grants.
- Consider hashing IdP refresh tokens at rest and adding a lookup proc if you plan to query by token.

## Immediate Next Tasks (focus: IdP token refresh)

1) Implement IdP refresh_token grant
- Update `OauthTokenService` to support `grant_type=refresh_token`:
  - Validate client authentication and input (`refresh_token`, optional `scope`).
  - Look up IdP session by (`c_application_id = client_id`, `vc_oidc_refreshtoken = token`).
    - Consider hashing the refresh token and adding a proc to query by hash.
  - Validate expiration and rotate refresh token (issue new token and update `dt_refresh_expiration`).
  - Re-issue access token; id_token optional but recommended; include consistent `sub`, `sid`, `aud = client_id`.
  - Throttle attempts (e.g., simple counter or fixed backoff) to mitigate abuse.

2) Wire refresh invalidation into SLO (IdP side)
- On backchannel fan-out (to be implemented), ensure `vc_oidc_refreshtoken` is nullified/expired for the client rows at IdP.

3) Front-channel logout + endsession
- Client: implement `BuildEndSessionRequest` and `HandleFrontChannelLogout`.
- IdP: implement `/oauth2/endsession` to terminate IdP SSO and post `logout_token` to each registered client (including IdP-as-client if applicable).

4) Tests
- Unit: refresh grant validation/rotation; replay cache behavior.
- Integration: Client PAR→Authorize→Token→Callback→Local session→IdP Refresh→Backchannel Logout (sessions removed on both sides).
- Idempotency: replayed logout_token and repeated refresh token usage.

5) Observability and safeguards
- Add structured logs for token issuance/refresh (app_id, client_id, user_id, sid, sub, jti).
- Add counters for token grants, refresh rotations, SLO events; rate-limit token endpoints.

## Adjusted Task Status
- IdP token issuance (authorization_code): COMPLETE
- Pairwise `sub` + pepper: COMPLETE
- IdP-side client session persistence: COMPLETE
- Client backchannel logout: COMPLETE
- IdP refresh_token grant: PENDING (next)
- Front-channel + IdP endsession: PENDING
- Tests and observability: PENDING

Release gating
- IdP authorization_code + refresh_token grants working with replay protection
- Backchannel logout fan-out and client handling verified
- Automated tests: token validation, refresh rotation, SLO idempotency
- Metrics/logging baseline in place


