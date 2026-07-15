namespace MicroM.Web.Authentication;

public class TotpSetupRequest
{
    public required string AuthenticatorName { get; set; }
}

public class TwoFactorEmailCodeRequest
{
    public required string ChallengeId { get; set; }
}

public class TotpDeleteAuthenticatorRequest
{
    public required string AuthenticatorId { get; set; }
}
