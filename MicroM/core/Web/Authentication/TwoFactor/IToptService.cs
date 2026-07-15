namespace MicroM.Web.Authentication;

/// <summary>
/// Service for generating and verifying TOTP codes using ASP.NET Identity's implementation.
/// </summary>
public interface ITotpService
{
    string GenerateSecret();

    Task<TotpServiceResult> HandleStartTotpSetup(IAuthenticationProvider auth, string app_id, string user_name, TotpSetupRequest request, Dictionary<string, object> server_claims, CancellationToken ct);

    Task<TotpServiceResult> HandleConfirmTotpSetup(IAuthenticationProvider auth, string app_id, string user_name, TotpConfirmRequest request, Dictionary<string, object> server_claims, CancellationToken ct);

    Task<TotpServiceResult> HandleDisableTotp(IAuthenticationProvider auth, string app_id, string user_name, Dictionary<string, object> server_claims, CancellationToken ct);

    Task<TotpServiceResult> HandleLoginTotpRegistration(IAuthenticationProvider auth, string app_id, TwoFactorRegistrationRequest request, CancellationToken ct);

    Task<TotpAuthenticatorsResponse> HandleListAuthenticators(IAuthenticationProvider auth, string app_id, string user_name, Dictionary<string, object> server_claims, CancellationToken ct);

    Task<TotpServiceResult> HandleDeleteAuthenticator(IAuthenticationProvider auth, string app_id, string user_name, TotpDeleteAuthenticatorRequest request, Dictionary<string, object> server_claims, CancellationToken ct);

    bool VerifyCode(string secret, string code, string? securityStampModifier = null, TotpSupportedDigits digits = TotpSupportedDigits.Six);

    string GenerateCurrentCode(string secret, string? securityStampModifier = null);

    string GetAuthenticatorUri(string username, string secret, string issuer = "MicroM", TotpSupportedDigits digits = TotpSupportedDigits.Six);

    string GetAuthenticatorQrCodeDataUrl(string authenticatorUri);

    string FormatSecret(string secret);
}
