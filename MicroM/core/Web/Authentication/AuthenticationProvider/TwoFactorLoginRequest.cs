namespace MicroM.Web.Authentication;

public class TwoFactorLoginRequest
{
    public required string ChallengeId { get; set; }
    public required string Code { get; set; }
}
