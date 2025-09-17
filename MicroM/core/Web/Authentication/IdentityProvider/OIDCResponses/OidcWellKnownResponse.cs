using System.Text.Json.Serialization;

namespace MicroM.Web.Authentication.SSO;

// String-style enums: member names match the exact strings expected in the OIDC discovery document.
// JsonStringEnumConverter will serialize enum members by name (so keep these identifiers matching spec strings).
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum OIDCResponseType { code }

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum OIDCResponseMode { query, fragment, form_post }


[JsonConverter(typeof(JsonStringEnumConverter))]
public enum OIDCSubjectType { @public }

/*
   +--------------+-------------------------------+--------------------+
   | "alg" Param  | Digital Signature or MAC      | Implementation     |
   | Value        | Algorithm                     | Requirements       |
   +--------------+-------------------------------+--------------------+
   | HS256        | HMAC using SHA-256            | Required           |
   | HS384        | HMAC using SHA-384            | Optional           |
   | HS512        | HMAC using SHA-512            | Optional           |
   | RS256        | RSASSA-PKCS1-v1_5 using       | Recommended        |
   |              | SHA-256                       |                    |
   | RS384        | RSASSA-PKCS1-v1_5 using       | Optional           |
   |              | SHA-384                       |                    |
   | RS512        | RSASSA-PKCS1-v1_5 using       | Optional           |
   |              | SHA-512                       |                    |
   | ES256        | ECDSA using P-256 and SHA-256 | Recommended+       |
   | ES384        | ECDSA using P-384 and SHA-384 | Optional           |
   | ES512        | ECDSA using P-521 and SHA-512 | Optional           |
   | PS256        | RSASSA-PSS using SHA-256 and  | Optional           |
   |              | MGF1 with SHA-256             |                    |
   | PS384        | RSASSA-PSS using SHA-384 and  | Optional           |
   |              | MGF1 with SHA-384             |                    |
   | PS512        | RSASSA-PSS using SHA-512 and  | Optional           |
   |              | MGF1 with SHA-512             |                    |
   | none         | No digital signature or MAC   | Optional           |
   |              | performed                     |                    |
   +--------------+-------------------------------+--------------------+
 
 */
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum OIDCSigningAlg { HS256, HS384, HS512, RS256, RS384, RS512, ES256, ES384, ES512, PS256, PS384, PS512, none }

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum OIDCCodeChallengeMethod { S256 }

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum OIDCKeyType { RSA, EC, OKP }

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum OIDCKeyUse { sig, enc }

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum OIDCKeyCurveValues
{
    [JsonPropertyName("P-256")]
    P256,
    [JsonPropertyName("P-384")]
    P384,
    [JsonPropertyName("P-521")]
    P521,
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum OIDCGrantType { authorization_code, refresh_token }

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum OIDCTokenEndpointAuthMethod { client_secret_basic, private_key_jwt }

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum OIDCProfileScopes { openid, email, profile }

public sealed record OIDCJwksResponse(List<OIDCJwksKeyResponse> keys);

public class OIDCWellKnownResponse
{
    // Required
    public required string issuer { get; set; }
    public required string authorization_endpoint { get; set; }
    public required string token_endpoint { get; set; }
    public required string jwks_uri { get; set; }

    // Use strongly-typed lists so serialization follows spec and values are consistent.
    public List<OIDCResponseType>? response_types_supported { get; set; }
    public List<OIDCSubjectType>? subject_types_supported { get; set; }
    public List<OIDCSigningAlg>? id_token_signing_alg_values_supported { get; set; }

    // Optional
    public string? userinfo_endpoint { get; set; }
    public string? end_session_endpoint { get; set; }
    public string? pushed_authorization_request_endpoint { get; set; }
    public string? check_session_iframe { get; set; }
    public List<OIDCGrantType>? grant_types_supported { get; set; }
    public List<string>? acr_values_supported { get; set; }
    public List<OIDCResponseMode>? response_modes_supported { get; set; }
    public List<OIDCProfileScopes>? scopes_supported { get; set; }
    public List<string>? claims_supported { get; set; }
    public bool? claims_parameter_supported { get; set; }
    public bool? request_uri_parameter_supported { get; set; }
    public bool? require_request_uri_registration { get; set; }
    public bool? require_pushed_authorization_requests { get; set; }

    public bool? frontchannel_logout_supported { get; set; }
    public bool? frontchannel_logout_session_supported { get; set; }
    public bool? backchannel_logout_supported { get; set; }
    public bool? backchannel_logout_session_supported { get; set; }

    // Token auth methods / signing algs
    public List<OIDCTokenEndpointAuthMethod>? token_endpoint_auth_methods_supported { get; set; }
    public List<OIDCSigningAlg>? token_endpoint_auth_signing_alg_values_supported { get; set; }

    public List<string>? display_values_supported { get; set; }
    public List<string>? claim_types_supported { get; set; }
    public string? service_documentation { get; set; }
    public string? op_policy_uri { get; set; }
    public string? op_tos_uri { get; set; }
    public List<string>? request_object_signing_alg_values_supported { get; set; }
    public List<string>? ui_locales_supported { get; set; }
    public List<string>? id_token_encryption_alg_values_supported { get; set; }
    public List<string>? id_token_encryption_enc_values_supported { get; set; }
    public List<string>? userinfo_signing_alg_values_supported { get; set; }
    public List<string>? userinfo_encryption_alg_values_supported { get; set; }
    public List<string>? userinfo_encryption_enc_values_supported { get; set; }
    public List<string>? request_object_encryption_alg_values_supported { get; set; }
    public List<string>? request_object_encryption_enc_values_supported { get; set; }

    public string? revocation_endpoint { get; set; }
    public List<OIDCTokenEndpointAuthMethod>? revocation_endpoint_auth_methods_supported { get; set; }
    public List<OIDCSigningAlg>? revocation_endpoint_auth_signing_alg_values_supported { get; set; }

    public string? introspection_endpoint { get; set; }
    public List<OIDCTokenEndpointAuthMethod>? introspection_endpoint_auth_methods_supported { get; set; }
    public List<OIDCSigningAlg>? introspection_endpoint_auth_signing_alg_values_supported { get; set; }

    public bool? authorization_response_iss_parameter_supported { get; set; }

    public List<OIDCCodeChallengeMethod>? code_challenge_methods_supported { get; set; }
    public string? registration_endpoint { get; set; }
    public List<string>? claims_locales_supported { get; set; }
    public bool? request_parameter_supported { get; set; }
}