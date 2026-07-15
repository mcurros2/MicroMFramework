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
    public string? two_factor_flow { get; set; }
    public bool two_factor_setup_required { get; set; }
    public bool authenticator_management_required { get; set; }
    public string? qr_code_data_url { get; set; }
    public Dictionary<string, string> client_claims { get; set; } = new(StringComparer.OrdinalIgnoreCase);
    public AuthenticatorResult authenticator_result { get; set; } = new();
}
