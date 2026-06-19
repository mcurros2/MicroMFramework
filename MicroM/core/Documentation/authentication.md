### OIDC, FAPI, PKCE, SSO & SLO Endpoints Table

| Endpoint | Purpose | Implementation | Channel | Data Sent | Data Returned | Changes / Notes (FAPI, PKCE, etc.) |
| :--- | :--- | :--- | :--- | :--- | :--- | :--- |
| **`.well-known/openid-configuration`** | Provider discovery metadata. | **IdP** | **Public HTTP** | — | JSON with endpoints (`jwks_uri`, `authorization_endpoint`, `token_endpoint`, `pushed_authorization_request_endpoint`).<br><br>Also lists supported features (PKCE, PAR, JAR, DPoP). | Source of truth for configuration. |
| **`jwks_uri`** | IdP’s JWKS for verifying token signatures. | **IdP** | **Public HTTP** | — | `{"keys":[...]}` (IdP public keys). | Critical for verifying IdP JWTs.<br><br>**Not** your client’s JWKS (that’s for `private_key_jwt`). |
| **`pushed_authorization_request_endpoint` (PAR)** | SPA -> API Backend -> IdP<br><br>Send the authorization request over a secure back-channel. | **IdP** | **Back-channel (private)** | 302 <- API Backend <- IdP<br><br>From client backend: same params as `authorize` (in body).<br><br>**Client authentication:** your case `client_secret_basic`. | JSON with `request_uri`, `expires_in`. | Recommended/required in **FAPI**.<br><br>You’re already using this. |
| **`authorization_endpoint`** | Starts user authentication & consent (basis for **SSO**). | **IdP** | **Front-channel (public)** | From browser: `client_id`, `redirect_uri`, `scope`, `response_type=code`, `state`.<br><br>With **PAR**: usually only `client_id` + `request_uri`. | Redirect to client with `code` and `state`.<br><br>Or POST if using `response_mode=form_post`. | **PKCE:** send `code_challenge` + `code_challenge_method=S256`.<br><br>**JAR:** signed JWT request (`request` / `request_uri`).<br><br>**PAR:** request was pushed; here only `request_uri`. |
| **`token_endpoint`** | Exchange `authorization_code` for `id_token` / `access_token` and (optionally) `refresh_token`.<br><br>Also used for refresh. | **IdP** | **Back-channel (private)** | From client backend: `grant_type`, `code`, `redirect_uri`, `code_verifier` (PKCE).<br><br>**Client authentication:** e.g., `client_secret_basic`. | JSON with `id_token`, `access_token`, `refresh_token`, expirations, scopes. | **PKCE:** `code_verifier` required.<br><br>**FAPI:** stronger client auth (`private_key_jwt` or mTLS).<br><br>Sender-constrained tokens (mTLS/DPoP) in stricter profiles.<br><br>Your current mode: `client_secret_basic`. |
| **`redirect_uri` (callback)** | Receive `code` and `state` after login. | **API Backend (your backend/BFF)** | **Front-channel (public)** | — | Incoming redirect (or POST) from IdP carrying `code` and `state`. | Register **your backend** as the callback (not the SPA).<br><br>Backend then calls `/token` and manages local session. |
| **`end_session_endpoint`** | **IdP SPA-Initiated Logout** at IdP (SLO). | **IdP** | **Front-channel (public)** | From browser: `id_token_hint`, `post_logout_redirect_uri`, `state`. | Redirect to SPA post-logout URL. | Use alongside invalidating your **local session** in the app. |
| **`backchannel_logout_uri`** | Logout notification to API Backend over back-channel. | **SPA Client (your app)** | **Back-channel (private)** | Incoming signed **Logout Token (JWT)** from IdP. | `200 OK` (ack). | Defined by Back-Channel Logout.<br><br>Robust SLO without browser dependency. |
| **`post_logout_redirect_uri`** | Logged out Page. | **SPA Client (your app)** | **Front-channel (public)** | None. | `200 OK` (ack). | Logged out UI page. |
| **`revocation_endpoint`** *(optional)* | Revoke `access_token` / `refresh_token`. | **IdP** | **Back-channel (private)** | From backend: `token`, `token_type_hint`.<br><br>**Client authentication** required. | Typically `200 OK` with empty body or minimal JSON. | Useful for remote session termination. |
| **`introspection_endpoint`** *(optional)* | Check status/claims of an **opaque** token. | **IdP** | **Back-channel (private)** | From backend/RS: `token`.<br><br>**Client authentication** required. | JSON with `active`, `sub`, `scope`, etc. | Used when tokens aren’t self-contained JWTs. |
| **`userinfo_endpoint`** | Retrieve user claims. | **IdP** | **Back-channel (private)** | `Authorization: Bearer <access_token>`. | JSON claims (`sub`, `email`, `name`, etc.). | Use **only if** needed claims weren’t in the `id_token`.<br><br>In FAPI, prefer sender-constrained tokens (mTLS/DPoP). |


