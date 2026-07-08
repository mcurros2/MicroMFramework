namespace MicroM.Web.Authentication;

public class TwoFactorRegistrationRequest
{
    public required string ChallengeId { get; set; }
}
