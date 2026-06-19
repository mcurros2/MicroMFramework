using System.Text.Json.Serialization;

namespace MicroM.Web.Authentication.SSO;

// String-style enums: member names match the exact strings expected in the OIDC discovery document.
// JsonStringEnumConverter will serialize enum members by name (so keep these identifiers matching spec strings).
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum OIDCResponseType { code }

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum OIDCResponseMode { query, fragment, form_post }


[JsonConverter(typeof(JsonStringEnumConverter))]
public enum OIDCSubjectType { @public, pairwise }

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
public enum OIDCCodeChallengeMethod { S256, plain }

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum OIDCKeyType { RSA, EC, OKP }

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum OIDCKeyUse { sig, enc }

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum OIDCKeyCurveValues
{
    [JsonStringEnumMemberName("P-256")]
    P256,
    [JsonStringEnumMemberName("P-384")]
    P384,
    [JsonStringEnumMemberName("P-521")]
    P521,
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum OIDCEncryptionAlg
{
    [JsonStringEnumMemberName("A128CBC-HS256")]
    A128CBC_HS256,
    [JsonStringEnumMemberName("A192CBC-HS384")]
    A192CBC_HS384,
    [JsonStringEnumMemberName("A256CBC-HS512")]
    A256CBC_HS512,
    A128GCM,
    A192GCM,
    A256GCM
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum OIDCKeyEncryptionAlgorithm
{
    RSA1_5,
    [JsonStringEnumMemberName("RSA-OAEP")]
    RSA_OAEP,
    [JsonStringEnumMemberName("RSA-OAEP-256")]
    RSA_OAEP_256,
    A128KW,
    A192KW,
    A256KW,
    dir,
    [JsonStringEnumMemberName("ECDH-ES")]
    ECDH_ES,
    [JsonStringEnumMemberName("ECDH-ES+A128KW")]
    ECDH_ES_A128KW,
    [JsonStringEnumMemberName("ECDH-ES+A192KW")]
    ECDH_ES_A192KW,
    [JsonStringEnumMemberName("ECDH-ES+A256KW")]
    ECDH_ES_A256KW,
    A128GCMKW,
    A192GCMKW,
    A256GCMKW,
    [JsonStringEnumMemberName("PBES2-HS256+A128KW")]
    PBES2_HS256_A128KW,
    [JsonStringEnumMemberName("PBES2-HS384+A192KW")]
    PBES2_HS256_A192KW,
    [JsonStringEnumMemberName("PBES2-HS512+A256KW")]
    PBES2_HS512_A256KW
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum OIDCGrantType { authorization_code, refresh_token }

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum OIDCTokenEndpointAuthMethod { client_secret_basic, private_key_jwt }

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum OIDCProfileScopes { openid, email, profile }

public sealed record OIDCJwksResponse(IReadOnlyList<OIDCJwksKeyResponse> keys);

public sealed record OIDCWellKnownResponse
(
    // Required
    string issuer,

    string authorization_endpoint,
    string token_endpoint,
    string jwks_uri,

    // Use strongly-typed lists so serialization follows spec and values are consistent.
    IReadOnlyList<OIDCResponseType>? response_types_supported = null,
    IReadOnlyList<OIDCSubjectType>? subject_types_supported = null,

    // Endpoints
    string? userinfo_endpoint = null,
    string? end_session_endpoint = null,
    string? pushed_authorization_request_endpoint = null,
    string? revocation_endpoint = null,
    string? introspection_endpoint = null,
    string? registration_endpoint = null,



    string? check_session_iframe = null,
    IReadOnlyList<OIDCGrantType>? grant_types_supported = null,
    IReadOnlyList<string>? acr_values_supported = null,
    IReadOnlyList<OIDCResponseMode>? response_modes_supported = null,
    IReadOnlyList<OIDCProfileScopes>? scopes_supported = null,
    IReadOnlyList<string>? claims_supported = null,
    bool? claims_parameter_supported = null,
    bool? request_uri_parameter_supported = null,
    bool? require_request_uri_registration = null,
    bool? require_pushed_authorization_requests = null,

    bool? frontchannel_logout_supported = null,
    bool? frontchannel_logout_session_supported = null,
    bool? backchannel_logout_supported = null,
    bool? backchannel_logout_session_supported = null,

    IReadOnlyList<string>? display_values_supported = null,
    IReadOnlyList<string>? claim_types_supported = null,
    string? service_documentation = null,
    string? op_policy_uri = null,
    string? op_tos_uri = null,
    IReadOnlyList<string>? ui_locales_supported = null,

    IReadOnlyList<OIDCKeyEncryptionAlgorithm>? id_token_encryption_alg_values_supported = null,
    IReadOnlyList<OIDCEncryptionAlg>? id_token_encryption_enc_values_supported = null,

    IReadOnlyList<OIDCKeyEncryptionAlgorithm>? userinfo_encryption_alg_values_supported = null,
    IReadOnlyList<OIDCEncryptionAlg>? userinfo_encryption_enc_values_supported = null,

    IReadOnlyList<OIDCKeyEncryptionAlgorithm>? request_object_encryption_alg_values_supported = null,
    IReadOnlyList<OIDCEncryptionAlg>? request_object_encryption_enc_values_supported = null,

    IReadOnlyList<OIDCKeyEncryptionAlgorithm>? authorization_encryption_alg_values_supported = null,
    IReadOnlyList<OIDCEncryptionAlg>? authorization_encryption_enc_values_supported = null,

    IReadOnlyList<OIDCKeyEncryptionAlgorithm>? pushed_authorization_request_encryption_alg_values_supported = null,
    IReadOnlyList<OIDCEncryptionAlg>? pushed_authorization_request_encryption_enc_values_supported = null,


    IReadOnlyList<OIDCSigningAlg>? id_token_signing_alg_values_supported = null,
    IReadOnlyList<OIDCSigningAlg>? userinfo_signing_alg_values_supported = null,
    IReadOnlyList<OIDCSigningAlg>? token_endpoint_auth_signing_alg_values_supported = null,
    IReadOnlyList<OIDCSigningAlg>? request_object_signing_alg_values_supported = null,
    IReadOnlyList<OIDCSigningAlg>? revocation_endpoint_auth_signing_alg_values_supported = null,
    IReadOnlyList<OIDCSigningAlg>? introspection_endpoint_auth_signing_alg_values_supported = null,


    IReadOnlyList<OIDCTokenEndpointAuthMethod>? revocation_endpoint_auth_methods_supported = null,
    IReadOnlyList<OIDCTokenEndpointAuthMethod>? token_endpoint_auth_methods_supported = null,
    IReadOnlyList<OIDCTokenEndpointAuthMethod>? introspection_endpoint_auth_methods_supported = null,
    IReadOnlyList<OIDCTokenEndpointAuthMethod>? pushed_authorization_request_endpoint_auth_methods_supported = null,

    bool? authorization_response_iss_parameter_supported = null,

    IReadOnlyList<OIDCCodeChallengeMethod>? code_challenge_methods_supported = null,

    IReadOnlyList<string>? claims_locales_supported = null,
    bool? request_parameter_supported = null
);