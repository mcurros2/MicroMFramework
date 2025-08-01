namespace MicroM.Web.Services;

public class SSOAuthenticatorResult
{
    public bool Succeeded { get; set; }
    public string? ErrorMessage { get; set; }
    public Dictionary<string, object>? Claims { get; set; }
}