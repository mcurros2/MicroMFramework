namespace MicroM.Web.Authentication;

public record LoginAttemptResult
{
    public LoginAttemptStatus Status { get; set; } = LoginAttemptStatus.Unknown;
    public string? Message { get; set; } = null;
    public string? RefreshToken { get; set; } = null;
}
