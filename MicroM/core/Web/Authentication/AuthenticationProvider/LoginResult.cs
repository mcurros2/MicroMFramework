namespace MicroM.Web.Authentication;

public record LoginResult
{
    public string? email { get; set; }
    public string username { get; set; } = "";
    public string? refresh_token { get; set; }
    public string? oidc_session_id { get; set; }
    public bool requires_two_factor { get; set; }
    public string? two_factor_challenge_id { get; set; }
    public string? two_factor_provider { get; set; }
    public Dictionary<string, string> client_claims { get; set; } = new(StringComparer.OrdinalIgnoreCase);
    public AuthenticatorResult authenticator_result { get; set; } = new();
}
