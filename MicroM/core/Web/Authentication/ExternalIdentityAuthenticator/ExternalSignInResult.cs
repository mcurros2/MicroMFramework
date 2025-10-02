namespace MicroM.Web.Authentication;

public record ExternalSignInResult(
    Dictionary<string, object> ServerClaims,
    Dictionary<string, string> ClientClaims,
    string? device_id,
    string? RefreshToken // per-device client refresh token
);