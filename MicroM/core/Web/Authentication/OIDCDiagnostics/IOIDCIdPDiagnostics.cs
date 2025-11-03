using MicroM.Data;
using MicroM.Web.Services;

namespace MicroM.Web.Authentication.SSO;

public interface IOIDCIdPDiagnostics
{
    // Orchestrator: for apps with role == IDPServer (tests each configured client)
    Task<List<OIDCDiagnosticsResult>> TestAllAsync(
        IEntityClient ec,
        string appId,
        IMicroMAppConfiguration appConfig,
        IIdentityProviderService idpService,
        IOIDCHttpClient httpClient,
        CancellationToken ct);

    // IdP signing material self-check
    Task<OIDCDiagnosticsResult> TestSigningMaterialAsync(
        IMicroMAppConfiguration appConfig,
        IIdentityProviderService idpService,
        string appId,
        CancellationToken ct);

    // IdP discovery + JWKS self-check (issuer/endpoints/algs/kids)
    Task<OIDCDiagnosticsResult> TestWellKnownAndJWKSAsync(
        IMicroMAppConfiguration appConfig,
        IIdentityProviderService idpService,
        string appId,
        CancellationToken ct);

    // IdP PAR handling (server-side) — test all configured clients
    Task<List<OIDCDiagnosticsResult>> TestPARAsync(
        IMicroMAppConfiguration appConfig,
        IIdentityProviderService idpService,
        string appId,
        CancellationToken ct);

    // Token grants smoke tests (error semantics, headers) — test all configured clients
    Task<List<OIDCDiagnosticsResult>> TestTokenGrantsAsync(
        IMicroMAppConfiguration appConfig,
        IIdentityProviderService idpService,
        IOIDCHttpClient httpClient,
        string appId,
        CancellationToken ct);

    // IdP end-session fanout to clients — test all configured clients
    Task<List<OIDCDiagnosticsResult>> TestEndSessionFanoutAsync(
        IEntityClient ec,
        IMicroMAppConfiguration appConfig,
        IIdentityProviderService idpService,
        string appId,
        CancellationToken ct);

    // Client registration/config sanity across configured clients — test all configured clients
    Task<List<OIDCDiagnosticsResult>> TestClientRegistrationSanityAsync(
        IEntityClient ec,
        IMicroMAppConfiguration appConfig,
        IIdentityProviderService idpService,
        string appId,
        CancellationToken ct);

    // Issuer consistency across discovery, tokens, and endsession (IdP-level)
    Task<OIDCDiagnosticsResult> TestIssuerConsistencyAsync(
        IMicroMAppConfiguration appConfig,
        IIdentityProviderService idpService,
        IOIDCHttpClient httpClient,
        string appId,
        CancellationToken ct);

    // Front-channel logout configuration check (IdP → client config)
    Task<List<OIDCDiagnosticsResult>> TestFrontChannelLogoutAsync(
        IMicroMAppConfiguration appConfig,
        IIdentityProviderService idpService,
        string appId,
        CancellationToken ct);
}
