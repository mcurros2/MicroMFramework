using MicroM.Data;
using MicroM.Web.Services;

namespace MicroM.Web.Authentication.SSO;

/// <summary>
/// Client-side OIDC diagnostics (apps with role == IDPClient).
/// </summary>
/// <remarks>
/// This interface defines diagnostics that validate the configured Identity Provider (IdP) for a given client app (IDPClient role).
/// All tests must use endpoints obtained from the IdP discovery (well-known) document and perform network I/O via <see cref="IOIDCHttpClient"/>.
/// Important: Front-channel logout diagnostics are performed from the IdPServer perspective because the URL is owned by the IdP client registration
/// (see ApplicationOption.OIDCClientConfiguration). This method remains for compatibility but should be treated as not applicable in client diagnostics.
/// </remarks>
public interface IOIDCClientDiagnostics
{
    /// <summary>
    /// Orchestrates client diagnostics for apps with role == IDPClient.
    /// Must exercise endpoints discovered from well-known (discovery/JWKS, PAR, authorize capabilities, token, etc.).
    /// </summary>
    Task<List<OIDCDiagnosticsResult>> TestAllAsync(
        IEntityClient ec,
        string appId,
        IMicroMAppConfiguration appConfig,
        IIdentityProviderService idpService,
        IOIDCClientService clientService,
        IOIDCHttpClient httpClient,
        CancellationToken ct);

    /// <summary>
    /// GET discovery + JWKS from the configured IdP (client POV) and validate basic shape:
    /// - issuer present,
    /// - jwks_uri present and returns a non-empty key set.
    /// </summary>
    Task<OIDCDiagnosticsResult> TestWellKnownAndJWKSAsync(
        IMicroMAppConfiguration appConfig,
        IOIDCHttpClient httpClient,
        string appId,
        CancellationToken ct);

    /// <summary>
    /// Client-side PAR probe to the IdP from the discovery document (pushed_authorization_request_endpoint).
    /// Uses <see cref="IOIDCHttpClient"/> to POST a minimal form and accepts 200/400/401/403 as valid semantics.
    /// </summary>
    Task<OIDCDiagnosticsResult> TestPARAsync(
        IEntityClient ec,
        IMicroMAppConfiguration appConfig,
        IOIDCHttpClient httpClient,
        string appId,
        CancellationToken ct);

    /// <summary>
    /// Authorize endpoint capability check derived from the discovery document (authorization_endpoint, response_types_supported=code, PKCE S256, scopes).
    /// No redirects are executed; this is a metadata validation.
    /// </summary>
    Task<OIDCDiagnosticsResult> TestAuthorizeUrlBuildAndRedirectUriAsync(
        IMicroMAppConfiguration appConfig,
        IOIDCClientService clientService,
        IOIDCHttpClient httpClient,
        string appId,
        CancellationToken ct);

    /// <summary>
    /// Client callback endpoint presence/shape validation (client POV).
    /// This should verify the presence and expected behavior of the callback route if applicable.
    /// </summary>
    Task<OIDCDiagnosticsResult> TestCallbackEndpointAsync(
        IMicroMAppConfiguration appConfig,
        IOIDCHttpClient httpClient,
        string appId,
        CancellationToken ct);

    /// <summary>
    /// Refresh token fallback (Strategy B: local → IdP) behavior validation.
    /// This should simulate the refresh flow at the client level, falling back to IdP refresh when needed.
    /// </summary>
    Task<OIDCDiagnosticsResult> TestRefreshFallbackAsync(
        IEntityClient ec,
        IMicroMAppConfiguration appConfig,
        IOIDCClientService clientService,
        string appId,
        CancellationToken ct);

    /// <summary>
    /// Direct IdP refresh/token endpoint probe (no local fallback).
    /// Posts an invalid refresh_token to validate token endpoint reachability and error semantics.
    /// Accepts 400/401/403 as valid outcomes.
    /// </summary>
    Task<OIDCDiagnosticsResult> TestIdpRefreshAsync(
        IEntityClient ec,
        IMicroMAppConfiguration appConfig,
        IOIDCClientService clientService,
        IOIDCHttpClient httpClient,
        string appId,
        CancellationToken ct);

    /// <summary>
    /// Backchannel logout receiver (client POV) validation.
    /// This should verify the presence of the backchannel route and basic error semantics without duplicating token validation logic.
    /// </summary>
    Task<OIDCDiagnosticsResult> TestBackchannelReceiverAsync(
        IEntityClient ec,
        IMicroMAppConfiguration appConfig,
        IIdentityProviderService idpService,
        IOIDCClientService clientService,
        string appId,
        CancellationToken ct);

    /// <summary>
    /// Probe end_session_endpoint presence and basic error semantics.
    /// A POST without required params is expected to yield 400/401/403/405.
    /// </summary>
    Task<OIDCDiagnosticsResult> TestEndSessionEndpointAsync(
        IMicroMAppConfiguration appConfig,
        IOIDCHttpClient httpClient,
        string appId,
        CancellationToken ct);

    /// <summary>
    /// Probe userinfo endpoint reachability and expected auth errors (401/403/405) with no Authorization header.
    /// </summary>
    Task<OIDCDiagnosticsResult> TestUserInfoEndpointAsync(
        IMicroMAppConfiguration appConfig,
        IOIDCHttpClient httpClient,
        string appId,
        CancellationToken ct);

    /// <summary>
    /// Probe OAuth 2.0 token revocation endpoint with an invalid token.
    /// Accepts 200/400/401/403 as valid semantics.
    /// </summary>
    Task<OIDCDiagnosticsResult> TestRevocationEndpointAsync(
        IMicroMAppConfiguration appConfig,
        IOIDCHttpClient httpClient,
        string appId,
        CancellationToken ct);

    /// <summary>
    /// Probe OAuth 2.0 token introspection endpoint with an invalid token.
    /// Accepts 200/400/401/403 as valid semantics.
    /// </summary>
    Task<OIDCDiagnosticsResult> TestIntrospectionEndpointAsync(
        IMicroMAppConfiguration appConfig,
        IOIDCHttpClient httpClient,
        string appId,
        CancellationToken ct);
}