# OAuth2/OIDC BFF Cheat Sheet — C# (.NET 8, Minimal snippets)

**Mode:** Confidential client · `client_secret_basic` · PAR · PKCE
**SPA:** the browser never handles IdP tokens (only an HttpOnly, Secure, SameSite session cookie)

---

## Namespaces to import (once)

```csharp
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
```

---

## 0) Generate PKCE values (one per auth request)

```csharp
// Helpers
string Base64Url(byte[] bytes) =>
    Convert.ToBase64String(bytes).Replace('+', '-').Replace('/', '_').TrimEnd('=');

// code_verifier: 43–128 chars high-entropy random
byte[] rnd = RandomNumberGenerator.GetBytes(64);
string verifierRaw = Base64Url(rnd);
string codeVerifier = verifierRaw.Length > 128 ? verifierRaw.Substring(0, 128) : verifierRaw;

// code_challenge: BASE64URL(SHA256(code_verifier))
using var sha = SHA256.Create();
byte[] hash = sha.ComputeHash(Encoding.ASCII.GetBytes(codeVerifier));
string codeChallenge = Base64Url(hash);

// Output (persist code_verifier server-side for the upcoming code exchange)
Console.WriteLine($"code_verifier={codeVerifier}");
Console.WriteLine($"code_challenge={codeChallenge}");
```

---

## 1) PAR — Push the authorization request (backend → IdP)

```csharp
// Placeholders
string IDP_BASE = "https://idp.example.com";
string PAR_ENDPOINT = $"{IDP_BASE}/oauth2/par";
string AUTH_ENDPOINT = $"{IDP_BASE}/oauth2/authorize";
string CLIENT_ID = "YOUR_CLIENT_ID";
string CLIENT_SECRET = "YOUR_CLIENT_SECRET";
string REDIRECT_URI = "https://app.example.com/callback";

// Inputs from step 0
string codeChallenge = "<CODE_CHALLENGE_FROM_STEP_0>";

// One-time values per auth request
string state = Guid.NewGuid().ToString("N");
string nonce = Guid.NewGuid().ToString("N");

// Helper
string BasicAuth(string id, string secret)
{
    var raw = $"{id}:{secret}";
    return "Basic " + Convert.ToBase64String(Encoding.ASCII.GetBytes(raw));
}

using var http = new HttpClient();

// Build PAR form
var parForm = new Dictionary<string, string>
{
    ["client_id"] = CLIENT_ID,
    ["response_type"] = "code",
    ["redirect_uri"] = REDIRECT_URI,
    ["scope"] = "openid profile email",
    ["state"] = state,
    ["nonce"] = nonce,
    ["code_challenge"] = codeChallenge,
    ["code_challenge_method"] = "S256"
};

// POST /par
using var parReq = new HttpRequestMessage(HttpMethod.Post, PAR_ENDPOINT)
{
    Content = new FormUrlEncodedContent(parForm)
};
parReq.Headers.Authorization = AuthenticationHeaderValue.Parse(BasicAuth(CLIENT_ID, CLIENT_SECRET));

using var parRes = await http.SendAsync(parReq);
parRes.EnsureSuccessStatusCode();

// Parse response { "request_uri": "...", "expires_in": 90 }
string parJson = await parRes.Content.ReadAsStringAsync();
using var doc = JsonDocument.Parse(parJson);
string requestUri = doc.RootElement.GetProperty("request_uri").GetString()!;

// Build 302 Location for the browser
string location = $"{AUTH_ENDPOINT}?client_id={Uri.EscapeDataString(CLIENT_ID)}&request_uri={Uri.EscapeDataString(requestUri)}";
Console.WriteLine("302 Location: " + location);
// In your web app, respond with: Response.Redirect(location);
```

**Response example — PAR (complete):**

