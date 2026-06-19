using MicroM.Core;

namespace MicroM.Web.Authentication.SSO;

public enum OIDCDiagnosticsTestType
{
    Unknown = 0,
    IdPConfiguration,

    WellKnownAndJWKS,
    PAR,
    RefreshFallback,
    BackchannelReceiver,
    SigningMaterial,
    PAR_IdPServer,
    EndSessionFanout,

    ClientRegistrationSanity,
    PairwiseSubDerivation,
    FrontChannelLogout,
    TokenEndpoint,
    EndSessionEndpoint,
    UserInfoEndpoint,
    RevocationEndpoint,
    IntrospectionEndpoint
}

public sealed record OIDCDiagnosticsResult
(
    OIDCDiagnosticsTestType TestType,
    string? Result = null,
    List<ErrorResult>? Errors = null
)
{
    public bool IsSuccess => Errors == null || Errors.Count == 0;
}
