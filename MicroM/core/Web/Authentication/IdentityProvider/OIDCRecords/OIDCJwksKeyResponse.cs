using System.Text.Json.Serialization;

namespace MicroM.Web.Authentication.SSO;

public sealed record OIDCJwksKeyResponse(
    // Key identification / common metadata
    string? kid = null,

    OIDCKeyType? kty = null,

    OIDCSigningAlg? alg = null,

    OIDCKeyUse? use = null,

    // Key operations (optional)
    List<string>? key_ops = null,

    // RSA parameters
    string? n = null,

    string? e = null,

    // EC parameters
    OIDCKeyCurveValues? crv = null,

    string? x = null,

    string? y = null,

    // X.509 certificate chain / thumbprints / URI
    List<string>? x5c = null,

    string? x5t = null,

    string? x5u = null,

    [property: JsonPropertyName("x5t#S256")]
    string? x5tS256 = null,

    // Extraction flag (optional)
    bool? ext = null,

    object? oth = null

);