using MicroM.Web.Services;

namespace MicroM.Web.Authentication.SSO;

public enum OIDCDiagnosticsTestType
{
    Unknown = 0,
    WellKnownAndJWKS = 1,
    PAR = 2,
    RefreshFallback = 3,
    BackchannelReceiver = 4,
    SigningMaterial = 5,
    PAR_IdPServer = 6,
    EndSessionFanout = 7,
    ClientRegistrationSanity = 8,
    PairwiseSubDerivation = 9,
    FrontChannelLogout = 10,
    TokenEndpoint = 11,
    EndSessionEndpoint = 12,
    UserInfoEndpoint = 13,
    RevocationEndpoint = 14,
    IntrospectionEndpoint = 15
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
