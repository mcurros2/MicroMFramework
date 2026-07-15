using Microsoft.AspNetCore.Identity;

namespace MicroM.Web.Authentication;

public class AuthenticatorResult
{
    public bool AccountDisabled = false;
    public bool AccountLocked = false;
    public bool AccountNotProvisioned = false;
    public PasswordVerificationResult PasswordVerificationResult = PasswordVerificationResult.Failed;
    public LoginData? LoginData = null;
    public Dictionary<string, object> ServerClaims = new(StringComparer.OrdinalIgnoreCase);
    public Dictionary<string, string> ClientClaims = new(StringComparer.OrdinalIgnoreCase);

    // MFA/TOTP support
    public bool RequiresTwoFactor = false;
    public string? TwoFactorChallengeId = null;
    public string? TwoFactorProvider = null;
    public string? TwoFactorFlow = null;
    public bool TwoFactorSetupRequired = false;
    public bool AuthenticatorManagementRequired = false;
    public string? QrCodeDataUrl = null;
}