```json
{
  "request_uri": "urn:ietf:params:oauth:request_uri:abc123",
  "expires_in": 90
}
```

**Error example — PAR:**

```json
{
  "error": "invalid_client",
  "error_description": "Client authentication failed",
  "error_uri": "https://example.com/docs/errors#invalid_client"
}
```

---

## 2) Token exchange (authorization code → tokens)

```csharp
// Placeholders
string IDP_BASE = "https://idp.example.com";
string TOKEN_ENDPOINT = $"{IDP_BASE}/oauth2/token";
string CLIENT_ID = "YOUR_CLIENT_ID";
string CLIENT_SECRET = "YOUR_CLIENT_SECRET";
string REDIRECT_URI = "https://app.example.com/callback";

// Inputs from callback query string and step 0
string code = "<AUTH_CODE_FROM_CALLBACK>";
string codeVerifier = "<CODE_VERIFIER_FROM_STEP_0>";

// Helper
string BasicAuth(string id, string secret)
{
    var raw = $"{id}:{secret}";
    return "Basic " + Convert.ToBase64String(Encoding.ASCII.GetBytes(raw));
}

using var http = new HttpClient();

// Build token form
var tokenForm = new Dictionary<string, string>
{
    ["grant_type"] = "authorization_code",
    ["code"] = code,
    ["redirect_uri"] = REDIRECT_URI,
    ["code_verifier"] = codeVerifier
};

// POST /token
using var req = new HttpRequestMessage(HttpMethod.Post, TOKEN_ENDPOINT)
{
    Content = new FormUrlEncodedContent(tokenForm)
};
req.Headers.Authorization = AuthenticationHeaderValue.Parse(BasicAuth(CLIENT_ID, CLIENT_SECRET));

using var res = await http.SendAsync(req);
res.EnsureSuccessStatusCode();

string tokenJson = await res.Content.ReadAsStringAsync();
Console.WriteLine(tokenJson); // contains id_token, access_token, refresh_token, etc.
// In production: validate id_token (JWKS signature, iss, aud, exp, nonce) and JIT-provision user.
```

**Response example — Token (complete):**

```json
{
  "access_token": "eyJhbGciOiJSUzI1NiIsInR5cCI6IkpXVCJ9...",
  "token_type": "Bearer",
  "expires_in": 3600,
  "refresh_token": "eyJhbGciOiJIUzI1NiJ9...",
  "id_token": "eyJraWQiOiJrMSIsImFsZyI6IlJTMjU2In0...",
  "scope": "openid profile email"
}
```

**Error example — Token:**

```json
{
  "error": "invalid_grant",
  "error_description": "Authorization code is invalid or expired",
  "error_uri": "https://example.com/docs/errors#invalid_grant"
}
```

---

## 3) Refresh tokens (get a new access token)

```csharp
// Placeholders
string IDP_BASE = "https://idp.example.com";
string TOKEN_ENDPOINT = $"{IDP_BASE}/oauth2/token";
string CLIENT_ID = "YOUR_CLIENT_ID";
string CLIENT_SECRET = "YOUR_CLIENT_SECRET";

// From your server-side session
string refreshToken = "<YOUR_REFRESH_TOKEN>";

// Helper
string BasicAuth(string id, string secret)
{
    var raw = $"{id}:{secret}";
    return "Basic " + Convert.ToBase64String(Encoding.ASCII.GetBytes(raw));
}

using var http = new HttpClient();

var refreshForm = new Dictionary<string, string>
{
    ["grant_type"] = "refresh_token",
    ["refresh_token"] = refreshToken
};

using var req = new HttpRequestMessage(HttpMethod.Post, TOKEN_ENDPOINT)
{
    Content = new FormUrlEncodedContent(refreshForm)
};
req.Headers.Authorization = AuthenticationHeaderValue.Parse(BasicAuth(CLIENT_ID, CLIENT_SECRET));

using var res = await http.SendAsync(req);
res.EnsureSuccessStatusCode();

string json = await res.Content.ReadAsStringAsync();
Console.WriteLine(json); // new access_token (and possibly new refresh_token)
```

**Response example — Refresh (complete):**

```json
{
  "access_token": "eyJhbGciOiJSUzI1NiIsInR5cCI6IkpXVCJ9...",
  "token_type": "Bearer",
  "expires_in": 3600,
  "refresh_token": "eyJhbGciOiJIUzI1NiJ9...",
  "id_token": "eyJraWQiOiJrMSIsImFsZyI6IlJTMjU2In0...",
  "scope": "openid profile email"
}
```

