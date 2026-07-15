namespace MicroM.Web.Authentication;

public class TotpConfirmRequest
{
    public string? SetupChallengeId { get; set; }
    public string Code { get; set; } = "";
}
