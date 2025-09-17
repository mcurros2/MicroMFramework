using System.Text.Json.Serialization;

namespace MicroM.Web.Authentication.SSO;

public class OIDCJwksKeyResponse
{
    // Key identification / common metadata
    public string? kid { get; set; }

    public OIDCKeyType? kty { get; set; }

    public OIDCSigningAlg? alg { get; set; }

    public OIDCKeyUse? use { get; set; }

    // Key operations (optional)
    public List<string>? key_ops { get; set; }

    // RSA public parameters
    public string? n { get; set; }

    public string? e { get; set; }

    // EC public parameters
    public OIDCKeyCurveValues? crv { get; set; }

    public string? x { get; set; }

    public string? y { get; set; }

    // X.509 certificate chain / thumbprints / URI
    public List<string>? x5c { get; set; }

    public string? x5t { get; set; }

    public string? x5u { get; set; }

    [JsonPropertyName("x5t#S256")]
    public string? x5tS256 { get; set; }

    // Extraction flag (optional)
    public bool? ext { get; set; }

    public object? oth { get; set; }

}