**Error example — Refresh:**

```json
{
  "error": "invalid_grant",
  "error_description": "Refresh token is invalid, revoked, or expired",
  "error_uri": "https://example.com/docs/errors#invalid_grant"
}
```

---

## 4) Optional — Revoke a token

```csharp
// Placeholders
string IDP_BASE = "https://idp.example.com";
string REVOCATION_ENDPOINT = $"{IDP_BASE}/oauth2/revoke";
string CLIENT_ID = "YOUR_CLIENT_ID";
string CLIENT_SECRET = "YOUR_CLIENT_SECRET";

// Choose which token to revoke (often refresh_token on logout)
string tokenToRevoke = "<REFRESH_TOKEN>";

// Helper
string BasicAuth(string id, string secret)
{
    var raw = $"{id}:{secret}";
    return "Basic " + Convert.ToBase64String(Encoding.ASCII.GetBytes(raw));
}

using var http = new HttpClient();

var form = new Dictionary<string, string>
{
    ["token"] = tokenToRevoke,
    ["token_type_hint"] = "refresh_token"
};

using var req = new HttpRequestMessage(HttpMethod.Post, REVOCATION_ENDPOINT)
{
    Content = new FormUrlEncodedContent(form)
};
req.Headers.Authorization = AuthenticationHeaderValue.Parse(BasicAuth(CLIENT_ID, CLIENT_SECRET));

using var res = await http.SendAsync(req);
res.EnsureSuccessStatusCode();

Console.WriteLine("Revoked");
```

**Response example — Revocation:**

> Per RFC 7009, successful revocation returns `200 OK` with an **empty body**.

**Error example — Revocation:**

```json
{
  "error": "invalid_client",
  "error_description": "Client authentication failed",
  "error_uri": "https://example.com/docs/errors#invalid_client"
}
```

---

## 5) Optional — Call UserInfo (only if you need extra claims)

```csharp
// Placeholders
string IDP_BASE = "https://idp.example.com";
string USERINFO_ENDPOINT = $"{IDP_BASE}/oauth2/userinfo";

// From your server-side session
string accessToken = "<ACCESS_TOKEN>";

using var http = new HttpClient();
using var req = new HttpRequestMessage(HttpMethod.Get, USERINFO_ENDPOINT);
req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

using var res = await http.SendAsync(req);
res.EnsureSuccessStatusCode();

string infoJson = await res.Content.ReadAsStringAsync();
Console.WriteLine(infoJson); // { "sub": "...", "email": "...", "name": "...", ... }
```

**Response example — UserInfo (rich set of claims):**

```json
{
  "sub": "248289761001",
  "name": "Jane Doe",
  "given_name": "Jane",
  "family_name": "Doe",
  "middle_name": "A.",
  "nickname": "jdoe",
  "preferred_username": "jane",
  "profile": "https://example.com/jane",
  "picture": "https://example.com/jane.jpg",
  "website": "https://jane.example.com",
  "email": "jane@example.com",
  "email_verified": true,
  "gender": "female",
  "birthdate": "1972-03-11",
  "zoneinfo": "Europe/Paris",
  "locale": "en-US",
  "phone_number": "+1 (425) 555-1212",
  "phone_number_verified": false,
  "address": {
    "formatted": "123 Main St, Apt 2, Anytown, CA 94019, USA",
    "street_address": "123 Main St, Apt 2",
    "locality": "Anytown",
    "region": "CA",
    "postal_code": "94019",
    "country": "US"
  },
  "updated_at": 1697040000
}
```

---

## Notes

* **Validate `id_token`**: verify the signature via the IdP `jwks_uri`, and check `iss`, `aud`, `exp`, and `nonce`.
* **Never expose IdP tokens to the browser**; the SPA uses only the **HttpOnly, Secure, SameSite** session cookie your backend sets.
* **FAPI hardening (later)**: migrate client auth to `private_key_jwt` or mTLS; consider sender-constrained tokens (DPoP/mTLS).
* **Never expose IdP tokens to the browser**; the SPA uses only the **HttpOnly, Secure, SameSite** session cookie your backend sets.
* **FAPI hardening (later)**: migrate client auth to `private_key_jwt` or mTLS; consider sender-constrained tokens (DPoP/mTLS).
