namespace MicroM.Web.Authentication;

public record LoginResult
{
    public string? email { get; set; }
    public string username { get; set; } = "";
    public string? refresh_token { get; set; }
    public Dictionary<string, string> client_claims { get; set; } = new(StringComparer.OrdinalIgnoreCase);
    public AuthenticatorResult authenticator_result { get; set; } = new();
}
