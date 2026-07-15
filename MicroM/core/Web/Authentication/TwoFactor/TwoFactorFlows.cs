namespace MicroM.Web.Authentication;

public static class TwoFactorFlows
{
    public const string Authenticator = "authenticator";
    public const string EmailSetup = "email_setup";
    public const string EmailRecovery = "email_recovery";
    public const string SupportRequired = "support_required";
    public const string SqlAdminSetup = "sql_admin_setup";
    public const string SqlAdminAuthenticator = "sql_admin_authenticator";
}

public static class TwoFactorChallengeMetadataKeys
{
    public const string Flow = "flow";
    public const string EmailTotpSecret = "email_totp_secret";
    public const string SetupTotpSecret = "setup_totp_secret";
    public const string AuthenticatorName = "authenticator_name";
}
